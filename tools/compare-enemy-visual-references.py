#!/usr/bin/env python3
"""Compare enemy material and animation target identities in local APK/OBB pairs.

The inventories must come from extract-unity-weapon-data.py --kind enemy.
This compares target identities and, with --assemblies, animation FPS and
ordered mesh names. --mesh-buffers additionally compares inline uncompressed
vertex and index buffers and their layouts. It does not compare all mesh
fields, behavior or rendering.
Inputs and the output can contain game-derived data and stay local. Requires
UnityPy; --assemblies also requires TypeTreeGeneratorAPI.
"""
import argparse
import hashlib
import importlib.metadata
import importlib.util
import json
import os
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


inventory = module('openbonk_build_inventory', 'tools/compare-unity-build-content.py')
item = module('openbonk_item_references', 'tools/compare-item-references.py')
FIELDS = {'material': 'Material', 'animation': 'MonoBehaviour'}


def targets(document):
    if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
        raise ValueError('unsupported inventory')
    rows = document.get('enemies')
    if not isinstance(rows, list) or len(rows) != 53:
        raise ValueError('unexpected enemy count')
    result = {}
    for row in rows:
        data = row['data']
        name = data['m_Name']
        if not isinstance(name, str) or not name or name in result:
            raise ValueError('invalid or duplicate enemy name')
        result[name] = {field: item.pointer_key(row, data[field]) for field in FIELDS}
    return result


def mesh_signature(mesh):
    """Describe supported inline mesh buffers without silently accepting missing data."""
    stream = mesh.m_StreamData
    vertices = bytes(mesh.m_VertexData.m_DataSize or b'')
    indices = bytes(mesh.m_IndexBuffer or [])
    if mesh.m_MeshCompression != 0 or (stream and stream.path) or not vertices or not indices or mesh.m_VertexData.m_VertexCount <= 0:
        raise ValueError('unsupported or empty animation mesh buffers')
    return {'name': mesh.m_Name, 'vertex_count': mesh.m_VertexData.m_VertexCount,
            'channels': [[c.stream, c.offset, c.format, c.dimension] for c in mesh.m_VertexData.m_Channels],
            'index_format': mesh.m_IndexFormat,
            'vertex_bytes': len(vertices), 'vertex_sha256': hashlib.sha256(vertices).hexdigest(),
            'index_bytes': len(indices), 'index_sha256': hashlib.sha256(indices).hexdigest()}


def read_build(apk, obb, document, animation_node=None, mesh_buffers=False):
    hashes = []
    for path in (apk, obb):
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    if document.get('input_sha256') != hashes:
        raise ValueError('inventory does not match APK/OBB inputs')
    pointers = targets(document)
    selected = {target[0] for fields in pointers.values() for target in fields.values() if target is not None}
    selected.add('globalgamemanagers.assets')
    objects, found = {}, set()
    def load_groups(selected_groups):
        needed = selected_groups - found
        seen = set()
        for path in (apk, obb):
            with zipfile.ZipFile(path) as archive:
                for group, parts in inventory.asset_groups(archive).items():
                    if group not in needed:
                        continue
                    if group in seen:
                        raise ValueError('duplicate serialized group')
                    seen.add(group)
                    raw = b''.join(archive.read(part) for part in parts)
                    for obj in UnityPy.load(raw).objects:
                        key = (group, obj.path_id)
                        if key in objects:
                            raise ValueError('duplicate serialized object')
                        objects[key] = obj
        if seen != needed:
            raise ValueError('referenced serialized group missing')
        found.update(seen)
    load_groups(selected)
    result, animation_meshes = {}, {}
    for enemy, fields in pointers.items():
        result[enemy] = {}
        for field, key in fields.items():
            if key is None:
                result[enemy][field] = None
                continue
            obj = objects.get(key)
            expected = FIELDS[field]
            if obj is None or obj.type.name != expected or obj.assets_file.unity_version != '2023.2.22f1':
                raise ValueError('unresolved or unexpected enemy target')
            head = obj.parse_monobehaviour_head() if field == 'animation' else None
            name = head.m_Name if head is not None else obj.read().m_Name
            if not isinstance(name, str) or not name:
                raise ValueError('unnamed enemy target')
            value = {'type': expected, 'name': name}
            if head is not None:
                pointer = head.m_Script
                external = [str(ref.path).replace('\\', '/').rsplit('/', 1)[-1] for ref in obj.assets_file.externals]
                script_key = item.pointer_key({'source_group': key[0], 'external_files': external},
                                              {'m_FileID': pointer.file_id, 'm_PathID': pointer.path_id})
                script_obj = objects.get(script_key)
                if script_obj is None or script_obj.type.name != 'MonoScript':
                    raise ValueError('unresolved animation script')
                script = script_obj.read()
                value['script'] = [script.m_AssemblyName, script.m_Namespace, script.m_ClassName]
                if animation_node is not None:
                    if value['script'] != ['Assembly-CSharp', '', 'AnimatedMeshScriptableObject']:
                        raise ValueError('unexpected animation script')
                    fields_data = obj.read_typetree(nodes=animation_node)
                    if fields_data.get('m_Name') != name or type(fields_data.get('AnimationFPS')) is not int:
                        raise ValueError('animation data failed structural validation')
                    animations = fields_data.get('Animations')
                    if not isinstance(animations, list):
                        raise ValueError('invalid animation list')
                    row = {'source_group': key[0], 'external_files': external}
                    frames = []
                    for animation in animations:
                        if not isinstance(animation.get('Name'), str) or not isinstance(animation.get('Meshes'), list):
                            raise ValueError('invalid animation')
                        frames.append({'name': animation['Name'], 'meshes': [item.pointer_key(row, p) for p in animation['Meshes']]})
                    value['fps'] = fields_data['AnimationFPS']
                    animation_meshes[enemy] = frames
            result[enemy][field] = value
    if animation_node is not None:
        load_groups({key[0] for frames in animation_meshes.values() for animation in frames
                     for key in animation['meshes'] if key is not None})
        for enemy, frames in animation_meshes.items():
            for animation in frames:
                names = []
                for key in animation['meshes']:
                    if key is None:
                        names.append(None)
                        continue
                    obj = objects.get(key)
                    if obj is None or obj.type.name != 'Mesh' or obj.assets_file.unity_version != '2023.2.22f1':
                        raise ValueError('unresolved animation mesh')
                    mesh = obj.read()
                    name = mesh.m_Name
                    if not isinstance(name, str):
                        raise ValueError('invalid animation mesh name')
                    names.append(mesh_signature(mesh) if mesh_buffers else name)
                animation['meshes'] = names
            result[enemy]['animation']['animations'] = frames
    return result


