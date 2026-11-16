#!/usr/bin/env python3
"""Classify Addressables bundles in a private launcher template.

Object and script types come from the local reconstructed OBB, because a
masked template bundle may not parse. This does not inspect MonoBehaviour
fields or prove asset ownership. Reports and inputs must remain local.
"""
import argparse
from collections import Counter
import io
import json
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
MAX_TEMPLATE = 1024 * 1024 * 1024
MAX_BUNDLE = 64 * 1024 * 1024


def inventory(reference, template, load=UnityPy.load):
    if len(reference.namelist()) != len(set(reference.namelist())) or len(template.namelist()) != len(set(template.namelist())):
        raise ValueError('duplicate archive entries')
    rows, problems = [], []
    names = sorted(n for n in template.namelist() if n.startswith('assets/aa/') and n.endswith('.bundle'))
    if not names: raise ValueError('no Addressables bundles found')
    for name in names:
        try:
            original_info, masked_info = reference.getinfo(name), template.getinfo(name)
            if not 0 < original_info.file_size <= MAX_BUNDLE or original_info.file_size != masked_info.file_size:
                raise ValueError('invalid bundle size')
            original, masked = reference.read(original_info), template.read(masked_info)
            if any(value and value != old for value, old in zip(masked, original)):
                raise ValueError('template bundle differs from reference')
            env = load(original)
            counts = Counter(obj.type.name for obj in env.objects)
            if not counts: raise ValueError('empty bundle')
            scripts = []
            for obj in env.objects:
                if obj.type.name != 'MonoScript': continue
                script = obj.read()
                scripts.append([script.m_AssemblyName, script.m_Namespace, script.m_ClassName])
            rows.append({'entry': name, 'bytes': len(masked), 'retained_nonzero_bytes': len(masked)-masked.count(0),
                         'object_types': dict(sorted(counts.items())), 'scripts': sorted(scripts)})
        except Exception as error:
            problems.append({'entry': name, 'reason': type(error).__name__ + ': ' + str(error)})
    return {'bundle_count': len(rows), 'retained_nonzero_bytes': sum(row['retained_nonzero_bytes'] for row in rows),
            'bundles': rows, 'problems': problems,
            'limitations': 'Classifies referenced OBB bundle object and script types, not MonoBehaviour fields, other APK assets or ownership.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apk', type=Path, required=True); parser.add_argument('--obb', type=Path, required=True)
    parser.add_argument('--platform', choices=('linux', 'windows'), default='linux'); parser.add_argument('--output', type=Path, required=True)
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
        with zipfile.ZipFile(io.BytesIO(apk.read(info))) as template: result = inventory(reference, template)
    with output.open('x', encoding='utf-8') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    print(f"Bundles: {result['bundle_count']}; retained nonzero bytes: {result['retained_nonzero_bytes']}; problems: {len(result['problems'])}")


if __name__ == '__main__': main()
