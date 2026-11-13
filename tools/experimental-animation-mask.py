#!/usr/bin/env python3
"""Locally mask and restore matching animation bytes using Steam data.

The resulting template still contains game-derived data and must not be
distributed. This is an experiment, not the launcher's conversion plan.
Requires UnityPy; the output is specific to both inputs.
"""
import argparse
import base64
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile


PREFIX = 'assets/bin/Data/'
SOURCE_NAMES = ('sharedassets1.assets', 'sharedassets2.assets')
ROOT = Path(__file__).resolve().parent.parent


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def mask_equal(target, source):
    if len(target) != len(source):
        raise ValueError('animation sizes differ')
    bits = bytearray((len(target) + 7) // 8)
    result = bytearray(target)
    count = 0
    for i, (a, b) in enumerate(zip(target, source)):
        if a and a == b:
            bits[i // 8] |= 1 << (i % 8)
            result[i] = 0
            count += 1
    return bytes(result), bytes(bits), count


def restore_masked(target, source, mask):
    if len(target) != len(source) or len(mask) != (len(target) + 7) // 8:
        raise ValueError('invalid mask dimensions')
    if len(target) % 8 and mask[-1] >> (len(target) % 8):
        raise ValueError('nonzero mask padding')
    result = bytearray(target)
    for i, b in enumerate(source):
        if mask[i // 8] & (1 << (i % 8)):
            if target[i] != 0 or b == 0:
                raise ValueError('invalid masked byte')
            result[i] = b
    return bytes(result)


def regular_output(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir():
        raise ValueError('output must be new in an existing directory')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)):
        raise ValueError('output must be separate from inputs')
    return path


def source_index(data):
    import UnityPy
    index = defaultdict(list)
    for name, raw in data.items():
        for obj in UnityPy.load(raw).objects:
            if obj.type.name != 'AnimationClip':
                continue
            clip = obj.get_raw_data()
            offset = obj.byte_start
            if raw[offset:offset + len(clip)] != clip:
                raise ValueError('source object bounds differ')
            index[(obj.read().m_Name, len(clip))].append((name, obj.path_id, offset, clip))
    return index


def read_sources(folder):
    data = {}
    for name in SOURCE_NAMES:
        path = folder / name
        if not path.is_file() or path.is_symlink():
            raise ValueError('missing regular Steam input: ' + name)
        data[name] = path.read_bytes()
    return data


def zip_entries(z):
    entries = z.infolist()
    names = [x.filename for x in entries]
    if len(names) != len(set(names)):
        raise ValueError('duplicate ZIP entry')
    return entries


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True)
    original = args.obb.resolve(strict=True)
    out = regular_output(args.output, (source, original))
    data = read_sources(source)
    index = source_index(data)
    records = []
    out.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(original) as zin, zipfile.ZipFile(out / 'template.obb', 'x') as zout:
            for info in zip_entries(zin):
                raw = zin.read(info)
                if re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', info.filename):
                    try:
                        objects = UnityPy.load(raw).objects
                    except Exception:
                        objects = []
                    clips = [obj for obj in objects if obj.type.name == 'AnimationClip']
                    if len(clips) > 1:
                        raise ValueError('multiple animation clips in one target')
                    if clips:
                        obj = clips[0]
                        clip = obj.get_raw_data()
                        offset = obj.byte_start
                        if raw[offset:offset + len(clip)] != clip:
                            raise ValueError('target object bounds differ')
                        candidates = index.get((obj.read().m_Name, len(clip)), [])
                        if not candidates:
                            raise ValueError('no matching Steam clip')
                        scored = [(sum(a == b for a, b in zip(clip, c[3])), c) for c in candidates]
                        best = max(score for score, _ in scored)
                        matches = [c for score, c in scored if score == best]
                        if len({c[3] for c in matches}) != 1:
                            raise ValueError('ambiguous Steam clip')
                        source_name, source_id, source_offset, source_clip = sorted(matches, key=lambda c: (c[0], c[1]))[0]
                        scrubbed, bits, count = mask_equal(clip, source_clip)
                        modified = raw[:offset] + scrubbed + raw[offset + len(clip):]
                        records.append({'entry': info.filename, 'target_offset': offset, 'size': len(clip),
                                        'source': source_name, 'source_id': source_id, 'source_offset': source_offset,
                                        'original_sha256': digest(raw), 'template_sha256': digest(modified),
                                        'mask': base64.b64encode(bits).decode('ascii'), 'masked_bytes': count})
                        raw = modified
                zout.writestr(info, raw)
        if not records:
            raise ValueError('no animation clips found')
        plan = {'schema': 1, 'kind': 'experimental-animation-equal-byte-mask',
                'sources': {name: digest(raw) for name, raw in data.items()},
                'template_sha256': file_digest(out / 'template.obb'), 'records': records}
        (out / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(out)
        raise
    print(f"Masked {sum(r['masked_bytes'] for r in records)} bytes in {len(records)} clips; template still contains game data.")


def restore(args):
    source = args.steam_data.resolve(strict=True)
    template = args.template.resolve(strict=True)
    plan_path = args.plan.resolve(strict=True)
    out = regular_output(args.output, (source, template, plan_path))
    data = read_sources(source)
    plan = json.loads(plan_path.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-animation-equal-byte-mask':
        raise ValueError('invalid plan')
    if plan.get('sources') != {name: digest(raw) for name, raw in data.items()}:
        raise ValueError('Steam input differs')
    if plan.get('template_sha256') != file_digest(template):
        raise ValueError('template archive differs')
    records = {r['entry']: r for r in plan['records']}
    if len(records) != len(plan['records']) or not records:
        raise ValueError('invalid records')
    seen = set()
    try:
        with zipfile.ZipFile(template) as zin, zipfile.ZipFile(out, 'x') as zout:
            for info in zip_entries(zin):
                raw = zin.read(info)
                if info.filename in records:
                    row = records[info.filename]
                    if digest(raw) != row['template_sha256'] or row['source'] not in data:
                        raise ValueError('template entry differs')
                    offset, size, source_offset = row['target_offset'], row['size'], row['source_offset']
                    steam = data[row['source']]
                    if size <= 0 or offset < 0 or offset + size > len(raw) or source_offset < 0 or source_offset + size > len(steam):
                        raise ValueError('invalid range')
                    bits = base64.b64decode(row['mask'], validate=True)
                    if sum(b.bit_count() for b in bits) != row['masked_bytes']:
                        raise ValueError('mask count differs')
                    clip = restore_masked(raw[offset:offset + size], steam[source_offset:source_offset + size], bits)
                    raw = raw[:offset] + clip + raw[offset + size:]
                    if digest(raw) != row['original_sha256']:
                        raise ValueError('restored entry differs')
                    seen.add(info.filename)
                zout.writestr(info, raw)
        if seen != records.keys():
            raise ValueError('missing template entries')
    except BaseException:
        out.unlink(missing_ok=True)
        raise
    print(f'Restored and verified {len(seen)} animation entries; output remains local.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest='command', required=True)
    make = sub.add_parser('generate')
    make.add_argument('--steam-data', type=Path, required=True)
    make.add_argument('--obb', type=Path, required=True)
    make.add_argument('--output', type=Path, required=True)
    recover = sub.add_parser('restore')
    recover.add_argument('--steam-data', type=Path, required=True)
    recover.add_argument('--template', type=Path, required=True)
    recover.add_argument('--plan', type=Path, required=True)
    recover.add_argument('--output', type=Path, required=True)
    args = p.parse_args()
    (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__':
    main()
