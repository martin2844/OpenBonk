#!/usr/bin/env python3
"""Generate or restore offset-only Steam audio plans for a local Android OBB.

The template retains other game-derived data and must stay local. Requires
UnityPy for generation. This is not integrated into the Android launcher.
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


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def source_files(folder):
    asset, resource = folder / 'sharedassets1.assets', folder / 'sharedassets1.resource'
    if any(not p.is_file() or p.is_symlink() for p in (asset, resource)):
        raise ValueError('Steam audio inputs must be regular files')
    return asset, resource


def output_path(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir():
        raise ValueError('output must be a new path in an existing directory')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)):
        raise ValueError('output must be separate from the inputs and source checkout')
    return path


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True)
    original = args.obb.resolve(strict=True)
    out = output_path(args.output, (source, original))
    asset, resource = source_files(source)
    index = defaultdict(list)
    for obj in UnityPy.load(str(asset)).objects:
        if obj.type.name == 'AudioClip':
            clip = obj.read()
            index[clip.m_Name].append(clip)
    records = []
    out.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(original) as zin, resource.open('rb') as steam, zipfile.ZipFile(out / 'template.obb', 'x') as zout:
            infos = zin.infolist()
            names = {info.filename for info in infos}
            if len(infos) != len(names):
                raise ValueError('duplicate archive entries')
            by_resource = {}
            for info in infos:
                name = info.filename
                if not re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', name) or name + '.resource' not in names:
                    continue
                clips = [obj.read() for obj in UnityPy.load(zin.read(name)).objects if obj.type.name == 'AudioClip']
                if len(clips) != 1 or len(index[clips[0].m_Name]) != 1:
                    raise ValueError('audio mapping is missing or ambiguous')
                target, original_clip = clips[0], index[clips[0].m_Name][0]
                src, dst = original_clip.m_Resource, target.m_Resource
                payload = zin.read(name + '.resource')
                if (src.m_Source != 'sharedassets1.resource' or src.m_Offset < 0 or src.m_Size <= 0 or
                        dst.m_Offset != 0 or dst.m_Size != src.m_Size or len(payload) != src.m_Size or
                        not dst.m_Source.endswith(name.rsplit('/', 1)[-1] + '.resource')):
                    raise ValueError('unsupported audio reference')
                steam.seek(src.m_Offset)
                if steam.read(src.m_Size) != payload:
                    raise ValueError('OBB audio differs from Steam')
                row = {'entry': name + '.resource', 'source_offset': src.m_Offset,
                       'size': src.m_Size, 'sha256': digest(payload)}
                records.append(row)
                by_resource[row['entry']] = row
            if not records:
                raise ValueError('no audio resources found')
            for info in infos:
                raw = zin.read(info)
                if info.filename in by_resource:
                    raw = bytes(len(raw))
                zout.writestr(info, raw)
        plan = {'schema': 1, 'kind': 'experimental-steam-audio-offsets',
                'asset_sha256': file_digest(asset), 'resource_sha256': file_digest(resource),
                'template_sha256': file_digest(out / 'template.obb'), 'records': records}
        (out / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(out)
        raise
    print(f'Generated local plan for {len(records)} audio resources; template still contains other game data.')


def restore(args):
    source = args.steam_data.resolve(strict=True)
    template = args.template.resolve(strict=True)
    plan_path = args.plan.resolve(strict=True)
    output = output_path(args.output, (source, template, plan_path))
    asset, resource = source_files(source)
    plan = json.loads(plan_path.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-steam-audio-offsets':
        raise ValueError('invalid plan')
    if (plan.get('asset_sha256') != file_digest(asset) or plan.get('resource_sha256') != file_digest(resource) or
            plan.get('template_sha256') != file_digest(template)):
        raise ValueError('input differs from plan')
    records = {r['entry']: r for r in plan['records']}
    if not records or len(records) != len(plan['records']):
        raise ValueError('duplicate or missing plan records')
    seen = set()
    created = False
    try:
        with zipfile.ZipFile(template) as zin, resource.open('rb') as steam, output.open('xb') as stream:
            created = True
            infos = zin.infolist()
            if len(infos) != len({info.filename for info in infos}):
                raise ValueError('duplicate archive entries')
            with zipfile.ZipFile(stream, 'w') as zout:
                for info in infos:
                    raw = zin.read(info)
                    if info.filename in records:
                        row = records[info.filename]
                        if row['size'] <= 0 or row['source_offset'] < 0 or len(raw) != row['size'] or any(raw):
                            raise ValueError('invalid masked resource')
                        steam.seek(row['source_offset'])
                        raw = steam.read(row['size'])
                        if len(raw) != row['size'] or digest(raw) != row['sha256']:
                            raise ValueError('Steam audio differs from plan')
                        seen.add(info.filename)
                    zout.writestr(info, raw)
        if seen != records.keys():
            raise ValueError('missing audio resources')
    except BaseException:
        if created:
            output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(seen)} local audio resources. Output still contains game data.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    generate_parser = sub.add_parser('generate')
    generate_parser.add_argument('--steam-data', type=Path, required=True)
    generate_parser.add_argument('--obb', type=Path, required=True)
    generate_parser.add_argument('--output', type=Path, required=True)
    restore_parser = sub.add_parser('restore')
    restore_parser.add_argument('--steam-data', type=Path, required=True)
    restore_parser.add_argument('--template', type=Path, required=True)
    restore_parser.add_argument('--plan', type=Path, required=True)
    restore_parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__':
    main()
