#!/usr/bin/env python3
"""Mask and restore exact Steam texture payloads in a local research OBB.

Only uniquely matched, byte-identical inline target pixels are handled.
Templates and restored OBBs still contain game data and must remain local.
Generation requires UnityPy; this is not an Android conversion plan.
"""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
GROUPS = ('sharedassets1', 'sharedassets2')
SOURCE = re.compile(r'sharedassets[12]\.assets(?:\.resS)?\Z')


def sha(data): return hashlib.sha256(data).hexdigest()


def file_hash(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()


def new_output(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir(): raise ValueError('output must be new')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    return path


def split_infos(zin, group):
    prefix = PREFIX + group + '.assets.split'
    found = []
    for info in zin.infolist():
        if info.filename.startswith(prefix):
            suffix = info.filename[len(prefix):]
            if not suffix.isdecimal() or str(int(suffix)) != suffix: raise ValueError('invalid split name')
            found.append((int(suffix), info))
    found.sort(key=lambda x: x[0])
    if not found or [n for n, _ in found] != list(range(len(found))): raise ValueError('invalid split sequence')
    return [info for _, info in found]


def fragments(parts, target, size, source, source_offset, payload):
    """Translate one serialized-file range into ZIP-entry ranges."""
    if target < 0 or size <= 0 or source_offset < 0 or len(payload) != size: raise ValueError('invalid texture range')
    result = []
    base = 0
    for part in parts:
        lo, hi = max(target, base), min(target + size, base + part.file_size)
        if lo < hi:
            offset = lo - target
            result.append({'entry': part.filename, 'target_offset': lo - base, 'source': source,
                           'source_offset': source_offset + offset, 'size': hi - lo,
                           'sha256': sha(payload[offset:offset + hi - lo])})
        base += part.file_size
    if sum(r['size'] for r in result) != size: raise ValueError('texture exceeds splits')
    return result


def apply(raw, rows, streams=None):
    out = bytearray(raw)
    previous = 0
    for row in sorted(rows, key=lambda r: r['target_offset']):
        start, size, offset = row['target_offset'], row['size'], row['source_offset']
        if any(type(x) is not int for x in (start, size, offset)) or start < previous or size <= 0 or start + size > len(out) or offset < 0 or not SOURCE.fullmatch(row['source']):
            raise ValueError('invalid or overlapping texture range')
        if streams is None:
            if sha(out[start:start + size]) != row['sha256']: raise ValueError('target texture differs')
            out[start:start + size] = bytes(size)
        else:
            if any(out[start:start + size]): raise ValueError('texture template not blank')
            stream = streams[row['source']]
            stream.seek(offset)
            data = stream.read(size)
            if len(data) != size or sha(data) != row['sha256']: raise ValueError('Steam texture differs')
            out[start:start + size] = data
        previous = start + size
    return bytes(out)


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True)
    obb = args.obb.resolve(strict=True)
    output = new_output(args.output, (source, obb))
    index = defaultdict(list)
    for group in GROUPS:
        path = source / (group + '.assets')
        if not path.is_file() or path.is_symlink(): raise ValueError('missing Steam asset file')
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name == 'Texture2D':
                tex = obj.read()
                key = (tex.m_Name, tex.m_Width, tex.m_Height, tex.m_TextureFormat, tex.m_MipCount)
                index[key].append((group, obj, tex))
    rows = []
    textures = 0
    output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            for group in GROUPS:
                parts = split_infos(zin, group)
                asset = b''.join(zin.read(part) for part in parts)
                for obj in UnityPy.load(asset).objects:
                    if obj.type.name != 'Texture2D': continue
                    tex = obj.read()
                    payload = bytes(tex.image_data)
                    if not payload: continue
                    body = obj.get_raw_data()
                    position = body.find(payload)
                    if position < 0 or body.count(payload) != 1: continue
                    key = (tex.m_Name, tex.m_Width, tex.m_Height, tex.m_TextureFormat, tex.m_MipCount)
                    matches = []
                    for source_group, other, candidate in index[key]:
                        if bytes(candidate.get_image_data()) != payload: continue
                        data = other.get_raw_data()
                        stream = candidate.m_StreamData
                        if stream.path:
                            filename, offset = stream.path, stream.offset
                            if not SOURCE.fullmatch(filename) or stream.size != len(payload): continue
                        else:
                            pos = data.find(payload)
                            if pos < 0 or data.count(payload) != 1: continue
                            filename, offset = source_group + '.assets', other.byte_start + pos
                        path = source / filename
                        if not path.is_file() or path.is_symlink(): raise ValueError('missing Steam texture input')
                        with path.open('rb') as f:
                            f.seek(offset)
                            if f.read(len(payload)) != payload: raise ValueError('Steam payload offset differs')
                        matches.append((filename, offset))
                    matches = sorted(set(matches))
                    if len(matches) != 1: continue
                    filename, offset = matches[0]
                    rows.extend(fragments(parts, obj.byte_start + position, len(payload), filename, offset, payload))
                    textures += 1
            if not rows: raise ValueError('no uniquely matched exact texture payloads')
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    selected = [r for r in rows if r['entry'] == info.filename]
                    zout.writestr(info, apply(raw, selected) if selected else raw)
        used = sorted({r['source'] for r in rows})
        plan = {'schema': 1, 'kind': 'experimental-exact-texture-offsets',
                'sources': {name: file_hash(source / name) for name in used},
                'template_sha256': file_hash(output / 'template.obb'), 'textures': textures, 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Masked {sum(r["size"] for r in rows)} bytes across {textures} textures; template still contains game data.')


def restore(args):
    from contextlib import ExitStack
    source, template, plan_file = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan))
    output = new_output(args.output, (source, template, plan_file))
    plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-exact-texture-offsets' or plan.get('template_sha256') != file_hash(template): raise ValueError('plan or template differs')
    rows, hashes = plan.get('records'), plan.get('sources')
    if not isinstance(rows, list) or not rows or not isinstance(hashes, dict) or not hashes: raise ValueError('invalid plan')
    if any(not SOURCE.fullmatch(name) or not (source / name).is_file() or (source / name).is_symlink() or file_hash(source / name) != value for name, value in hashes.items()): raise ValueError('Steam input differs')
    if any(r.get('source') not in hashes or not re.fullmatch(re.escape(PREFIX) + r'sharedassets[12]\.assets\.split(?:0|[1-9]\d*)', r.get('entry', '')) for r in rows): raise ValueError('invalid plan entry')
    created = False
    try:
        with ExitStack() as stack:
            zin = stack.enter_context(zipfile.ZipFile(template))
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate ZIP entry')
            streams = {name: stack.enter_context((source / name).open('rb')) for name in hashes}
            target = stack.enter_context(output.open('xb'))
            created = True
            zout = stack.enter_context(zipfile.ZipFile(target, 'w'))
            seen = set()
            for info in infos:
                raw = zin.read(info)
                selected = [r for r in rows if r['entry'] == info.filename]
                if selected:
                    raw = apply(raw, selected, streams)
                    seen.add(info.filename)
                zout.writestr(info, raw)
            if seen != {r['entry'] for r in rows}: raise ValueError('missing template entries')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} texture fragments; output still contains game data.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        s = sub.add_parser(command)
        s.add_argument('--steam-data', type=Path, required=True)
        s.add_argument('--output', type=Path, required=True)
        if command == 'generate': s.add_argument('--obb', type=Path, required=True)
        else:
            s.add_argument('--template', type=Path, required=True)
            s.add_argument('--plan', type=Path, required=True)
    args = p.parse_args()
    (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
