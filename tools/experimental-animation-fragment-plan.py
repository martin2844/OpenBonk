#!/usr/bin/env python3
"""Mask matching Steam animation bytes in standalone and split Unity assets.

Only equal nonzero bytes are masked; unmatched animation bytes and other game
data remain in the template. Inputs and output must stay local. Generation
requires UnityPy. This is not a distributable build or proof of fidelity.
"""
import argparse
import base64
from collections import Counter, defaultdict
import importlib.util
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('animation_masks', ROOT / 'tools/experimental-animation-mask.py')
animation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(animation)
spec2 = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec2)
spec2.loader.exec_module(study)
ENTRY = re.compile(re.escape(study.PREFIX) + r'(?:[0-9a-f]{32}|sharedassets[12]\.assets\.split(?:0|[1-9]\d*))\Z')


def make_fragments(parts, target_offset, target, source_name, source_offset, source):
    if target_offset < 0 or source_offset < 0 or len(target) != len(source) or not target: raise ValueError('invalid animation range')
    rows, base = [], 0
    for info in parts:
        lo, hi = max(target_offset, base), min(target_offset + len(target), base + info.file_size)
        if lo < hi:
            start, size = lo - target_offset, hi - lo
            original = target[start:start + size]; src = source[start:start + size]
            masked, bits, count = animation.mask_equal(original, src)
            if count:
                rows.append({'entry': info.filename, 'target_offset': lo - base, 'size': size,
                             'source': source_name, 'source_offset': source_offset + start,
                             'original_sha256': animation.digest(original), 'masked_sha256': animation.digest(masked),
                             'mask': base64.b64encode(bits).decode('ascii'), 'masked_bytes': count})
        base += info.file_size
    if target_offset + len(target) > base: raise ValueError('animation exceeds splits')
    return rows


def apply(raw, rows, sources, restore=False):
    out = bytearray(raw); end = 0
    for row in sorted(rows, key=lambda r: r['target_offset']):
        offset, size, source_offset = row['target_offset'], row['size'], row['source_offset']
        if (any(type(v) is not int for v in (offset, size, source_offset)) or offset < end or size <= 0 or offset + size > len(out) or
                source_offset < 0 or row['source'] not in sources or source_offset + size > len(sources[row['source']])):
            raise ValueError('invalid or overlapping animation ranges')
        src = sources[row['source']][source_offset:source_offset + size]
        bits = base64.b64decode(row['mask'], validate=True)
        if len(bits) != (size + 7) // 8 or (size % 8 and bits[-1] >> (size % 8)) or sum(x.bit_count() for x in bits) != row['masked_bytes']:
            raise ValueError('invalid animation mask')
        current = bytes(out[offset:offset + size])
        if restore:
            if animation.digest(current) != row['masked_sha256']: raise ValueError('masked animation differs')
            result = animation.restore_masked(current, src, bits)
            if animation.digest(result) != row['original_sha256']: raise ValueError('restored animation differs')
        else:
            if animation.digest(current) != row['original_sha256']: raise ValueError('original animation differs')
            result, actual_bits, count = animation.mask_equal(current, src)
            if actual_bits != bits or count != row['masked_bytes'] or animation.digest(result) != row['masked_sha256']: raise ValueError('animation mask differs')
        out[offset:offset + size] = result; end = offset + size
    return bytes(out)


def generate(args):
    import UnityPy
    source, original = args.steam_data.resolve(strict=True), args.obb.resolve(strict=True)
    output = animation.regular_output(args.output, (source, original)); sources = animation.read_sources(source); index = animation.source_index(sources)
    rows = []; outcomes = Counter(); output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(original) as zin:
            infos = animation.zip_entries(zin)
            groups = [(study.parts(zin, group), group) for group in study.GROUPS]
            groups += [([info], info.filename) for info in infos if re.fullmatch(re.escape(study.PREFIX) + r'[0-9a-f]{32}', info.filename)]
            for parts, _ in groups:
                data = b''.join(zin.read(info) for info in parts)
                try: objects = UnityPy.load(data).objects
                except Exception: continue
                for obj in objects:
                    if obj.type.name != 'AnimationClip': continue
                    target = obj.get_raw_data(); offset = obj.byte_start
                    if data[offset:offset + len(target)] != target: raise ValueError('animation target bounds differ')
                    candidates = index.get((obj.read().m_Name, len(target)), [])
                    if not candidates: outcomes['unmatched'] += 1; continue
                    scored = [(sum(a == b for a, b in zip(target, c[3])), c) for c in candidates]
                    score = max(s for s, _ in scored); matches = [c for s, c in scored if s == score]
                    if len({c[3] for c in matches}) != 1: outcomes['ambiguous'] += 1; continue
                    source_name, source_id, source_offset, src = sorted(matches, key=lambda c: (c[0], c[1]))[0]
                    fragments = make_fragments(parts, offset, target, source_name, source_offset, src)
                    if not fragments: outcomes['no_matching_bytes'] += 1; continue
                    rows.extend(fragments); outcomes['matched'] += 1
            if not rows: raise ValueError('no matching animation bytes')
            grouped = defaultdict(list)
            for row in rows: grouped[row['entry']].append(row)
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    zout.writestr(info, apply(raw, grouped[info.filename], sources) if info.filename in grouped else raw)
        plan = {'schema': 1, 'kind': 'experimental-animation-fragment-mask',
                'template_sha256': animation.file_digest(output / 'template.obb'),
                'sources': {name: animation.digest(data) for name, data in sources.items()},
                'outcomes': dict(outcomes), 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output); raise
    print(f'Masked {sum(r["masked_bytes"] for r in rows)} nonzero bytes across {outcomes["matched"]} clips; other game data remains.')
    print('Not selected:', {k: v for k, v in outcomes.items() if k != 'matched'})


def restore(args):
    source, template, plan_file = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan))
    output = animation.regular_output(args.output, (source, template, plan_file)); sources = animation.read_sources(source)
    plan = json.loads(plan_file.read_text())
    if (plan.get('schema') != 1 or plan.get('kind') != 'experimental-animation-fragment-mask' or
            plan.get('template_sha256') != animation.file_digest(template) or
            plan.get('sources') != {name: animation.digest(data) for name, data in sources.items()}): raise ValueError('plan or input differs')
    rows = plan.get('records')
    if not isinstance(rows, list) or not rows or len(rows) > 5000: raise ValueError('invalid animation plan')
    grouped = defaultdict(list)
    for row in rows:
        if not ENTRY.fullmatch(row.get('entry', '')) or row.get('source') not in sources: raise ValueError('invalid animation record')
        grouped[row['entry']].append(row)
    created = False
    try:
        with zipfile.ZipFile(template) as zin, output.open('xb') as target:
            created = True; infos = animation.zip_entries(zin); seen = set()
            with zipfile.ZipFile(target, 'w') as zout:
                for info in infos:
                    raw = zin.read(info)
                    if info.filename in grouped: raw = apply(raw, grouped[info.filename], sources, restore=True); seen.add(info.filename)
                    zout.writestr(info, raw)
            if seen != set(grouped): raise ValueError('missing animation entries')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} animation fragments; output remains private.')


def main():
    parser = argparse.ArgumentParser(description=__doc__); sub = parser.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        p = sub.add_parser(command); p.add_argument('--steam-data', type=Path, required=True); p.add_argument('--output', type=Path, required=True)
        if command == 'generate': p.add_argument('--obb', type=Path, required=True)
        else: p.add_argument('--template', type=Path, required=True); p.add_argument('--plan', type=Path, required=True)
    args = parser.parse_args(); (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
