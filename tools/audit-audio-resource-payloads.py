#!/usr/bin/env python3
"""Audit referenced AudioClip resource ranges in a private launcher template.

Only audio ranges referenced by readable serialized AudioClip records in the
provided OBB are checked. This does not inspect asset bundles, inline audio,
video, other media, or establish ownership or audible fidelity. Keep reports
and all input files local.
"""
import argparse
import importlib.util
import io
import json
from pathlib import Path
import re
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('audio_objects', ROOT / 'tools/experimental-exact-object-plan.py')
objects = importlib.util.module_from_spec(spec); spec.loader.exec_module(objects)
PREFIX = 'assets/bin/Data/'
SOURCE = re.compile(r'(?:[0-9a-f]{32}|sharedassets[0-9]+)\.resource\Z')
MAX_RESOURCE = 256 * 1024 * 1024
MAX_TEMPLATE = 1024 * 1024 * 1024


def retained_range(reference, template, source, offset, size, cache):
    if (not isinstance(source, str) or not SOURCE.fullmatch(source) or
            type(offset) is not int or type(size) is not int or offset < 0 or size <= 0):
        raise ValueError('invalid audio resource reference')
    name = PREFIX + source
    if name not in cache:
        original_info, masked_info = reference.getinfo(name), template.getinfo(name)
        if not 0 < original_info.file_size <= MAX_RESOURCE or original_info.file_size != masked_info.file_size:
            raise ValueError('invalid resource size')
        cache[name] = (reference.read(original_info), template.read(masked_info))
    original, masked = cache[name]
    if offset > len(original) - size:
        raise ValueError('audio range exceeds resource')
    before, remaining = original[offset:offset + size], masked[offset:offset + size]
    if any(value and value != old for value, old in zip(remaining, before)):
        raise ValueError('template audio differs from reference')
    return len(remaining) - remaining.count(0)


def audit(reference, template):
    if len(reference.namelist()) != len(set(reference.namelist())) or len(template.namelist()) != len(set(template.namelist())):
        raise ValueError('duplicate archive entries')
    rows, skipped, cache = [], [], {}
    for group, parts in objects.asset_groups(reference).items():
        try:
            asset_objects = UnityPy.load(b''.join(reference.read(part) for part in parts)).objects
        except Exception as error:
            skipped.append({'group': group, 'reason': type(error).__name__}); continue
        for obj in asset_objects:
            if obj.type.name != 'AudioClip': continue
            try:
                tree = obj.read_typetree()
                if tree.get('m_AudioData'):
                    raise ValueError('inline audio is not covered')
                resource = tree['m_Resource']
                source, offset, size = resource['m_Source'], resource['m_Offset'], resource['m_Size']
                count = retained_range(reference, template, source, offset, size, cache)
                rows.append({'group': group, 'path_id': obj.path_id, 'name': tree.get('m_Name', ''),
                             'source': source, 'offset': offset, 'size': size, 'retained_nonzero_bytes': count})
            except (KeyError, ValueError, TypeError, OSError, zipfile.BadZipFile) as error:
                skipped.append({'group': group, 'path_id': obj.path_id, 'reason': str(error)})
    return {'clip_count': len(rows), 'resource_count': len(cache),
            'retained_nonzero_bytes': sum(row['retained_nonzero_bytes'] for row in rows),
            'clips': rows, 'skipped': skipped,
            'limitations': 'Only referenced audio ranges in readable OBB AudioClip records; bundles, inline audio, video and ownership are not covered.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apk', type=Path, required=True); parser.add_argument('--obb', type=Path, required=True)
    parser.add_argument('--platform', choices=('linux', 'windows'), default='linux')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    for path in (args.apk, args.obb):
        if path.is_symlink() or not path.is_file(): parser.error('inputs must be regular files')
    output = args.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): parser.error('output must be new in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in (args.apk.resolve(), args.obb.resolve()): parser.error('output must be outside checkout and inputs')
    entry = 'assets/openbonk-diagnostic/' + ('windows/' if args.platform == 'windows' else '') + 'generated-audio-template.openbonk'
    with zipfile.ZipFile(args.apk) as apk, zipfile.ZipFile(args.obb) as reference:
        info = apk.getinfo(entry)
        if not 0 < info.file_size <= MAX_TEMPLATE: raise ValueError('invalid template size')
        with zipfile.ZipFile(io.BytesIO(apk.read(info))) as template: result = audit(reference, template)
    with output.open('x', encoding='utf-8') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    print(f"Audio clips: {result['clip_count']}; resources: {result['resource_count']}; retained nonzero bytes: {result['retained_nonzero_bytes']}; skipped: {len(result['skipped'])}")


if __name__ == '__main__': main()
