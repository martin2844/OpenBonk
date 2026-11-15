import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('characters', Path(__file__).resolve().parents[1] / 'compare-extracted-character-data.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def document():
    return {'schema': 1, 'unity_version': '2023.2.22f1',
            'characters': [{'data': {'eCharacter': i, 'speed': 1.0,
                                     'icon': {'m_FileID': 1, 'm_PathID': i},
                                     'stats': [{'stat': 2, 'modification': 0.5}]}}
                           for i in range(21)]}


class CompareCharacterDataTests(unittest.TestCase):
    def test_compares_fields_but_skips_unstable_unity_pointers(self):
        reference = document()
        candidate = copy.deepcopy(reference)
        candidate['characters'][3]['data']['icon']['m_PathID'] = 999
        candidate['characters'][4]['data']['stats'][0]['modification'] = 0.75
        result = module.compare(reference, candidate)
        self.assertEqual(result['pointers_skipped'], 21)
        self.assertEqual(len(result['differences']), 1)
        self.assertEqual(result['differences'][0]['path'], [4, 'stats', 0, 'modification'])

    def test_rejects_duplicate_character_ids(self):
        candidate = document()
        candidate['characters'][1]['data']['eCharacter'] = 0
        with self.assertRaises(ValueError):
            module.compare(document(), candidate)

    def test_weapon_inventory_uses_weapon_ids_and_expected_count(self):
        weapons = {'schema': 1, 'unity_version': '2023.2.22f1',
                   'weapons': [{'data': {'eWeapon': i, 'damage': 10.0}} for i in range(30)]}
        candidate = copy.deepcopy(weapons)
        candidate['weapons'][5]['data']['damage'] = 12.0
        result = module.compare(weapons, candidate, kind='weapon')
        self.assertEqual(result['reference_weapons'], 30)
        self.assertEqual(result['differences'][0]['path'], [5, 'damage'])
        with self.assertRaises(ValueError):
            module.compare(document(), candidate, kind='weapon')


if __name__ == '__main__':
    unittest.main()
