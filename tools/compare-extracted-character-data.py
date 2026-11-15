#!/usr/bin/env python3
"""Compare scalar fields in two decoded gameplay data inventories.

Inputs use the corresponding local extractor's schema. Unity object pointers
are deliberately excluded; matching values do not prove that their targets,
code, animations, assets or gameplay match. Reports can contain game data and
must remain private.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def records(document, kind='character'):
    if document.get('schema') != 1 or document.get('unity_version') != '2023.2.22f1':
        raise ValueError('unsupported character inventory schema or Unity version')
    if kind not in ('character', 'weapon', 'item', 'upgrade', 'map', 'stage', 'enemy', 'noise', 'map-parameters'):
        raise ValueError('unsupported data kind')
    plural, field, expected = {'character': ('characters', 'eCharacter', 21),
                               'weapon': ('weapons', 'eWeapon', 30),
                               'item': ('items', 'eItem', 87),
                               'upgrade': ('upgrades', 'm_Name', 29),
                               'map': ('maps', 'eMap', 5),
                               'stage': ('stages', 'm_Name', 9),
                               'enemy': ('enemies', 'm_Name', 53),
                               'noise': ('noise', 'm_Name', 3),
                               'map-parameters': ('map_parameters', 'm_Name', 4)}[kind]
    rows = document.get(plural)
    if not isinstance(rows, list) or len(rows) != expected:
        raise ValueError('unexpected record count')
    result = {}
    for row in rows:
        data = row.get('data') if isinstance(row, dict) else None
        character = data.get(field) if isinstance(data, dict) else None
        valid = type(character) is int if kind in ('character', 'weapon', 'item', 'map') else isinstance(character, str) and bool(character)
        if not valid or character in result:
            raise ValueError('missing or duplicate data identity')
        result[character] = data
    return result


def compare(reference, candidate, kind='character'):
    left, right = records(reference, kind), records(candidate, kind)
    counts, differences = Counter(), []

    def walk(a, b, path):
        if isinstance(a, dict) and isinstance(b, dict):
            if set(a) == {'m_FileID', 'm_PathID'} and set(b) == set(a):
                counts['pointers_skipped'] += 1
                return
            for key in sorted(a.keys() | b.keys()):
                child = path + [key]
                if key not in a or key not in b:
                    differences.append({'path': child, 'kind': 'missing_key', 'side': 'reference' if key not in a else 'candidate'})
                else:
                    walk(a[key], b[key], child)
        elif isinstance(a, list) and isinstance(b, list):
            if len(a) != len(b):
                differences.append({'path': path, 'kind': 'length', 'reference': len(a), 'candidate': len(b)})
            for index, (x, y) in enumerate(zip(a, b)):
                walk(x, y, path + [index])
        else:
            counts['scalars_compared'] += 1
            if type(a) is not type(b) or a != b:
                differences.append({'path': path, 'kind': 'value', 'reference': a, 'candidate': b})

    for character in sorted(left.keys() | right.keys()):
        if character not in left or character not in right:
            differences.append({'path': [character], 'kind': 'missing_character',
                                'side': 'reference' if character not in left else 'candidate'})
        else:
            walk(left[character], right[character], [character])
    plural = {'character': 'characters', 'weapon': 'weapons', 'item': 'items', 'upgrade': 'upgrades', 'map': 'maps', 'stage': 'stages',
              'enemy': 'enemies', 'noise': 'noise', 'map-parameters': 'map_parameters'}[kind]
    return {'reference_' + plural: len(left), 'candidate_' + plural: len(right),
            'scalars_compared': counts['scalars_compared'], 'pointers_skipped': counts['pointers_skipped'],
            'differences': differences}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('reference', type=Path)
    parser.add_argument('candidate', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--kind', choices=('character', 'weapon', 'item', 'upgrade', 'map', 'stage', 'enemy', 'noise', 'map-parameters'), default='character')
    args = parser.parse_args()
    inputs = []
    for path in (args.reference, args.candidate):
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
    documents = [json.loads(path.read_text(encoding='utf-8')) for path in inputs]
    result = compare(*documents, kind=args.kind)
    result['schema'] = 1
    result['kind'] = args.kind
    result['input_sha256'] = []
    for path in inputs:
        with path.open('rb') as stream:
            result['input_sha256'].append(hashlib.file_digest(stream, 'sha256').hexdigest())
    result['limitations'] = 'Unity object pointers, referenced assets, code, animation and gameplay are not compared.'
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, ensure_ascii=False, indent=2, allow_nan=False)
        stream.write('\n')
    print('Compared', result['scalars_compared'], 'scalars; differences:', len(result['differences']),
          '; object pointers skipped:', result['pointers_skipped'], '. Report remains private.')


if __name__ == '__main__':
    main()
