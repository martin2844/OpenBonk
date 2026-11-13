#!/usr/bin/env python3
"""Experimentally decode local Steam CharacterData; not an APK build step.

Outputs game-derived data locally. The raw Unity pointers still need mapping
into an assembled project, so the output cannot yet restore playable assets.
Requires UnityPy 1.25.4 and TypeTreeGeneratorAPI 0.0.10 in a local environment.
"""
import argparse
import importlib.metadata
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
VERSION = '2023.2.22f1'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-data', required=True, type=Path, help='local Megabonk_Data directory')
    parser.add_argument('--assemblies', required=True, type=Path, help='local exported GameAssemblies directory')
    parser.add_argument('--output', required=True, type=Path, help='new local JSON file outside the checkout and inputs')
    args = parser.parse_args()
    data = args.game_data.expanduser().resolve(strict=True)
    assemblies = args.assemblies.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) for path in (ROOT, data, assemblies)):
        parser.error('output must be outside the checkout and input directories')
    inputs = [data / 'globalgamemanagers.assets', data / 'sharedassets1.assets']
    if any(not f.is_file() or f.is_symlink() for f in inputs):
        parser.error('required original game asset files are missing')
    if not (assemblies / 'Assembly-CSharp.dll').is_file():
        parser.error('exported Assembly-CSharp.dll is missing')
    for name, expected in (('UnityPy', '1.25.4'), ('TypeTreeGeneratorAPI', '0.0.10')):
        try:
            version = importlib.metadata.version(name)
        except importlib.metadata.PackageNotFoundError:
            parser.error(name + ' ' + expected + ' is required')
        if version != expected:
            parser.error(name + ' ' + expected + ' is required; found ' + version)
    import UnityPy
    from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
    generator = TypeTreeGenerator(VERSION)
    loaded = 0
    for f in sorted(assemblies.glob('*.dll')):
        if f.is_symlink() or not f.is_file():
            parser.error('assembly inputs must be regular local files')
        try:
            generator.load_dll(f.read_bytes())
            loaded += 1
        except Exception as error:
            raise SystemExit('Unable to load assembly ' + f.name + ': ' + type(error).__name__) from error
    node = generator.get_nodes_up('Assembly-CSharp.dll', 'CharacterData')
    registries = [child for child in node.m_Children if child.m_Name == 'references']
    # The generator inserts a registry for both the base and derived class;
    # Unity serializes the final registry once, after the derived fields.
    if len(registries) != 2 or node.m_Children[-1].m_Name != 'references':
        raise SystemExit('Unexpected CharacterData type tree; refusing to decode.')
    node.m_Children[:] = [child for child in node.m_Children if child.m_Name != 'references'] + registries[-1:]
    env = UnityPy.load(*map(str, inputs))
    env.typetree_generator = generator
    if any(obj.assets_file.unity_version != VERSION for obj in env.objects):
        raise SystemExit('Unsupported Unity game version.')
    rows = []
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour':
            continue
        try:
            header = obj.parse_monobehaviour_head()
            script = header.m_Script.deref_parse_as_object()
        except (ValueError, AttributeError, KeyError):
            continue
        if script.m_ClassName != 'CharacterData' or script.m_AssemblyName != 'Assembly-CSharp':
            continue
        if obj.assets_file.name != 'sharedassets1.assets':
            raise SystemExit('Character record was found in an unexpected source file.')
        row = obj.read_typetree(nodes=node)
        if row.get('m_Name') != header.m_Name or not isinstance(row.get('eCharacter'), int):
            raise SystemExit('Decoded character data did not pass structural validation.')
        rows.append({'path_id': obj.path_id, 'data': row})
    if len(rows) != 21 or len({r['data']['eCharacter'] for r in rows}) != 21:
        raise SystemExit('Unexpected character count or duplicate character ID; no output was written.')
    payload = {'schema': 1, 'unity_version': VERSION, 'source_file': 'sharedassets1.assets',
               'assemblies_loaded': loaded, 'characters': rows,
               'notice': 'Game-derived data; raw Unity pointers require additional mapping.'}
    encoded = (json.dumps(payload, indent=2, ensure_ascii=False, allow_nan=False) + '\n').encode('utf-8')
    fd = os.open(output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, 'wb') as f:
        f.write(encoded)
    print('Decoded 21 character records to a local file. Asset pointer mapping is still required.')


if __name__ == '__main__':
    main()
