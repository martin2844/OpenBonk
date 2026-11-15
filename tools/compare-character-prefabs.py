#!/usr/bin/env python3
"""Compare selected character prefab hierarchies in private APK/OBB pairs.

Uses source-hash-bound inventories from extract-unity-weapon-data.py --kind
character. Checks object names, active states, layers, local transforms,
component identities and selected non-pointer physics fields. Other fields,
referenced assets, code, animations and rendering are not checked. Reports
contain game-derived information and remain local. Requires UnityPy.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent


def module(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


prefab = module('openbonk_prefab_comparison', 'tools/compare-enemy-attack-prefabs.py')
characters = module('openbonk_character_references', 'tools/compare-character-references.py')


def targets(document):
    result = {}
    for character, fields in characters.targets(document).items():
        key = fields['prefab']
        # The reusable hierarchy reader identifies records by (name, index).
        result[(str(character), 0)] = ({'attack': 'character', 'target': None} if key is None else
                                       {'attack': 'character', 'group': key[0], 'path_id': key[1]})
    return result


def compare(reference, candidate):
    report = prefab.compare(reference, candidate)
    for difference in report['differences']:
        difference['character'] = difference.pop('enemy')
        difference.pop('index')
    return {'reference_characters': report['reference_attacks'], 'candidate_characters': report['candidate_attacks'],
            'reference_nonnull': report['reference_nonnull'], 'candidate_nonnull': report['candidate_nonnull'],
            'reference_object_occurrences': report['reference_object_occurrences'],
            'candidate_object_occurrences': report['candidate_object_occurrences'],
            'reference_physics_occurrences': report['reference_physics_occurrences'],
            'candidate_physics_occurrences': report['candidate_physics_occurrences'],
            'differences': report['differences']}


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
    reference = prefab.read_build(paths[0], paths[1], json.loads(paths[2].read_text(encoding='utf-8')),
                                  output.parent, target_loader=targets)
    candidate = prefab.read_build(paths[3], paths[4], json.loads(paths[5].read_text(encoding='utf-8')),
                                  output.parent, target_loader=targets)
    report = compare(reference, candidate)
    hashes = []
    for path in paths:
        with path.open('rb') as stream:
            hashes.append(hashlib.file_digest(stream, 'sha256').hexdigest())
    report.update(schema=1, input_sha256=hashes,
                  limitations='Hierarchy, component identities and selected non-pointer physics fields only; other fields, referenced assets, code and rendering are not compared.')
    fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'w', encoding='utf-8') as stream:
        json.dump(report, stream, ensure_ascii=False, indent=2)
        stream.write('\n')
    print('Compared', report['reference_characters'], 'characters; differences:', len(report['differences']), '. Report remains private.')


if __name__ == '__main__':
    main()
