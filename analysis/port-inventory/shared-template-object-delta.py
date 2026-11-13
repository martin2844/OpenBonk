#!/usr/bin/env python3
"""Rebuild historical local shared-asset templates from a restored OBB.

Delta creation matches serialized objects by ID and type. Delta restoration
uses pinned binary copy/literal/patch commands and verifies the entire result.
The delta contains game-derived data and is not a public artifact.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import zipfile
import zlib

OBB_SHA = 'caba193e78eef5c4b2b4d4e5dd066306584ef151832e5f4c9f9d1ecfb4ca64fb'
CONFIG = {
    1: ('34a015813b4ea109bde686895d36d5d80c0c4a69a140afffc86d306e0043b833',
        '35ea532829bf7a3639406c5d514cc68779433b4485f5b27c1bd0ca7246d4fbac',
        'a8c72940f67acabb614f4e9a5c1cccab198f6b7a312960c738890795ecbb3be1', 'textures-manifest.json'),
    2: ('77d16b6174cf96eaf0565a5d555235de571de38dde7755096b09220e39f8d464',
        '44b4cb2976690124ab38f33fee67b88dced40acb1ae914786cd7337bee3e08b2',
        '831fee2261236f0ec9db20798e5b8f4c05bfb2baa786e26d9ed65b5acfb26b61', 'mesh-manifest.json'),
}
MAGIC = b'OBS1'
LIMIT = 256*1024*1024


def sha(data): return hashlib.sha256(data).hexdigest()
def file_sha(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def source(obb, index):
    if file_sha(obb) != OBB_SHA: raise ValueError('unexpected restored base OBB')
    with zipfile.ZipFile(obb) as z:
        names = z.namelist()
        if len(names) != len(set(names)): raise ValueError('duplicate OBB entry')
        prefix = f'assets/bin/Data/sharedassets{index}.assets.split'
        parts = sorted((n for n in names if n.startswith(prefix)), key=lambda n:int(n[len(prefix):]))
        if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))):
            raise ValueError('invalid asset fragments')
        data = b''.join(z.read(n) for n in parts)
    if sha(data) != CONFIG[index][0]: raise ValueError('unexpected source asset')
    return data


def create(obb, index, template, out):
    import UnityPy
    old = source(obb, index); target = (template/f'sharedassets{index}.assets').read_bytes()
    manifest = (template/CONFIG[index][3]).read_bytes()
    if sha(target) != CONFIG[index][1] or sha(manifest) != CONFIG[index][2]:
        raise ValueError('unexpected historical template')
    src = UnityPy.load(old); dst = UnityPy.load(target)
    source_objects = {(o.path_id, o.type.name):o for o in src.objects}
    if len(source_objects) != len(src.objects): raise ValueError('duplicate source object')
    objects = sorted(dst.objects, key=lambda o:o.byte_start)
    body = bytearray(MAGIC); count = 0; cursor = 0
    def literal(data):
        nonlocal count
        if data:
            body.extend(b'L'+struct.pack('<I', len(data))); body.extend(data); count += 1
    for obj in objects:
        start, length = obj.byte_start, obj.byte_size
        if start < cursor or start+length > len(target): raise ValueError('invalid target object layout')
        literal(target[cursor:start])
        previous = source_objects.get((obj.path_id, obj.type.name))
        if previous is None or previous.byte_size != length:
            literal(target[start:start+length])
        else:
            base = previous.byte_start
            if base+length > len(old): raise ValueError('invalid source object layout')
            body.extend(b'C'+struct.pack('<II', base, length)); count += 1
            before = memoryview(old)[base:base+length]; after = memoryview(target)[start:start+length]
            begin = None; last_difference = None
            for i, (x, y) in enumerate(zip(before, after)):
                if x != y:
                    if begin is None: begin = i
                    last_difference = i
                elif begin is not None and i-last_difference > 32:
                    data = after[begin:last_difference+1]; body.extend(b'P'+struct.pack('<II', start+begin, len(data))); body.extend(data); count += 1; begin = None
            if begin is not None:
                data = after[begin:last_difference+1]; body.extend(b'P'+struct.pack('<II', start+begin, len(data))); body.extend(data); count += 1
        cursor = start+length
        if len(body) > LIMIT: raise ValueError('delta unexpectedly large')
    literal(target[cursor:])
    encoded = zlib.compress(body, 9)
    out.mkdir(mode=0o700)
    try:
        (out/'delta.zlib').write_bytes(encoded); (out/CONFIG[index][3]).write_bytes(manifest)
        info = {'index':index,'source_sha256':CONFIG[index][0], 'target_sha256':CONFIG[index][1],
                'manifest_sha256':CONFIG[index][2], 'delta_sha256':sha(encoded), 'commands':count,
                'kind':'local research only, not for distribution'}
        (out/'report.json').write_text(json.dumps(info, indent=2)+'\n')
    except BaseException:
        shutil.rmtree(out); raise
    print(json.dumps(info, indent=2))


def restore(obb, index, delta, out, expected):
    old = source(obb, index); encoded = (delta/'delta.zlib').read_bytes(); manifest = (delta/CONFIG[index][3]).read_bytes()
    if sha(encoded) != expected or sha(manifest) != CONFIG[index][2]: raise ValueError('incompatible delta or manifest')
    dec = zlib.decompressobj(); body = dec.decompress(encoded, LIMIT+1)
    if len(body) > LIMIT or not dec.eof or dec.unused_data or dec.unconsumed_tail or body[:4] != MAGIC:
        raise ValueError('invalid compressed delta')
    result = bytearray(); pos = 4; count = 0
    while pos < len(body):
        op = body[pos:pos+1]; pos += 1; count += 1
        if count > 2_000_000: raise ValueError('too many delta commands')
        if op == b'L':
            if pos+4 > len(body): raise ValueError('truncated literal')
            size, = struct.unpack_from('<I', body, pos); pos += 4
            if not size or pos+size > len(body) or len(result)+size > LIMIT: raise ValueError('invalid literal')
            result.extend(body[pos:pos+size]); pos += size
        elif op in (b'C', b'P'):
            if pos+8 > len(body): raise ValueError('truncated range')
            start, size = struct.unpack_from('<II', body, pos); pos += 8
            if not size: raise ValueError('empty range')
            if op == b'C':
                if start+size > len(old) or len(result)+size > LIMIT: raise ValueError('invalid copy')
                result.extend(old[start:start+size])
            else:
                if start+size > len(result) or pos+size > len(body): raise ValueError('invalid patch')
                result[start:start+size] = body[pos:pos+size]; pos += size
        else: raise ValueError('unknown delta command')
    if sha(result) != CONFIG[index][1]: raise ValueError('restored template differs')
    out.mkdir(mode=0o700)
    try:
        (out/f'sharedassets{index}.assets').write_bytes(result); (out/CONFIG[index][3]).write_bytes(manifest)
    except BaseException:
        shutil.rmtree(out); raise
    print(f'Historical shared{index} template restored exactly; local research only.')


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('create','restore'))
    p.add_argument('obb',type=Path);p.add_argument('index',type=int,choices=(1,2));p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--delta-sha256')
    a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    for src in (a.obb,a.input):
        if a.output.resolve().is_relative_to(src.resolve()) or src.resolve().is_relative_to(a.output.resolve()):p.error('output must be separate from inputs')
    if a.action=='create':create(a.obb,a.index,a.input,a.output)
    else:
        if not a.delta_sha256:p.error('--delta-sha256 required')
        restore(a.obb,a.index,a.input,a.output,a.delta_sha256)


if __name__=='__main__':main()
