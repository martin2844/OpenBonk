#!/usr/bin/env python3
"""Compare weapon-to-upgrade or map-to-stage links in decoded inventories.

Requires the relevant reports generated from each APK/OBB pair by
extract-unity-weapon-data.py. Reports contain game-derived names and stay local.
This checks selected pointers, not prefabs, effects or gameplay behavior.
"""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def links(weapons, upgrades):
    for document in (weapons, upgrades):
        if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
            raise ValueError('unsupported inventory schema or Unity version')
    weapon_rows, upgrade_rows = weapons.get('weapons'), upgrades.get('upgrades')
    if not isinstance(weapon_rows, list) or len(weapon_rows) != 30 or not isinstance(upgrade_rows, list) or len(upgrade_rows) != 29:
        raise ValueError('unexpected weapon or upgrade count')
    targets, names = {}, set()
    for row in upgrade_rows:
        key = (row['source_group'], row['path_id'])
        name = row['data']['m_Name']
        if not isinstance(key[0], str) or type(key[1]) is not int or not isinstance(name, str) or not name or key in targets or name in names:
            raise ValueError('invalid or duplicate upgrade identity')
        targets[key] = name
        names.add(name)
    result = {}
    for row in weapon_rows:
        data = row['data']
        weapon = data['eWeapon']
        pointer = data['upgradeData']
        file_id, path_id = pointer['m_FileID'], pointer['m_PathID']
        external = row['external_files']
        if type(weapon) is not int or weapon in result or type(file_id) is not int or type(path_id) is not int or not isinstance(external, list):
            raise ValueError('invalid weapon or pointer')
        if file_id == 0:
            group = row['source_group']
        elif 0 < file_id <= len(external):
            group = external[file_id - 1]
        else:
            raise ValueError('unresolved weapon upgrade file')
        target = targets.get((group, path_id))
        if target is None:
            raise ValueError('unresolved weapon upgrade target')
        result[weapon] = target
    return result


def compare(ref_weapons, ref_upgrades, candidate_weapons, candidate_upgrades):
    reference = links(ref_weapons, ref_upgrades)
    candidate = links(candidate_weapons, candidate_upgrades)
    differences = [{'weapon_id': key, 'reference': reference.get(key), 'candidate': candidate.get(key)}
                   for key in sorted(reference.keys() | candidate.keys()) if reference.get(key) != candidate.get(key)]
    return {'reference_weapons': len(reference), 'candidate_weapons': len(candidate),
            'reference_unique_upgrades': len(set(reference.values())),
            'candidate_unique_upgrades': len(set(candidate.values())), 'differences': differences}


def map_links(maps, stages):
    for document in (maps, stages):
        if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
            raise ValueError('unsupported inventory schema or Unity version')
    map_rows, stage_rows = maps.get('maps'), stages.get('stages')
    if not isinstance(map_rows, list) or len(map_rows) != 5 or not isinstance(stage_rows, list) or len(stage_rows) != 9:
        raise ValueError('unexpected map or stage count')
    targets, names = {}, set()
    for row in stage_rows:
        key, name = (row['source_group'], row['path_id']), row['data']['m_Name']
        if not isinstance(key[0], str) or type(key[1]) is not int or not isinstance(name, str) or not name or key in targets or name in names:
            raise ValueError('invalid or duplicate stage identity')
        targets[key] = name
        names.add(name)
    result = {}
    for row in map_rows:
        data, external = row['data'], row['external_files']
        map_id, pointers = data['eMap'], data['stages']
        if type(map_id) is not int or map_id in result or not isinstance(pointers, list) or not isinstance(external, list):
            raise ValueError('invalid map or stages')
        result[map_id] = []
        for pointer in pointers:
            file_id, path_id = pointer['m_FileID'], pointer['m_PathID']
            if type(file_id) is not int or type(path_id) is not int:
                raise ValueError('invalid stage pointer')
            if file_id == 0:
                group = row['source_group']
            elif 0 < file_id <= len(external):
                group = external[file_id - 1]
            else:
                raise ValueError('unresolved stage file')
            target = targets.get((group, path_id))
            if target is None:
                raise ValueError('unresolved stage target')
            result[map_id].append(target)
    return result


def compare_maps(ref_maps, ref_stages, candidate_maps, candidate_stages):
    reference = map_links(ref_maps, ref_stages)
    candidate = map_links(candidate_maps, candidate_stages)
    differences = [{'map_id': key, 'reference': reference.get(key), 'candidate': candidate.get(key)}
                   for key in sorted(reference.keys() | candidate.keys()) if reference.get(key) != candidate.get(key)]
    return {'reference_maps': len(reference), 'candidate_maps': len(candidate),
            'reference_stage_links': sum(map(len, reference.values())),
            'candidate_stage_links': sum(map(len, candidate.values())), 'differences': differences}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--kind', choices=('weapon', 'map'), default='weapon')
    parser.add_argument('--reference-weapons', '--reference-maps', dest='reference_weapons', type=Path, required=True)
    parser.add_argument('--reference-upgrades', '--reference-stages', dest='reference_upgrades', type=Path, required=True)
    parser.add_argument('--candidate-weapons', '--candidate-maps', dest='candidate_weapons', type=Path, required=True)
    parser.add_argument('--candidate-upgrades', '--candidate-stages', dest='candidate_upgrades', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    inputs = []
    for path in (args.reference_weapons, args.reference_upgrades, args.candidate_weapons, args.candidate_upgrades):
        path = path.expanduser()
        if path.is_symlink() or not path.is_file():
            parser.error('inputs must be regular files')
        inputs.append(path.resolve())
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in inputs:
        parser.error('output must be separate from checkout and inputs')
    comparison = compare if args.kind == 'weapon' else compare_maps
    result = comparison(*(json.loads(path.read_text(encoding='utf-8')) for path in inputs))
    result['schema'] = 1
    result['kind'] = args.kind
    result['input_sha256'] = []
    for path in inputs:
        with path.open('rb') as stream:
            result['input_sha256'].append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result['limitations'] = 'Only selected references are compared; other references, assets, code and gameplay are not checked.'
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    count = result['reference_weapons'] if args.kind == 'weapon' else result['reference_maps']
    print('Compared', count, args.kind, 'records; differences:', len(result['differences']), '. Report remains private.')


if __name__ == '__main__':
    main()
