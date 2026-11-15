#!/usr/bin/env python3
"""Compare selected character target identities in private APK/OBB pairs.

Uses hash-bound inventories from extract-unity-weapon-data.py --kind character.
Target identity does not establish equal contents, animation, audio or gameplay.
Inputs and output can contain game-derived information and must stay local.
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
FIELDS = {'icon': 'Texture2D', 'prefab': 'GameObject', 'weapon': 'MonoBehaviour',
          'passive': 'MonoBehaviour', 'achievementRequirement': 'MonoBehaviour',
          'meshDefault': 'Mesh', 'meshLow': 'Mesh', 'meshLowest': 'Mesh',
          'themeSong': 'MonoBehaviour', 'audioFootsteps': 'AudioClip'}


def targets(document):
    if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
        raise ValueError('unsupported inventory')
    rows = document.get('characters')
    if not isinstance(rows, list) or len(rows) != 21:
        raise ValueError('unexpected character count')
    result = {}
    for row in rows:
        data = row['data']
        identity = data['eCharacter']
        if type(identity) is not int or identity in result or row['source_group'] != 'sharedassets1.assets':
            raise ValueError('invalid or duplicate canonical character')
        fields = {}
        for field in FIELDS:
            value = data[field]
            if field == 'audioFootsteps':
                if not isinstance(value, list):
                    raise ValueError('invalid footstep list')
                fields[field] = [item.pointer_key(row, pointer) for pointer in value]
            else:
                fields[field] = item.pointer_key(row, value)
        result[identity] = fields
    return result


def read_build(apk, obb, document):
    hashes = []
    for path in (apk, obb):
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    if document.get('input_sha256') != hashes:
        raise ValueError('inventory does not match APK/OBB inputs')
    pointers = targets(document)
    selected = {'globalgamemanagers.assets'}
    for fields in pointers.values():
        for value in fields.values():
            for key in value if isinstance(value, list) else [value]:
                if key is not None:
                    selected.add(key[0])
    objects, found = {}, set()
    for path in (apk, obb):
        with zipfile.ZipFile(path) as archive:
            for group, parts in inventory.asset_groups(archive).items():
                if group not in selected:
                    continue
                if group in found:
                    raise ValueError('duplicate serialized group')
                found.add(group)
                for obj in UnityPy.load(b''.join(archive.read(part) for part in parts)).objects:
                    key = (group, obj.path_id)
                    if key in objects:
                        raise ValueError('duplicate serialized object')
                    objects[key] = obj
    if found != selected:
        raise ValueError('referenced serialized group missing')

    def describe(key, expected):
        if key is None:
            return None
        obj = objects.get(key)
        if obj is None or obj.type.name != expected or obj.assets_file.unity_version != '2023.2.22f1':
            raise ValueError('unresolved or unexpected character target: expected ' + expected +
                             ', found ' + ('missing' if obj is None else obj.type.name))
        head = obj.parse_monobehaviour_head() if expected == 'MonoBehaviour' else None
        name = head.m_Name if head is not None else obj.read().m_Name
        if not isinstance(name, str) or not name:
            raise ValueError('unnamed character target')
        result = {'type': expected, 'name': name}
        if head is not None:
            external = [str(ref.path).replace('\\', '/').rsplit('/', 1)[-1] for ref in obj.assets_file.externals]
            script_key = item.pointer_key({'source_group': key[0], 'external_files': external},
                                          {'m_FileID': head.m_Script.file_id, 'm_PathID': head.m_Script.path_id})
            script_obj = objects.get(script_key)
            if script_obj is None or script_obj.type.name != 'MonoScript':
                raise ValueError('unresolved target script')
            script = script_obj.read()
            result['script'] = [script.m_AssemblyName, script.m_Namespace, script.m_ClassName]
        return result

    return {identity: {field: ([describe(key, FIELDS[field]) for key in value]
                              if isinstance(value, list) else describe(value, FIELDS[field]))
                       for field, value in fields.items()} for identity, fields in pointers.items()}


def compare(reference, candidate):
    if set(reference) != set(candidate):
        raise ValueError('character ID sets differ')
    return [{'character': identity, 'field': field, 'reference': reference[identity][field],
             'candidate': candidate[identity][field]}
            for identity in sorted(reference) for field in FIELDS
            if reference[identity][field] != candidate[identity][field]]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'reference-characters', 'candidate-apk', 'candidate-obb', 'candidate-characters', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    paths = [getattr(args, name).expanduser() for name in ('reference_apk', 'reference_obb', 'reference_characters', 'candidate_apk', 'candidate_obb', 'candidate_characters')]
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
    hashes = []
    for path in paths:
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result = {'schema': 1, 'character_count': len(reference), 'reference': reference, 'candidate': candidate,
              'differences': differences, 'input_sha256': hashes,
              'limitations': 'Selected target identities only; contents, other pointers, code and gameplay are not compared.'}
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', len(reference), 'characters; differences:', len(differences), '. Report remains private.')


if __name__ == '__main__':
    main()
