#!/usr/bin/env python3
"""Audit retained inline texture bytes in one private launcher template.

This identifies byte ranges, not ownership or visual recoverability. It does
not inspect streamed textures, audio, meshes, animations or non-Unity files.
Keep the APK, reference OBB and resulting report local.
"""
import argparse
import importlib.util
import io
import json
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('objects', ROOT / 'tools/experimental-exact-object-plan.py')
objects = importlib.util.module_from_spec(spec)
spec.loader.exec_module(objects)
MAX_TEMPLATE = 512 * 1024 * 1024


def retained_payload(target, masked, start, body, payload):
    """Return nonzero texture payload bytes left in a same-layout template."""
    if len(target) != len(masked) or start < 0 or start + len(body) > len(target):
        raise ValueError('invalid serialized object range')
    if target[start:start + len(body)] != body:
        raise ValueError('serialized object differs from reference')
    if not payload or payload not in body:
        raise ValueError('inline payload missing or ambiguous')
    if body.count(payload) != 1:
        # An all-zero payload cannot contribute retained nonzero bytes if the
        # entire object is demonstrably only masked, even when its offset is
        # ambiguous. Reject any other change before making that inference.
        if any(payload):
            raise ValueError('inline payload missing or ambiguous')
        remaining_body = masked[start:start + len(body)]
        if any(value and value != original for value, original in zip(remaining_body, body)):
            raise ValueError('template object differs beyond masking')
        return 0
    offset = start + body.index(payload)
    remaining = masked[offset:offset + len(payload)]
    if any(value and value != original for value, original in zip(remaining, payload)):
        raise ValueError('template payload differs from reference')
    return len(remaining) - remaining.count(0)


def audit(reference, template):
    reference_groups, template_groups = objects.asset_groups(reference), objects.asset_groups(template)
    rows, skipped, empty = [], [], []
    for name, parts in reference_groups.items():
        other = template_groups.get(name)
        if not other or [(p.filename, p.file_size) for p in parts] != [(p.filename, p.file_size) for p in other]:
            raise ValueError('asset group layout differs: ' + name)
        data = b''.join(reference.read(part) for part in parts)
        masked = b''.join(template.read(part) for part in other)
        try:
            asset_objects = UnityPy.load(data).objects
        except (ValueError, TypeError, EOFError, OSError) as e:
            skipped.append({'group': name, 'reason': type(e).__name__})
            continue
        for obj in asset_objects:
            if obj.type.name != 'Texture2D':
                continue
            try:
                texture = obj.read()
                payload = bytes(texture.image_data)
                body = obj.get_raw_data()
                if not payload:
                    stream = getattr(texture, 'm_StreamData', None)
                    if (getattr(texture, 'm_Width', None) == 0 and getattr(texture, 'm_Height', None) == 0 and
                            (stream is None or (not stream.path and not stream.size))):
                        empty.append({'group': name, 'path_id': obj.path_id, 'name': texture.m_Name})
                        continue
                    skipped.append({'group': name, 'path_id': obj.path_id, 'reason': 'no inline payload'})
                    continue
                count = retained_payload(data, masked, obj.byte_start, body, payload)
                rows.append({'group': name, 'path_id': obj.path_id, 'name': texture.m_Name,
                             'payload_bytes': len(payload), 'retained_nonzero_bytes': count})
            except (ValueError, TypeError, EOFError, OSError, AttributeError) as e:
                skipped.append({'group': name, 'path_id': obj.path_id, 'reason': str(e)})
    return {'inline_texture_count': len(rows), 'retained_nonzero_bytes': sum(r['retained_nonzero_bytes'] for r in rows),
            'textures': rows, 'empty_textures': empty, 'skipped': skipped,
            'limitations': 'Inline Texture2D payloads only; zero bytes do not prove removal or establish ownership.'}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--apk', type=Path, required=True)
    p.add_argument('--obb', type=Path, required=True)
    p.add_argument('--platform', choices=('linux', 'windows'), default='linux')
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    for path in (a.apk, a.obb):
        if path.is_symlink() or not path.is_file(): p.error('inputs must be regular files')
    output = a.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        p.error('output must be new, outside the checkout, in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in (a.apk.resolve(), a.obb.resolve()):
        p.error('output must be new, outside the checkout, in an existing directory')
    entry = 'assets/openbonk-diagnostic/' + ('windows/' if a.platform == 'windows' else '') + 'generated-audio-template.openbonk'
    with zipfile.ZipFile(a.apk) as apk, zipfile.ZipFile(a.obb) as reference:
        info = apk.getinfo(entry)
        if not 0 < info.file_size <= MAX_TEMPLATE: raise ValueError('invalid template size')
        with zipfile.ZipFile(io.BytesIO(apk.read(info))) as template:
            result = audit(reference, template)
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, indent=2)
        stream.write('\n')
    print(f"Inline textures: {result['inline_texture_count']}; retained nonzero payload bytes: {result['retained_nonzero_bytes']}; empty: {len(result['empty_textures'])}; skipped: {len(result['skipped'])}")


if __name__ == '__main__':
    main()