def compare(reference, candidate):
    if set(reference) != set(candidate):
        raise ValueError('enemy name sets differ')
    return [{'enemy': enemy, 'field': field, 'reference': reference[enemy][field],
             'candidate': candidate[enemy][field]}
            for enemy in sorted(reference) for field in FIELDS
            if reference[enemy][field] != candidate[enemy][field]]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'reference-enemies', 'candidate-apk', 'candidate-obb', 'candidate-enemies', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    parser.add_argument('--assemblies', type=Path, help='local Steam GameAssemblies for animation fields')
    parser.add_argument('--mesh-buffers', action='store_true', help='compare supported animation mesh buffers; requires --assemblies')
    args = parser.parse_args()
    if args.mesh_buffers and args.assemblies is None:
        parser.error('--mesh-buffers requires --assemblies')
    paths = [getattr(args, name).expanduser() for name in ('reference_apk', 'reference_obb', 'reference_enemies', 'candidate_apk', 'candidate_obb', 'candidate_enemies')]
    if any(path.is_symlink() or not path.is_file() for path in paths):
        parser.error('inputs must be regular files')
    paths = [path.resolve() for path in paths]
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in paths:
        parser.error('output must be separate from checkout and inputs')
    node, assembly_hashes = None, {}
    if args.assemblies is not None:
        assemblies = args.assemblies.expanduser()
        if assemblies.is_symlink() or not assemblies.is_dir() or not (assemblies / 'Assembly-CSharp.dll').is_file():
            parser.error('assemblies must be a local exported GameAssemblies directory')
        assemblies = assemblies.resolve()
        if output.is_relative_to(assemblies):
            parser.error('output must be separate from assemblies')
        for package, version in (('UnityPy', '1.25.4'), ('TypeTreeGeneratorAPI', '0.0.10')):
            try:
                if importlib.metadata.version(package) != version:
                    parser.error(package + ' ' + version + ' is required')
            except importlib.metadata.PackageNotFoundError:
                parser.error(package + ' ' + version + ' is required')
        from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
        generator = TypeTreeGenerator('2023.2.22f1')
        for path in sorted(assemblies.glob('*.dll')):
            if path.is_symlink() or not path.is_file():
                raise ValueError('assembly inputs must be regular files')
            data = path.read_bytes()
            generator.load_dll(data)
            assembly_hashes[path.name] = hashlib.sha256(data).hexdigest()
        node = generator.get_nodes_up('Assembly-CSharp.dll', 'AnimatedMeshScriptableObject')
    reference = read_build(paths[0], paths[1], json.loads(paths[2].read_text(encoding='utf-8')), node, args.mesh_buffers)
    candidate = read_build(paths[3], paths[4], json.loads(paths[5].read_text(encoding='utf-8')), node, args.mesh_buffers)
    differences = compare(reference, candidate)
    input_hashes = []
    for path in paths:
        with path.open('rb') as stream:
            input_hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result = {'schema': 1, 'enemy_count': len(reference), 'reference': reference, 'candidate': candidate,
              'differences': differences, 'input_sha256': input_hashes,
              'animation_fields_checked': node is not None, 'mesh_buffers_checked': args.mesh_buffers,
              'assembly_sha256': assembly_hashes,
              'limitations': 'Target identities and optional animation fields and mesh buffers only; other mesh fields, materials, code and rendering are not compared.'}
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', len(reference), 'enemies; differences:', len(differences), '. Report remains private.')


if __name__ == '__main__':
    main()
