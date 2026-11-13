#!/usr/bin/env python3
"""Generate and restore an experimental packed-audio offset plan.

Generated templates retain other game data and must stay local. The plan is
specific to the exact OBB and Steam inputs and is not integrated with Android.
Generation requires UnityPy; restoration uses the Python standard library.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('audio_inspect', ROOT / 'tools/inspect-packed-steam-audio.py')
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def file_digest(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def sources(folder):
    files = {group + ext: folder / (group + ext) for group in audio.GROUPS for ext in ('.assets', '.resource')}
    if any(not path.is_file() or path.is_symlink() for path in files.values()):
        raise ValueError('Steam inputs must be regular files')
    return files


def output_path(path, inputs):
    path = path.expanduser().absolute()
    if path.exists() or path.is_symlink() or not path.parent.is_dir():
        raise ValueError('output must be new in an existing directory')
    path = path.parent.resolve() / path.name
    if any(path.is_relative_to(p) or p.is_relative_to(path) for p in (*inputs, ROOT)):
        raise ValueError('output must be separate from inputs and source checkout')
    return path


def apply_ranges(raw, rows, streams=None):
    """Mask or restore sorted, nonoverlapping payloads in one resource."""
    result = bytearray(raw)
    end = 0
    for row in sorted(rows, key=lambda r: r['target_offset']):
        start, size = row['target_offset'], row['size']
        if type(start) is not int or type(size) is not int or start < end or size <= 0 or start + size > len(result):
            raise ValueError('invalid or overlapping target ranges')
        offset = row['source_offset']
        if type(offset) is not int or offset < 0 or row['source'] not in {g + '.resource' for g in audio.GROUPS}:
            raise ValueError('invalid source range')
        if streams is None:
            if digest(result[start:start + size]) != row['sha256']:
                raise ValueError('payload differs from plan')
            result[start:start + size] = bytes(size)
        else:
            if any(result[start:start + size]):
                raise ValueError('masked bytes are not zero')
            stream = streams[row['source']]
            stream.seek(offset)
            payload = stream.read(size)
            if len(payload) != size or digest(payload) != row['sha256']:
                raise ValueError('source payload differs from plan')
            result[start:start + size] = payload
        end = start + size
    return bytes(result)


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True)
    obb = args.obb.resolve(strict=True)
    output = output_path(args.output, (source, obb))
    files = sources(source)
    index = defaultdict(list)
    for group in audio.GROUPS:
        for obj in UnityPy.load(str(files[group + '.assets'])).objects:
            if obj.type.name == 'AudioClip':
                clip = obj.read()
                index[clip.m_Name].append(clip)
    if not index:
        raise ValueError('no Steam clips found')
    rows = []
    output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}):
                raise ValueError('duplicate archive entries')
            for group in audio.GROUPS:
                data = audio.split_data(zin, group)
                entry = audio.PREFIX + group + '.resource'
                resource = zin.read(entry)
                for obj in UnityPy.load(data).objects:
                    if obj.type.name != 'AudioClip':
                        continue
                    clip = obj.read()
                    dst = clip.m_Resource
                    if not dst.m_Source or not dst.m_Size:
                        continue
                    matches = [c for c in index[clip.m_Name] if audio.compatible(clip, c)]
                    if len(matches) > 1:
                        raise ValueError('ambiguous Steam clip')
                    if not matches:
                        continue
                    src = matches[0].m_Resource
                    if (src.m_Source not in {g + '.resource' for g in audio.GROUPS} or
                            dst.m_Source.rsplit('/', 1)[-1] != group + '.resource' or
                            src.m_Offset < 0 or dst.m_Offset < 0 or src.m_Size <= 0 or src.m_Size != dst.m_Size or
                            dst.m_Offset + dst.m_Size > len(resource)):
                        raise ValueError('unexpected audio resource reference')
                    with files[src.m_Source].open('rb') as stream:
                        stream.seek(src.m_Offset)
                        payload = stream.read(src.m_Size)
                    if len(payload) != src.m_Size or resource[dst.m_Offset:dst.m_Offset + dst.m_Size] != payload:
                        raise ValueError('target audio does not match Steam')
                    rows.append({'entry': entry, 'target_offset': dst.m_Offset, 'source': src.m_Source,
                                 'source_offset': src.m_Offset, 'size': src.m_Size, 'sha256': digest(payload)})
            if not rows:
                raise ValueError('no matching packed clips')
            with zipfile.ZipFile(output / 'template.obb', 'x') as zout:
                for info in infos:
                    raw = zin.read(info)
                    selected = [r for r in rows if r['entry'] == info.filename]
                    zout.writestr(info, apply_ranges(raw, selected) if selected else raw)
        plan = {'schema': 1, 'kind': 'experimental-packed-steam-audio-offsets',
                'sources': {name: file_digest(path) for name, path in files.items()},
                'template_sha256': file_digest(output / 'template.obb'), 'records': rows}
        (output / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Masked {sum(r["size"] for r in rows)} bytes from {len(rows)} clips; template still contains game data.')


def restore(args):
    source = args.steam_data.resolve(strict=True)
    template = args.template.resolve(strict=True)
    plan_path = args.plan.resolve(strict=True)
    output = output_path(args.output, (source, template, plan_path))
    files = sources(source)
    plan = json.loads(plan_path.read_text())
    if (plan.get('schema') != 1 or plan.get('kind') != 'experimental-packed-steam-audio-offsets' or
            plan.get('sources') != {name: file_digest(path) for name, path in files.items()} or
            plan.get('template_sha256') != file_digest(template)):
        raise ValueError('plan or inputs differ')
    rows = plan.get('records')
    allowed = {audio.PREFIX + group + '.resource' for group in audio.GROUPS}
    if not isinstance(rows, list) or not rows or any(r.get('entry') not in allowed for r in rows):
        raise ValueError('invalid plan records')
    seen = set()
    created = False
    try:
        with zipfile.ZipFile(template) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}):
                raise ValueError('duplicate archive entries')
            with files['sharedassets1.resource'].open('rb') as one, files['sharedassets2.resource'].open('rb') as two:
                streams = {'sharedassets1.resource': one, 'sharedassets2.resource': two}
                with output.open('xb') as target:
                    created = True
                    with zipfile.ZipFile(target, 'w') as zout:
                        for info in infos:
                            raw = zin.read(info)
                            selected = [r for r in rows if r['entry'] == info.filename]
                            if selected:
                                raw = apply_ranges(raw, selected, streams)
                                seen.add(info.filename)
                            zout.writestr(info, raw)
        if seen != {r['entry'] for r in rows}:
            raise ValueError('missing planned resource')
    except BaseException:
        if created:
            output.unlink(missing_ok=True)
        raise
    print(f'Restored {len(rows)} packed audio clips; output still contains game data.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    for name in ('generate', 'restore'):
        p = sub.add_parser(name)
        p.add_argument('--steam-data', type=Path, required=True)
        p.add_argument('--output', type=Path, required=True)
        if name == 'generate':
            p.add_argument('--obb', type=Path, required=True)
        else:
            p.add_argument('--template', type=Path, required=True)
            p.add_argument('--plan', type=Path, required=True)
    args = parser.parse_args()
    (generate if args.command == 'generate' else restore)(args)


if __name__ == '__main__':
    main()
