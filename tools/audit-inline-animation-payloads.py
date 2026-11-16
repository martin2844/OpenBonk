#!/usr/bin/env python3
"""Audit selected inline animation sample arrays in a private launcher template.

The report measures retained nonzero bytes, not ownership or visual fidelity.
It excludes other animation fields, other asset types and skipped records.
Keep all inputs and the output report local.
"""
import argparse
import importlib.util
import io
import json
from pathlib import Path
import struct
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_audit', ROOT / 'tools/audit-inline-texture-payloads.py')
texture_audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(texture_audit)


def sample_payloads(tree):
    clip = tree['m_MuscleClip']['m_Clip']['data']
    arrays = (('streamed', clip['m_StreamedClip']['data'], 'I'),
              ('dense', clip['m_DenseClip']['m_SampleArray'], 'f'),
              ('constant', clip['m_ConstantClip']['data'], 'f'))
    return [(name, struct.pack('<%d%s' % (len(values), fmt), *values))
            for name, values, fmt in arrays if values]


def audit(reference, template):
    reference_groups = texture_audit.objects.asset_groups(reference)
    template_groups = texture_audit.objects.asset_groups(template)
    rows, skipped, empty = [], [], []
    for name, parts in reference_groups.items():
        other = template_groups.get(name)
        if not other or [(p.filename, p.file_size) for p in parts] != [(p.filename, p.file_size) for p in other]:
            raise ValueError('asset group layout differs: ' + name)
        data = b''.join(reference.read(part) for part in parts)
        masked = b''.join(template.read(part) for part in other)
        try:
            asset_objects = UnityPy.load(data).objects
        except (ValueError, TypeError, EOFError, OSError) as error:
            skipped.append({'group': name, 'reason': type(error).__name__})
            continue
        for obj in asset_objects:
            if obj.type.name != 'AnimationClip':
                continue
            try:
                tree = obj.read_typetree(); body = obj.get_raw_data()
                payloads = sample_payloads(tree)
                if not payloads:
                    empty.append({'group': name, 'path_id': obj.path_id, 'name': tree['m_Name']})
                for kind, payload in payloads:
                    try:
                        count = texture_audit.retained_payload(data, masked, obj.byte_start, body, payload)
                        rows.append({'group': name, 'path_id': obj.path_id, 'name': tree['m_Name'], 'kind': kind,
                                     'payload_bytes': len(payload), 'retained_nonzero_bytes': count})
                    except ValueError as error:
                        skipped.append({'group': name, 'path_id': obj.path_id, 'kind': kind, 'reason': str(error)})
            except (ValueError, TypeError, EOFError, OSError, AttributeError, KeyError, OverflowError, struct.error) as error:
                skipped.append({'group': name, 'path_id': obj.path_id, 'reason': type(error).__name__})
    return {'array_count': len(rows), 'retained_nonzero_bytes': sum(r['retained_nonzero_bytes'] for r in rows),
            'arrays': rows, 'empty_clips': empty, 'skipped': skipped,
            'limitations': 'Selected inline sample arrays only; zero bytes do not prove removal or establish ownership.'}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--apk', type=Path, required=True)
    p.add_argument('--obb', type=Path, required=True)
    p.add_argument('--platform', choices=('linux', 'windows'), default='linux')
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    for path in (a.apk, a.obb):
        if path.is_symlink() or not path.is_file():
            p.error('inputs must be regular files')
    output = a.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        p.error('output must be new, outside the checkout, in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in (a.apk.resolve(), a.obb.resolve()):
        p.error('output must be new, outside the checkout, in an existing directory')
    entry = 'assets/openbonk-diagnostic/' + ('windows/' if a.platform == 'windows' else '') + 'generated-audio-template.openbonk'
    with zipfile.ZipFile(a.apk) as apk, zipfile.ZipFile(a.obb) as reference:
        info = apk.getinfo(entry)
        if not 0 < info.file_size <= texture_audit.MAX_TEMPLATE:
            raise ValueError('invalid template size')
        with zipfile.ZipFile(io.BytesIO(apk.read(info))) as template:
            result = audit(reference, template)
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(f"Inline arrays: {result['array_count']}; retained nonzero bytes: {result['retained_nonzero_bytes']}; empty clips: {len(result['empty_clips'])}; skipped: {len(result['skipped'])}")


if __name__ == '__main__':
    main()
