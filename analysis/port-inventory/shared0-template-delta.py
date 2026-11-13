#!/usr/bin/env python3
"""Reconstruct a historical local template from the restored research base.

The compact delta contains game-derived metadata and is not a public artifact.
Both source and restored output are pinned. This does not establish provenance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import tempfile
import zipfile
import zlib

SOURCE_SHA = '69ae7f8ccd1048553ac88cb0cfd86d599f1e772bc2b443c0263af5e1890ee844'
TARGET_SHA = '3040fe95e6ddc94305c7418164e22e6c8a16470d8128fdbf0d6b9dbc0c0aa989'
MANIFEST_SHA = 'e2b44b36a3d60d315f7e84861f5f0c84f71e4300894b3ee7d4f4353c09e40838'
APK_SHA = 'f756235db25a5aed06afb34ec7e0267aa49cf770eb148e0e3cd048674b13e93a'
MAGIC = b'OBD1'


def sha(data): return hashlib.sha256(data).hexdigest()
def file_sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def source(apk):
    if file_sha(apk) != APK_SHA: raise ValueError('unexpected restored base APK')
    with zipfile.ZipFile(apk) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)): raise ValueError('duplicate APK entry')
        prefix = 'assets/bin/Data/sharedassets0.assets.split'
        parts = sorted((n for n in names if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
        if [int(n[len(prefix):]) for n in parts] != list(range(len(parts))) or not parts:
            raise ValueError('invalid asset fragments')
        result = b''.join(archive.read(name) for name in parts)
    if sha(result) != SOURCE_SHA: raise ValueError('unexpected source asset')
    return result


def create(apk, template, output):
    original = source(apk); target = (template/'sharedassets0.assets').read_bytes()
    manifest = (template/'textures-manifest.json').read_bytes()
    if len(target) != len(original) or sha(target) != TARGET_SHA or sha(manifest) != MANIFEST_SHA:
        raise ValueError('unexpected historical template')
    runs = []
    start = None
    for i, (before, after) in enumerate(zip(original, target)):
        if before != after and start is None: start = i
        elif before == after and start is not None:
            runs.append((start, target[start:i])); start = None
    if start is not None: runs.append((start, target[start:]))
    body = bytearray(MAGIC + struct.pack('<I', len(runs)))
    for start, data in runs:
        body.extend(struct.pack('<II', start, len(data))); body.extend(data)
    if len(body) > 8*1024*1024: raise ValueError('delta unexpectedly large')
    output.mkdir(mode=0o700)
    try:
        (output/'delta.zlib').write_bytes(zlib.compress(body, 9))
        (output/'textures-manifest.json').write_bytes(manifest)
        info = {'source_sha256':SOURCE_SHA, 'target_sha256':TARGET_SHA,
                'delta_sha256':sha((output/'delta.zlib').read_bytes()),
                'manifest_sha256':MANIFEST_SHA, 'runs':len(runs),
                'kind':'local research only, not for distribution'}
        (output/'report.json').write_text(json.dumps(info, indent=2)+'\n')
    except BaseException:
        shutil.rmtree(output); raise
    print(json.dumps(info, indent=2))


def restore(apk, delta_dir, output, expected_delta):
    original = source(apk); encoded = (delta_dir/'delta.zlib').read_bytes()
    manifest = (delta_dir/'textures-manifest.json').read_bytes()
    if sha(encoded) != expected_delta or sha(manifest) != MANIFEST_SHA:
        raise ValueError('incompatible delta or manifest')
    dec = zlib.decompressobj(); body = dec.decompress(encoded, 8*1024*1024+1)
    if len(body) > 8*1024*1024 or not dec.eof or dec.unused_data or dec.unconsumed_tail:
        raise ValueError('invalid compressed delta')
    if len(body) < 8 or body[:4] != MAGIC: raise ValueError('invalid delta header')
    count, = struct.unpack_from('<I', body, 4); position = 8; end = 0
    if count > 200000: raise ValueError('too many delta ranges')
    result = bytearray(original)
    for _ in range(count):
        if position+8 > len(body): raise ValueError('truncated delta')
        start, length = struct.unpack_from('<II', body, position); position += 8
        if start < end or not length or start+length > len(result) or position+length > len(body):
            raise ValueError('invalid delta range')
        result[start:start+length] = body[position:position+length]
        position += length; end = start+length
    if position != len(body) or sha(result) != TARGET_SHA: raise ValueError('restored template differs')
    output.mkdir(mode=0o700)
    try:
        (output/'sharedassets0.assets').write_bytes(result)
        (output/'textures-manifest.json').write_bytes(manifest)
    except BaseException:
        shutil.rmtree(output); raise
    print('Historical shared0 template restored exactly; local research only.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('create', 'restore')); p.add_argument('apk', type=Path)
    p.add_argument('input', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--delta-sha256')
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new')
    for src in (a.apk, a.input):
        if a.output.resolve().is_relative_to(src.resolve()) or src.resolve().is_relative_to(a.output.resolve()):
            p.error('output must be separate from inputs')
    if a.action == 'create': create(a.apk, a.input, a.output)
    else:
        if not a.delta_sha256: p.error('--delta-sha256 required for restore')
        restore(a.apk, a.input, a.output, a.delta_sha256)


if __name__ == '__main__': main()
