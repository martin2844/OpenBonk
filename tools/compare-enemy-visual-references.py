#!/usr/bin/env python3
"""Compare enemy material and animation target identities in local APK/OBB pairs.

The inventories must come from extract-unity-weapon-data.py --kind enemy.
This compares target identities, not asset contents, animation behavior or
rendering. Inputs and the output can contain game-derived data and stay local.
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


def read_build(apk, obb, document):
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
    for path in (apk, obb):
        with zipfile.ZipFile(path) as archive:
            for group, parts in inventory.asset_groups(archive).items():
                if group not in selected:
                    continue
                if group in found:
                    raise ValueError('duplicate serialized group')
                found.add(group)
                raw = b''.join(archive.read(part) for part in parts)
                for obj in UnityPy.load(raw).objects:
                    key = (group, obj.path_id)
                    if key in objects:
                        raise ValueError('duplicate serialized object')
                    objects[key] = obj
    if found != selected:
        raise ValueError('referenced serialized group missing')
    result = {}
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
            result[enemy][field] = value
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
    args = parser.parse_args()
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
    reference = read_build(paths[0], paths[1], json.loads(paths[2].read_text(encoding='utf-8')))
    candidate = read_build(paths[3], paths[4], json.loads(paths[5].read_text(encoding='utf-8')))
    differences = compare(reference, candidate)
    input_hashes = []
    for path in paths:
        with path.open('rb') as stream:
            input_hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result = {'schema': 1, 'enemy_count': len(reference), 'reference': reference, 'candidate': candidate,
              'differences': differences, 'input_sha256': input_hashes,
              'limitations': 'Target identities only; material and animation contents, code and rendering are not compared.'}
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', len(reference), 'enemies; differences:', len(differences), '. Report remains private.')


if __name__ == '__main__':
    main()
