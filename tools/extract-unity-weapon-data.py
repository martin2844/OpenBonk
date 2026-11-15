#!/usr/bin/env python3
"""Decode selected gameplay data records from a Unity APK/OBB pair.

Uses locally exported Steam GameAssemblies and an isolated temporary workspace.
The output contains game-derived data and must remain private. Unity object
pointers have not been mapped or validated; this does not build an APK.
Requires UnityPy 1.25.4 and TypeTreeGeneratorAPI 0.0.10.
"""
import argparse
import importlib.metadata
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('build_inventory', ROOT / 'tools/compare-unity-build-content.py')
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('apk', 'obb', 'assemblies', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--kind', choices=('weapon', 'upgrade', 'map', 'stage', 'enemy', 'noise', 'map-parameters'), default='weapon')
    args = parser.parse_args()
    class_name, full_name, expected, id_field, registry_count, plural = {
        'weapon': ('WeaponData', 'WeaponData', 30, 'eWeapon', 2, 'weapons'),
        'upgrade': ('UpgradeData', 'Assets.Scripts.Inventory__Items__Pickups.Upgrades.UpgradeData', 29, 'm_Name', 0, 'upgrades'),
        'map': ('MapData', 'MapData', 5, 'eMap', 2, 'maps'),
        'stage': ('StageData', 'StageData', 9, 'm_Name', 1, 'stages'),
        'enemy': ('EnemyData', 'EnemyData', 53, 'm_Name', 1, 'enemies'),
        'noise': ('NoiseData', 'NoiseData', 3, 'm_Name', 0, 'noise'),
        'map-parameters': ('MapParameters', 'Assets.Scripts.MapGeneration.ProceduralTiles.MapParameters', 4, 'm_Name', 0, 'map_parameters'),
    }[args.kind]
    inputs = []
    for path in (args.apk, args.obb):
        path = path.expanduser()
        if path.is_symlink() or not path.is_file():
            parser.error('APK and OBB must be regular files')
        inputs.append(path.resolve())
    assemblies = args.assemblies.expanduser()
    if assemblies.is_symlink() or not assemblies.is_dir():
        parser.error('assemblies must be a local directory')
    assemblies = assemblies.resolve()
    if not (assemblies / 'Assembly-CSharp.dll').is_file():
        parser.error('exported Assembly-CSharp.dll is missing')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new file')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output.is_relative_to(assemblies) or output in inputs:
        parser.error('output must be separate from checkout and inputs')
    for package, expected_version in (('UnityPy', '1.25.4'), ('TypeTreeGeneratorAPI', '0.0.10')):
        try:
            version = importlib.metadata.version(package)
        except importlib.metadata.PackageNotFoundError:
            parser.error(package + ' ' + expected_version + ' is required')
        if version != expected_version:
            parser.error(package + ' ' + expected_version + ' is required')
    import UnityPy
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator

    groups = {}
    for path in inputs:
        with zipfile.ZipFile(path) as archive:
            for name, parts in inventory.asset_groups(archive).items():
                if name in groups:
                    raise ValueError('duplicate serialized group')
                groups[name] = b''.join(archive.read(part) for part in parts)
    if 'globalgamemanagers.assets' not in groups:
        raise ValueError('missing global game managers')
    scripts = {}
    for obj in UnityPy.load(groups['globalgamemanagers.assets']).objects:
        if obj.type.name == 'MonoScript':
            script = obj.read()
            scripts[obj.path_id] = (script.m_AssemblyName, script.m_ClassName)
    selected = {'globalgamemanagers.assets'}
    for name, raw in groups.items():
        env = UnityPy.load(raw)
        if not env.objects:
            raise ValueError('empty serialized group: ' + name)
        if any(obj.assets_file.unity_version != '2023.2.22f1' for obj in env.objects):
            raise ValueError('unsupported Unity version')
        externals = env.objects[0].assets_file.externals
        local = {}
        for obj in env.objects:
            if obj.type.name == 'MonoScript':
                script = obj.read()
                local[obj.path_id] = (script.m_AssemblyName, script.m_ClassName)
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            pointer = obj.read_typetree(check_read=False)['m_Script']
            file_id, path_id = pointer['m_FileID'], pointer['m_PathID']
            if file_id == 0:
                identity = local.get(path_id)
            elif 0 < file_id <= len(externals) and str(externals[file_id - 1].path).replace('\\', '/').rsplit('/', 1)[-1] == 'globalgamemanagers.assets':
                identity = scripts.get(path_id)
            else:
                identity = None
            if identity is None:
                raise ValueError('unresolved script in ' + name)
            if identity == ('Assembly-CSharp', class_name):
                selected.add(name)
    generator = TypeTreeGenerator('2023.2.22f1')
    for path in sorted(assemblies.glob('*.dll')):
        if path.is_symlink() or not path.is_file():
            raise ValueError('assembly inputs must be regular files')
        generator.load_dll(path.read_bytes())
    node = generator.get_nodes_up('Assembly-CSharp.dll', full_name)
    registries = [child for child in node.m_Children if child.m_Name == 'references']
    if len(registries) != registry_count or (registries and node.m_Children[-1].m_Name != 'references'):
        raise ValueError('unexpected data type tree')
    if registries:
        node.m_Children[:] = [child for child in node.m_Children if child.m_Name != 'references'] + registries[-1:]
    with tempfile.TemporaryDirectory(prefix='openbonk-weapons-', dir=output.parent) as temp:
        paths = []
        for name in sorted(selected):
            path = Path(temp) / name
            path.write_bytes(groups[name])
            paths.append(str(path))
        env = UnityPy.load(*paths)
        env.typetree_generator = generator
        rows = []
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            head = obj.parse_monobehaviour_head()
            script = head.m_Script.deref_parse_as_object()
            if (script.m_AssemblyName, script.m_ClassName) != ('Assembly-CSharp', class_name):
                continue
            data = obj.read_typetree(nodes=node)
            if (data.get('m_Name') != head.m_Name or not data.get('m_Name') or
                    (args.kind in ('weapon', 'map') and type(data.get(id_field)) is not int) or
                    (args.kind == 'upgrade' and not isinstance(data.get('upgradeModifiers'), list)) or
                    (args.kind == 'map' and not isinstance(data.get('stages'), list)) or
                    (args.kind == 'stage' and not isinstance(data.get('farClipPlane'), (int, float))) or
                    (args.kind == 'enemy' and type(data.get('enemyName')) is not int) or
                    (args.kind == 'noise' and not isinstance(data.get('noiseScale'), (int, float))) or
                    (args.kind == 'map-parameters' and type(data.get('size')) is not int)):
                raise ValueError('data record failed structural validation')
            rows.append({'source_group': obj.assets_file.name, 'path_id': obj.path_id,
                         'external_files': [str(ref.path).replace('\\', '/').rsplit('/', 1)[-1]
                                            for ref in obj.assets_file.externals], 'data': data})
    if len(rows) != expected or len({row['data'][id_field] for row in rows}) != expected:
        raise ValueError(f'unexpected data count or duplicate ID: {len(rows)} records, '
                         f'{len({row["data"][id_field] for row in rows})} unique')
    payload = {'schema': 1, 'unity_version': '2023.2.22f1', plural: rows,
               'notice': 'Game-derived data; Unity object pointers require additional mapping.'}
    encoded = (json.dumps(payload, indent=2, ensure_ascii=False, allow_nan=False) + '\n').encode('utf-8')
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'wb') as stream:
        stream.write(encoded)
    print('Decoded', expected, args.kind, 'records locally. Asset pointer mapping is still required.')


if __name__ == '__main__':
    main()
