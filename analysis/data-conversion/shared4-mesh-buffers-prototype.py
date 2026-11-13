#!/usr/bin/env python3
"""Locally blank and reconstruct seven final-scene meshes from pinned Steam data.

This only removes vertex/index buffers from an experimental asset. Its other
objects, metadata, dependencies and code are not cleared for distribution.
"""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data

PINS = {
    'sharedassets4.assets': 'cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3',
    'sharedassets4.assets.resS': '862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127',
    'level4': 'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600',
}
WINDOWS_PINS = {**PINS, 'sharedassets4.assets':'896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9', 'level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}
BASE = 'cd0a2e7c6f93ecc00bdde165a6654b3818c3e4afaaad2eb52068a93a62b1c29a'
ORIGINAL_BASE = 'fd49162dc6d36ec83cf52f45338a2c871c061664987ae7d207512c2ab3c57aa0'
NORMALIZED_ORIGINAL = '521dd2cbe9a32ef45227ff38e3d303c437146e6ee1d6426707210de8aed230f6'
TEXTURES_REBUILT = 'e12a3f6edd2a570bdf8caf871460a73ff6538a7fa47581d5dcc7a6f1e272a9e7'
TEXTURES_AND_MESHES_REBUILT = '153369b4099a487db5f7a294323c9b58b0a8c113064acffb897ae2ef89f5a3c7'
MAPPING = {8: ('sharedassets4.assets', 8), 9: ('sharedassets4.assets', 9),
           10: ('level4', 1), 11: ('level4', 4), 13: ('level4', 2),
           14: ('level4', 5), 15: ('level4', 3)}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    with path.open('rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()


def geometry(mesh):
    return (mesh.m_VertexData.m_VertexCount,
            tuple((c.stream, c.offset, c.format, c.dimension) for c in mesh.m_VertexData.m_Channels),
            mesh.m_IndexFormat)


def payload(mesh):
    vertices = bytes(mesh.m_VertexData.m_DataSize or b'')
    stream = mesh.m_StreamData
    if not vertices and stream and stream.path:
        if stream.path != 'sharedassets4.assets.resS':
            raise ValueError('unexpected mesh resource')
        vertices = get_resource_data(stream.path, mesh.object_reader.assets_file, stream.offset, stream.size)
        if len(vertices) != stream.size:
            raise ValueError('short mesh stream')
    return vertices, bytes(mesh.m_IndexBuffer or [])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('template', 'restore'))
    p.add_argument('pc_data', type=Path)
    p.add_argument('input_asset', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--manifest', type=Path)
    p.add_argument('--textures-rebuilt', action='store_true', help='restore meshes after pinned experimental texture conversion')
    p.add_argument('--windows-source', action='store_true')
    a = p.parse_args()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new and its parent must exist')
    out, pc, asset = a.output.resolve(), a.pc_data.resolve(), a.input_asset.resolve()
    if out.is_relative_to(pc) or pc.is_relative_to(out) or out.is_relative_to(asset) or asset.is_relative_to(out):
        p.error('output must be separate from inputs')
    for name, expected in (WINDOWS_PINS if a.windows_source else PINS).items():
        if not (pc / name).is_file() or sha(pc / name) != expected:
            raise ValueError('incompatible Steam input: ' + name)
    manifest = None
    base_sha = sha(asset)
    if a.action == 'template':
        if a.textures_rebuilt:
            p.error('--textures-rebuilt requires restore')
        if base_sha not in (BASE, ORIGINAL_BASE):
            raise ValueError('unexpected asset input')
    else:
        if not a.manifest:
            p.error('restore requires --manifest')
        manifest = json.loads(a.manifest.read_text())
        base_sha = manifest.get('base_sha256')
        expected_input = TEXTURES_REBUILT if a.textures_rebuilt and base_sha == ORIGINAL_BASE else manifest.get('template_sha256')
        if (manifest.get('version') != 1 or base_sha not in (BASE, ORIGINAL_BASE)
                or (a.textures_rebuilt and base_sha != ORIGINAL_BASE) or sha(asset) != expected_input):
            raise ValueError('unexpected template or manifest')
    sources = {name: {o.path_id: o for o in UnityPy.load(str(pc / name)).objects}
               for name in ('sharedassets4.assets', 'level4')}
    env = UnityPy.load(str(asset))
    objects = {o.path_id: o for o in env.objects}
    if len(objects) != len(env.objects):
        raise ValueError('duplicate object IDs')
    unchanged = {i: (o.type.name, digest(o.get_raw_data())) for i, o in objects.items() if i not in MAPPING}
    records = []
    if manifest is not None:
        by_id = {r['target_id']: r for r in manifest['meshes']}
        if len(manifest['meshes']) != len(MAPPING) or set(by_id) != set(MAPPING):
            raise ValueError('unexpected mesh inventory')
    for target_id, (source_file, source_id) in MAPPING.items():
        obj = objects[target_id]
        source_obj = sources[source_file][source_id]
        if obj.type.name != 'Mesh' or source_obj.type.name != 'Mesh':
            raise ValueError('unexpected object type')
        dst, src = obj.read(), source_obj.read()
        if dst.m_StreamData and dst.m_StreamData.path:
            raise ValueError('unexpected Android mesh stream')
        vertices, indices = payload(dst)
        sv, si = payload(src)
        if geometry(dst) != geometry(src) or len(vertices) != len(sv) or len(indices) != len(si):
            raise ValueError('mesh layout mismatch')
        rec = {'target_id': target_id, 'source_file': source_file, 'source_id': source_id,
               'name': dst.m_Name, 'vertices_size': len(sv), 'indices_size': len(si),
               'vertices_sha256': digest(sv), 'indices_sha256': digest(si)}
        if a.action == 'template':
            if vertices != sv or indices != si:
                raise ValueError('mesh bytes do not match Steam')
            dst.m_VertexData.m_DataSize = bytes(len(vertices))
            dst.m_IndexBuffer = list(bytes(len(indices)))
        else:
            if by_id[target_id] != rec or any(vertices) or any(indices):
                raise ValueError('invalid mesh manifest or template')
            dst.m_VertexData.m_DataSize = sv
            dst.m_IndexBuffer = list(si)
        dst.save()
        records.append(rec)
    a.output.mkdir()
    try:
        result = a.output / 'sharedassets4.assets'
        result.write_bytes(env.file.save())
        check = {o.path_id: o for o in UnityPy.load(str(result)).objects}
        if set(check) != set(objects) or any(check[i].type.name != typ or digest(check[i].get_raw_data()) != h for i, (typ, h) in unchanged.items()):
            raise ValueError('unrelated object changed')
        for rec in records:
            v, i = payload(check[rec['target_id']].read())
            if a.action == 'template':
                if any(v) or any(i):
                    raise ValueError('buffer not blanked')
            elif digest(v) != rec['vertices_sha256'] or digest(i) != rec['indices_sha256']:
                raise ValueError('restored buffer mismatch')
        result_sha = sha(result)
        expected_restored = TEXTURES_AND_MESHES_REBUILT if a.textures_rebuilt else NORMALIZED_ORIGINAL if base_sha == ORIGINAL_BASE else base_sha
        if a.action == 'restore' and result_sha != expected_restored:
            raise ValueError('restored file differs from original: ' + result_sha)
        report = {'version': 1, 'base_sha256': base_sha, 'template_sha256': result_sha if a.action == 'template' else manifest['template_sha256'],
                  'meshes': records, 'unchanged_objects': len(unchanged),
                  'blanked_bytes': sum(r['vertices_size'] + r['indices_size'] for r in records),
                  'limitations': 'Local research only; other original content and code remain.'}
        (a.output / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n')
        print(a.action, len(records), 'meshes,', report['blanked_bytes'], 'buffer bytes; SHA256', result_sha)
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__ == '__main__':
    main()
