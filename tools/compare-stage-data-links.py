#!/usr/bin/env python3
"""Compare selected stage-to-boss and terrain-parameter references.

Inputs are local inventories from extract-unity-weapon-data.py. The report
contains game-derived names and stays private. Other references, terrain meshes,
enemy behavior and gameplay are not compared.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TYPES = (('boss', 'enemies', 53), ('noise', 'noise', 3), ('parameters', 'map_parameters', 4))


def stage_links(stages, enemies, noise, parameters):
    for document in (stages, enemies, noise, parameters):
        if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
            raise ValueError('unsupported inventory schema or Unity version')
    stage_rows = stages.get('stages')
    if not isinstance(stage_rows, list) or len(stage_rows) != 9:
        raise ValueError('unexpected stage count')
    targets = {}
    for (key, plural, count), document in zip(TYPES, (enemies, noise, parameters)):
        rows = document.get(plural)
        if not isinstance(rows, list) or len(rows) != count:
            raise ValueError('unexpected target count')
        index, names = {}, set()
        for row in rows:
            identity, name = (row['source_group'], row['path_id']), row['data']['m_Name']
            if (not isinstance(identity[0], str) or type(identity[1]) is not int or
                    not isinstance(name, str) or not name or identity in index or name in names):
                raise ValueError('invalid or duplicate target identity')
            index[identity] = name
            names.add(name)
        targets[key] = index
    result = {}
    for row in stage_rows:
        data, external = row['data'], row['external_files']
        name = data['m_Name']
        if not isinstance(name, str) or not name or name in result or not isinstance(external, list):
            raise ValueError('invalid stage identity')
        pointers = {'boss': data['stageTimeline']['boss'], 'noise': data['proceduralNoiseData'],
                    'parameters': data['mapParameters']}
        resolved = {}
        for key, pointer in pointers.items():
            file_id, path_id = pointer['m_FileID'], pointer['m_PathID']
            if type(file_id) is not int or type(path_id) is not int:
                raise ValueError('invalid stage pointer')
            if file_id == 0 and path_id == 0:
                resolved[key] = None
                continue
            if file_id == 0:
                group = row['source_group']
            elif 0 < file_id <= len(external):
                group = external[file_id - 1]
            else:
                raise ValueError('unresolved target file')
            target = targets[key].get((group, path_id))
            if target is None:
                raise ValueError('unresolved stage target')
            resolved[key] = target
        result[name] = resolved
    return result


def compare(reference, candidate):
    left, right = stage_links(*reference), stage_links(*candidate)
    differences = [{'stage': key, 'reference': left.get(key), 'candidate': right.get(key)}
                   for key in sorted(left.keys() | right.keys()) if left.get(key) != right.get(key)]
    def counts(rows):
        return dict(Counter(key for row in rows.values() for key, value in row.items() if value is not None))
    return {'reference_stages': len(left), 'candidate_stages': len(right),
            'reference_nonnull_links': counts(left), 'candidate_nonnull_links': counts(right), 'differences': differences}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for side in ('reference', 'candidate'):
        for name in ('stages', 'enemies', 'noise', 'parameters'):
            parser.add_argument('--' + side + '-' + name, type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    inputs = []
    for side in ('reference', 'candidate'):
        for name in ('stages', 'enemies', 'noise', 'parameters'):
            path = getattr(args, side + '_' + name).expanduser()
            if path.is_symlink() or not path.is_file():
                parser.error('inputs must be regular files')
            inputs.append(path.resolve())
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output in inputs:
        parser.error('output must be separate from checkout and inputs')
    docs = [json.loads(path.read_text(encoding='utf-8')) for path in inputs]
    result = compare(docs[:4], docs[4:])
    result['schema'] = 1
    result['input_sha256'] = []
    for path in inputs:
        with path.open('rb') as stream:
            result['input_sha256'].append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result['limitations'] = 'Only selected boss, noise and map-parameter references are compared; other references, assets, code and gameplay are not checked.'
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', result['reference_stages'], 'stage records; differences:', len(result['differences']), '. Report remains private.')


if __name__ == '__main__':
    main()
