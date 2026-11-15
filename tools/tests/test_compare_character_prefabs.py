import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('character_prefabs', Path(__file__).resolve().parents[1] / 'compare-character-prefabs.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def document():
    null = {'m_FileID': 0, 'm_PathID': 0}
    rows = []
    for i in range(21):
        data = {name: copy.deepcopy(null) for name in module.characters.FIELDS if name != 'audioFootsteps'}
        data.update(eCharacter=i, prefab={'m_FileID': 1, 'm_PathID': i + 1}, audioFootsteps=[])
        rows.append({'source_group': 'sharedassets1.assets', 'external_files': ['model'], 'data': data})
    return {'schema': 1, 'unity_version': '2023.2.22f1', 'characters': rows}


class CharacterPrefabTests(unittest.TestCase):
    def test_maps_prefab_references_and_rejects_duplicate_character(self):
        d = document()
        self.assertEqual(module.targets(d)[('0', 0)]['group'], 'model')
        self.assertEqual(module.targets(d)[('0', 0)]['path_id'], 1)
        d['characters'][1]['data']['eCharacter'] = 0
        with self.assertRaises(ValueError): module.targets(d)

    def test_comparison_detects_a_changed_child_component(self):
        child = {'name': 'child', 'children': [], 'components': [['MeshRenderer']]}
        parent = {'name': 'model', 'children': [child], 'components': [['Transform']]}
        reference = {('0', 0): {'attack': 'character', 'target': 'model', 'hierarchy': parent}}
        candidate = copy.deepcopy(reference)
        candidate[('0', 0)]['hierarchy']['children'][0]['components'] = [['SkinnedMeshRenderer']]
        report = module.compare(reference, candidate)
        self.assertEqual(report['reference_object_occurrences'], 2)
        self.assertEqual(report['differences'][0]['character'], '0')
        self.assertEqual(len(report['differences']), 1)


if __name__ == '__main__':
    unittest.main()
