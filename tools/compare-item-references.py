#!/usr/bin/env python3
"""Compare item icon and unlock target identities in private APK/OBB pairs.

Target names and types do not establish equal asset contents or gameplay.
The input inventories and output contain game-derived information; keep them local.
Requires UnityPy.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('build_inventory', ROOT / 'tools/compare-unity-build-content.py')
inventory = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inventory)


def pointer_key(row, pointer):
    if not isinstance(pointer, dict) or set(pointer) != {'m_FileID', 'm_PathID'}:
        raise ValueError('invalid pointer')
    file_id, path_id = pointer['m_FileID'], pointer['m_PathID']
    if type(file_id) is not int or type(path_id) is not int:
        raise ValueError('invalid pointer values')
    if file_id == 0 and path_id == 0:
        return None
    external = row['external_files']
    if not isinstance(external, list):
        raise ValueError('invalid external file table')
    if file_id == 0:
        group = row['source_group']
    elif 0 < file_id <= len(external):
        group = external[file_id - 1]
    else:
        raise ValueError('unresolved external reference')
    return group, path_id


def compare(reference, candidate):
    if set(reference) != set(candidate):
        raise ValueError('item ID sets differ')
    return [{'item': item, 'field': field, 'reference': reference[item][field],
             'candidate': candidate[item][field]}
            for item in sorted(reference) for field in ('icon', 'unlockRequirement')
            if reference[item][field] != candidate[item][field]]


def read_build(apk, obb, document):
    hashes = []
    for path in (apk, obb):
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1' or document.get('input_sha256') != hashes:
        raise ValueError('inventory does not match APK/OBB inputs')
    rows = document.get('items')
    if not isinstance(rows, list) or len(rows) != 87:
        raise ValueError('unexpected item count')
    targets = {}
    for row in rows:
        item = row['data']['eItem']
        if type(item) is not int or item in targets:
            raise ValueError('invalid or duplicate item ID')
        targets[item] = {field: pointer_key(row, row['data'][field]) for field in ('icon', 'unlockRequirement')}
    selected = {target[0] for fields in targets.values() for target in fields.values() if target is not None}
    selected.add('globalgamemanagers.assets')
    groups = {}
    for path in (apk, obb):
        with zipfile.ZipFile(path) as archive:
            for name, parts in inventory.asset_groups(archive).items():
                if name in selected:
                    if name in groups:
                        raise ValueError('duplicate serialized group')
                    groups[name] = b''.join(archive.read(part) for part in parts)
    if set(groups) != selected:
        raise ValueError('referenced group missing')
    objects = {}
    for name, raw in groups.items():
        for obj in UnityPy.load(raw).objects:
            objects[(name, obj.path_id)] = obj
    result = {}
    for item, fields in targets.items():
        result[item] = {}
        for field, key in fields.items():
            if key is None:
                result[item][field] = None
                continue
            obj = objects.get(key)
            expected = 'Texture2D' if field == 'icon' else 'MonoBehaviour'
            if obj is None or obj.type.name != expected or obj.assets_file.unity_version != '2023.2.22f1':
                raise ValueError('unresolved or unexpected item target')
            head = obj.parse_monobehaviour_head() if field == 'unlockRequirement' else None
            name = obj.read().m_Name if head is None else head.m_Name
            if not isinstance(name, str) or not name:
                raise ValueError('unnamed item target')
            result[item][field] = {'type': expected, 'name': name}
            if head is not None:
                pointer = head.m_Script
                external = [str(ref.path).replace('\\', '/').rsplit('/', 1)[-1] for ref in obj.assets_file.externals]
                script_key = pointer_key({'source_group': key[0], 'external_files': external},
                                         {'m_FileID': pointer.file_id, 'm_PathID': pointer.path_id})
                script_obj = objects.get(script_key)
                if script_obj is None or script_obj.type.name != 'MonoScript':
                    raise ValueError('unresolved unlock script')
                script = script_obj.read()
                result[item][field]['script'] = [script.m_AssemblyName, script.m_Namespace, script.m_ClassName]
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'reference-items', 'candidate-apk', 'candidate-obb', 'candidate-items', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    args = parser.parse_args()
    paths = [getattr(args, name).expanduser() for name in ('reference_apk', 'reference_obb', 'reference_items', 'candidate_apk', 'candidate_obb', 'candidate_items')]
    if any(path.is_symlink() or not path.is_file() for path in paths):
        parser.error('inputs must be regular files')
    paths = [path.resolve() for path in paths]
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in paths:
        parser.error('output must be separate from checkout and inputs')
    reference = read_build(paths[0], paths[1], json.loads(paths[2].read_text(encoding='utf-8')))
    candidate = read_build(paths[3], paths[4], json.loads(paths[5].read_text(encoding='utf-8')))
    differences = compare(reference, candidate)
    result = {'schema': 1, 'item_count': len(reference), 'reference': reference, 'candidate': candidate,
              'differences': differences, 'limitations': 'Target type, name and unlock script identity only; contents, code and behavior are not compared.'}
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', len(reference), 'items; differences:', len(differences), '. Report remains private.')


if __name__ == '__main__':
    main()
