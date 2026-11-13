#!/usr/bin/env python3
"""Mask byte-identical serialized objects in a private research OBB.

Exact whole-object matches and matching shader fragments from local Steam files
are handled. Matching bytes do not establish provenance or distribution rights. The template keeps
other game data and must remain local. Generation requires UnityPy.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
BUNDLE_NAME = r'localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\.bundle'
SOURCE = re.compile(r'(?:sharedassets\d+\.assets|level\d+|resources\.assets|StreamingAssets/aa/Standalone(?:Linux|Windows)64/' + BUNDLE_NAME + r')\Z')
BUNDLE_ENTRY = re.compile(r'assets/aa/StandaloneLinux64/' + BUNDLE_NAME + r'\Z')
SINGLE = re.compile(r'(?:[0-9a-f]{32}|level\d+|resources\.assets|sharedassets\d+\.assets)\Z')
SPLIT = re.compile(r'((?:sharedassets\d+\.assets|level\d+|resources\.assets))\.split(0|[1-9]\d*)\Z')
MIN_OBJECT = 64
STEAM_INPUTS = ROOT / 'tools/launcher-steam-inputs.json'


def launcher_source_names():
    """Use only files present in both supported minimal launcher ZIPs."""
    platforms = json.loads(STEAM_INPUTS.read_text(encoding='utf-8'))['platforms']
    return {name for name in platforms['linux'].keys() & platforms['windows'].keys() if SOURCE.fullmatch(name)}


def launcher_localization_bundles(platform):
    platforms = json.loads(STEAM_INPUTS.read_text(encoding='utf-8'))['platforms']
    return {name for name in platforms[platform] if name.startswith('StreamingAssets/') and SOURCE.fullmatch(name)}


def sha(data): return hashlib.sha256(data).hexdigest()


def file_hash(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def new_output(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir(): raise ValueError('output must be new')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    return path


def asset_groups(zin):
    groups = defaultdict(list)
    for info in zin.infolist():
        if info.is_dir() or not info.filename.startswith(PREFIX): continue
        name = info.filename[len(PREFIX):]; match = SPLIT.fullmatch(name)
        if match: groups[match.group(1)].append((int(match.group(2)), info))
        elif SINGLE.fullmatch(name): groups[name].append((0, info))
    for name, parts in groups.items():
        parts.sort(key=lambda p: p[0])
        if [n for n, _ in parts] != list(range(len(parts))): raise ValueError('invalid split sequence')
        if sum(i.file_size for _, i in parts) > 256 * 1024 * 1024: raise ValueError('asset group exceeds limit')
    return {name: [info for _, info in parts] for name, parts in groups.items()}


def fragments(parts, offset, data, source, source_offset):
    if offset < 0 or source_offset < 0 or not data or not SOURCE.fullmatch(source): raise ValueError('invalid object range')
    rows = []; base = 0
    for part in parts:
        lo, hi = max(offset, base), min(offset + len(data), base + part.file_size)
        if lo < hi:
            pos = lo - offset
            rows.append({'entry': part.filename, 'target_offset': lo - base, 'size': hi - lo,
                         'source': source, 'source_offset': source_offset + pos, 'sha256': sha(data[pos:pos + hi - lo])})
        base += part.file_size
    if offset + len(data) > base: raise ValueError('object exceeds asset group')
    return rows


def common_fragments(target, sources):
    """Yield nonoverlapping exact matches; source and target offsets may differ."""
    index = {}
    for name, source_offset, data in sources:
        for offset in range(0, len(data) - 31, 4):
            block = data[offset:offset + 32]
            if block.count(0) < 24:
                index.setdefault(block, (name, source_offset, data, offset))
    offset = 0
    while offset + 32 <= len(target):
        match = index.get(target[offset:offset + 32])
        if match is None:
            offset += 4
            continue
        name, source_base, data, source_pos = match
        end = offset + 32
        while end < len(target) and source_pos + end - offset < len(data) and target[end] == data[source_pos + end - offset]:
            end += 1
        yield offset, target[offset:end], name, source_base + source_pos
        offset = (end + 3) // 4 * 4


def apply(raw, rows, streams=None):
    out = bytearray(raw); end = 0
    for row in sorted(rows, key=lambda r: r['target_offset']):
        start, size, source_offset = row['target_offset'], row['size'], row['source_offset']
        if (any(type(v) is not int for v in (start, size, source_offset)) or start < end or size <= 0 or
                start + size > len(out) or source_offset < 0 or not SOURCE.fullmatch(row['source'])): raise ValueError('invalid or overlapping object range')
        if streams is None:
            if sha(out[start:start + size]) != row['sha256']: raise ValueError('target object differs')
            out[start:start + size] = bytes(size)
        else:
            if any(out[start:start + size]): raise ValueError('object template not blank')
            stream = streams[row['source']]; stream.seek(source_offset); data = stream.read(size)
            if len(data) != size or sha(data) != row['sha256']: raise ValueError('Steam object differs')
            out[start:start + size] = data
        end = start + size
    return bytes(out)


def generate(args):
    import UnityPy
    source, original = args.steam_data.resolve(strict=True), args.obb.resolve(strict=True)
    output = new_output(args.output, (source, original)); targets = defaultdict(list); shader_targets = []; skipped = Counter()
    with zipfile.ZipFile(original) as zin:
        infos = zin.infolist()
        if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
        groups = asset_groups(zin)
        for name, parts in sorted(groups.items()):
            data = b''.join(zin.read(info) for info in parts)
            try: objects = UnityPy.load(data).objects
            except Exception: skipped['unparsed_target_groups'] += 1; continue
            if not objects: skipped['empty_target_groups'] += 1; continue
            for obj in objects:
                if obj.byte_size < MIN_OBJECT: continue
                raw = obj.get_raw_data(); offset = obj.byte_start
                if len(raw) != obj.byte_size or data[offset:offset + len(raw)] != raw: raise ValueError('target object bounds differ')
                if not any(raw): continue
                targets[(obj.type.name, len(raw), sha(raw))].append((name, offset))
                if obj.type.name in ('Shader', 'ComputeShader'):
                    try: shader_targets.append((obj.type.name, obj.read().m_Name, name, offset, raw))
                    except Exception: skipped['unreadable_target_shaders'] += 1
    allowed_sources = launcher_source_names()
    source_files = sorted(p for p in source.iterdir() if p.name in allowed_sources and p.is_file() and not p.is_symlink())
    if not source_files: raise ValueError('no Steam serialized assets')
    found = {}; shader_sources = defaultdict(list)
    for path in source_files:
        if path.stat().st_size > 256 * 1024 * 1024: raise ValueError('Steam asset exceeds limit')
        data = path.read_bytes()
        for obj in UnityPy.load(data).objects:
            if obj.byte_size < MIN_OBJECT: continue
            raw = obj.get_raw_data(); key = (obj.type.name, len(raw), sha(raw))
            if key in targets and key not in found:
                offset = obj.byte_start
                if data[offset:offset + len(raw)] != raw: raise ValueError('Steam object bounds differ')
                found[key] = (path.name, offset)
            if obj.type.name in ('Shader', 'ComputeShader'):
                try: name = obj.read().m_Name
                except Exception:
                    skipped['unreadable_source_shaders'] += 1
                    continue
                offset = obj.byte_start
                if data[offset:offset + len(raw)] != raw: raise ValueError('Steam shader bounds differ')
                shader_sources[(obj.type.name, name)].append((path.name, offset, raw))
    output.mkdir(mode=0o700); rows = []; counts = Counter(); shader_count = 0; bundle_count = 0; bundle_fragment_count = 0
    try:
        with zipfile.ZipFile(original) as zin:
            groups = asset_groups(zin)
            for key, (source_name, source_offset) in found.items():
                kind, size, digest = key
                with (source / source_name).open('rb') as stream:
                    stream.seek(source_offset); payload = stream.read(size)
                if len(payload) != size or sha(payload) != digest: raise ValueError('Steam object differs')
                for group, target_offset in targets[key]:
                    rows.extend(fragments(groups[group], target_offset, payload, source_name, source_offset)); counts[kind] += 1
            for kind, name, group, target_offset, raw in shader_targets:
                if (kind, len(raw), sha(raw)) in found: continue
                for offset, payload, source_name, source_offset in common_fragments(raw, shader_sources.get((kind, name), ())):
                    rows.extend(fragments(groups[group], target_offset + offset, payload, source_name, source_offset))
                    shader_count += 1
            handled_bundles = set()
            bundle_sources = sorted(launcher_localization_bundles('linux')) + sorted(launcher_localization_bundles('windows'))
            for source_name in bundle_sources:
                path = source
                for part in Path(source_name).parts:
                    path = path / part
                    if path.is_symlink(): raise ValueError('Steam localization path contains a symbolic link')
                entry = 'assets/aa/StandaloneLinux64/' + path.name
                if entry in handled_bundles or not path.is_file() or entry not in zin.namelist(): continue
                raw = path.read_bytes()
                if not raw or len(raw) > 32 * 1024 * 1024: continue
                target = zin.read(entry)
                if target == raw:
                    rows.append({'entry': entry, 'target_offset': 0, 'size': len(raw), 'source': source_name,
                                 'source_offset': 0, 'sha256': sha(raw)})
                    bundle_count += 1
                elif '/StandaloneWindows64/' in source_name:
                    for offset, payload, selected, source_offset in common_fragments(target, [(source_name, 0, raw)]):
                        rows.append({'entry': entry, 'target_offset': offset, 'size': len(payload), 'source': selected,
                                     'source_offset': source_offset, 'sha256': sha(payload)})
                        bundle_fragment_count += 1
                else: continue
                handled_bundles.add(entry)
            if not rows: raise ValueError('no exact object or shader matches')
            if len(rows) > 10000: raise ValueError('object plan exceeds runtime record limit')
            grouped = defaultdict(list)
            for row in rows: grouped[row['entry']].append(row)
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in zin.infolist():
                    raw = zin.read(info)
                    zout.writestr(info, apply(raw, grouped[info.filename]) if info.filename in grouped else raw)
        used = sorted({r['source'] for r in rows})
        if len(used) > 32: raise ValueError('object plan exceeds runtime source limit')
        plan = {'schema': 1, 'kind': 'experimental-exact-steam-object-offsets',
                'template_sha256': file_hash(output / 'template.obb'), 'sources': {n: file_hash(source / n) for n in used},
                'matched_objects_by_type': dict(counts), 'matched_shader_fragments': shader_count, 'matched_localization_bundles': bundle_count,
                'matched_localization_fragments': bundle_fragment_count,
                'skipped': dict(skipped), 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output); raise
    print(f'Masked {sum(r["size"] for r in rows)} bytes from {sum(counts.values())} objects, {shader_count} shader fragments, {bundle_count} localization bundles and {bundle_fragment_count} localization fragments; template still contains game data.')
    print('Object types:', dict(counts), '; skipped groups:', dict(skipped))


def restore(args):
    from contextlib import ExitStack
    source, template, plan_file = (p.resolve(strict=True) for p in (args.steam_data, args.template, args.plan))
    output = new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-exact-steam-object-offsets' or plan.get('template_sha256') != file_hash(template): raise ValueError('plan or template differs')
    rows, hashes = plan.get('records'), plan.get('sources')
    if not isinstance(rows, list) or not rows or len(rows) > 10000 or not isinstance(hashes, dict) or not hashes: raise ValueError('invalid plan')
    if any(not SOURCE.fullmatch(n) or not (source / n).is_file() or (source / n).is_symlink() or file_hash(source / n) != h for n, h in hashes.items()): raise ValueError('Steam source differs')
    grouped = defaultdict(list)
    for row in rows:
        entry = row.get('entry', '')
        if row.get('source') not in hashes or not (BUNDLE_ENTRY.fullmatch(entry) or (entry.startswith(PREFIX) and (SINGLE.fullmatch(entry[len(PREFIX):]) or SPLIT.fullmatch(entry[len(PREFIX):])))): raise ValueError('invalid object entry')
        grouped[entry].append(row)
    created = False
    try:
        with ExitStack() as stack:
            zin = stack.enter_context(zipfile.ZipFile(template)); infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            streams = {name: stack.enter_context((source / name).open('rb')) for name in hashes}
            target = stack.enter_context(output.open('xb')); created = True
            zout = stack.enter_context(zipfile.ZipFile(target, 'w')); seen = set()
            for info in infos:
                raw = zin.read(info)
                if info.filename in grouped: raw = apply(raw, grouped[info.filename], streams); seen.add(info.filename)
                zout.writestr(info, raw)
            if seen != set(grouped): raise ValueError('missing object entries')
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} exact object fragments; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='command', required=True)
    for command in ('generate', 'restore'):
        s = sub.add_parser(command); s.add_argument('--steam-data', type=Path, required=True); s.add_argument('--output', type=Path, required=True)
        if command == 'generate': s.add_argument('--obb', type=Path, required=True)
        else: s.add_argument('--template', type=Path, required=True); s.add_argument('--plan', type=Path, required=True)
    args = p.parse_args(); (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__': main()
