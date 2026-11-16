#!/usr/bin/env python3
"""Audit inline mesh buffer ranges in a private launcher template.

The report measures retained nonzero payload bytes, not ownership. It does not
cover streamed meshes, other asset types or skipped records.
Keep all inputs and the output report local.
"""
import argparse
import importlib.util
import io
import json
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_audit', ROOT / 'tools/audit-inline-texture-payloads.py')
texture_audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(texture_audit)


def audit(reference, template):
    reference_groups = texture_audit.objects.asset_groups(reference)
    template_groups = texture_audit.objects.asset_groups(template)
    rows, skipped = [], []
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
            if obj.type.name != 'Mesh':
                continue
            try:
                mesh = obj.read()
                body = obj.get_raw_data()
                vertices = bytes(mesh.m_VertexData.m_DataSize or b'')
                indices = bytes(mesh.m_IndexBuffer or [])
                stream = mesh.m_StreamData
                if not vertices and stream and stream.path and stream.size:
                    skipped.append({'group': name, 'path_id': obj.path_id, 'reason': 'streamed vertex data'})
                payloads = [('vertices', vertices), ('indices', indices)]
                compressed = getattr(mesh, 'm_CompressedMesh', None)
                if compressed:
                    for field in ('m_Vertices', 'm_UV', 'm_Normals', 'm_Tangents', 'm_Weights',
                                  'm_NormalSigns', 'm_TangentSigns', 'm_FloatColors', 'm_BoneIndices',
                                  'm_Triangles', 'm_BindPoses', 'm_Colors'):
                        packed = getattr(compressed, field, None)
                        if packed is not None:
                            payloads.append(('compressed/' + field, bytes(getattr(packed, 'm_Data', None) or b'')))
                for kind, payload in payloads:
                    if not payload:
                        continue
                    try:
                        count = texture_audit.retained_payload(data, masked, obj.byte_start, body, payload)
                        rows.append({'group': name, 'path_id': obj.path_id, 'name': mesh.m_Name,
                                     'kind': kind, 'payload_bytes': len(payload), 'retained_nonzero_bytes': count})
                    except ValueError as e:
                        skipped.append({'group': name, 'path_id': obj.path_id, 'kind': kind, 'reason': str(e)})
            except (ValueError, TypeError, EOFError, OSError, AttributeError) as e:
                skipped.append({'group': name, 'path_id': obj.path_id, 'reason': str(e)})
    return {'inline_buffer_count': len(rows), 'retained_nonzero_bytes': sum(r['retained_nonzero_bytes'] for r in rows),
            'buffers': rows, 'skipped': skipped,
            'limitations': 'Inline vertex, index and selected compressed buffers only; zero bytes do not prove removal or establish ownership.'}


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
    print(f"Inline buffers: {result['inline_buffer_count']}; retained nonzero payload bytes: {result['retained_nonzero_bytes']}; skipped: {len(result['skipped'])}")


if __name__ == '__main__':
    main()
