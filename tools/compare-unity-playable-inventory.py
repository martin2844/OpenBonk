#!/usr/bin/env python3
"""Compare names of selected gameplay data in two local Unity APK/OBB pairs.

The private report contains game-derived names. Matching names and counts do
not prove matching fields, referenced assets, code, or gameplay. Asset bundles
and objects whose scripts cannot be resolved are outside the comparison.
Requires UnityPy.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('build_inventory', ROOT / 'tools/compare-unity-build-content.py')
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)
CLASSES = frozenset(('CharacterData', 'WeaponData', 'ItemData', 'MapData', 'StageData'))


def script_identity(file_id, path_id, external, local_scripts, global_scripts):
    if file_id == 0:
        return local_scripts.get(path_id)
    if external == 'globalgamemanagers.assets':
        return global_scripts.get(path_id)
    return None


def read_build(paths):
    groups = {}
    for path in paths:
        with zipfile.ZipFile(path) as archive:
            for name, parts in inventory.asset_groups(archive).items():
                if name in groups:
                    raise ValueError('duplicate serialized group: ' + name)
                groups[name] = b''.join(archive.read(part) for part in parts)
    if 'globalgamemanagers.assets' not in groups:
        raise ValueError('missing global game managers')
    scripts = {}
    for obj in UnityPy.load(groups['globalgamemanagers.assets']).objects:
        if obj.type.name == 'MonoScript':
            script = obj.read()
            scripts[obj.path_id] = (script.m_AssemblyName, script.m_ClassName)
    result = defaultdict(Counter)
    problems = []
    unresolved = []
    for group, raw in groups.items():
        try:
            env = UnityPy.load(raw)
            if not env.objects:
                raise ValueError('empty serialized group')
        except Exception as error:
            problems.append({'group': group, 'error': type(error).__name__})
            continue
        externals = env.objects[0].assets_file.externals
        local_scripts = {}
        for obj in env.objects:
            if obj.type.name != 'MonoScript':
                continue
            try:
                script = obj.read()
                local_scripts[obj.path_id] = (script.m_AssemblyName, script.m_ClassName)
            except Exception as error:
                problems.append({'group': group, 'path_id': obj.path_id, 'error': type(error).__name__})
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            try:
                base = obj.read_typetree(check_read=False)
                pointer = base['m_Script']
                file_id = pointer['m_FileID']
                external = (str(externals[file_id - 1].path).replace('\\', '/').rsplit('/', 1)[-1]
                            if 0 < file_id <= len(externals) else None)
                identity = script_identity(file_id, pointer['m_PathID'], external, local_scripts, scripts)
                if identity is None:
                    unresolved.append({'group': group, 'path_id': obj.path_id,
                                       'object_name': base.get('m_Name'), 'script_file_id': file_id,
                                       'script_path_id': pointer['m_PathID'], 'script_external': external})
                elif identity[0] == 'Assembly-CSharp' and identity[1] in CLASSES:
                    name = base.get('m_Name')
                    if not isinstance(name, str) or not name:
                        raise ValueError('missing data object name')
                    result[identity[1]][name] += 1
            except Exception as error:
                problems.append({'group': group, 'path_id': obj.path_id, 'error': type(error).__name__})
    return result, problems, unresolved


def compare(reference, candidate):
    return {name: {'reference_count': sum(reference[name].values()),
                   'candidate_count': sum(candidate[name].values()),
                   'reference_only': dict(sorted((reference[name] - candidate[name]).items())),
                   'candidate_only': dict(sorted((candidate[name] - reference[name]).items()))}
            for name in sorted(CLASSES)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'candidate-apk', 'candidate-obb', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    paths = []
    for name in ('reference_apk', 'reference_obb', 'candidate_apk', 'candidate_obb'):
        path = getattr(args, name).expanduser()
        if path.is_symlink() or not path.is_file():
            parser.error(name + ' must be a regular file')
        paths.append(path.resolve())
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in paths:
        parser.error('output must be separate from checkout and inputs')
    ref, ref_problems, ref_unresolved = read_build(paths[:2])
    candidate, candidate_problems, candidate_unresolved = read_build(paths[2:])
    hashes = []
    for path in paths:
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    report = {'schema': 1, 'input_sha256': dict(zip(('reference_apk', 'reference_obb', 'candidate_apk', 'candidate_obb'), hashes)),
              'classes': compare(ref, candidate),
              'reference_problems': ref_problems, 'candidate_problems': candidate_problems,
              'reference_unresolved_scripts': len(ref_unresolved), 'candidate_unresolved_scripts': len(candidate_unresolved),
              'reference_unresolved_details': ref_unresolved, 'candidate_unresolved_details': candidate_unresolved,
              'limitations': 'Names and counts only; unresolved scripts, bundles, fields, references, code and gameplay are not compared.'}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared selected names; report remains private. Unresolved scripts:', len(ref_unresolved), len(candidate_unresolved))


if __name__ == '__main__':
    main()
