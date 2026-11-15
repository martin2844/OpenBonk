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

    def test_upgrade_inventory_matches_by_name_and_rejects_duplicates(self):
        upgrades = {'schema': 1, 'unity_version': '2023.2.22f1',
                    'upgrades': [{'data': {'m_Name': f'upgrade-{i}', 'upgradeModifiers': [i]}} for i in range(29)]}
        candidate = copy.deepcopy(upgrades)
        candidate['upgrades'][3]['data']['upgradeModifiers'][0] = 5
        result = module.compare(upgrades, candidate, kind='upgrade')
        self.assertEqual(result['reference_upgrades'], 29)
        self.assertEqual(result['differences'][0]['path'], ['upgrade-3', 'upgradeModifiers', 0])
        candidate['upgrades'][4]['data']['m_Name'] = 'upgrade-3'
        with self.assertRaises(ValueError):
            module.compare(upgrades, candidate, kind='upgrade')

    def test_map_and_stage_fields_use_correct_identity(self):
        maps = {'schema': 1, 'unity_version': '2023.2.22f1',
                'maps': [{'data': {'eMap': i, 'stageDuration': 100.0}} for i in range(5)]}
        stages = {'schema': 1, 'unity_version': '2023.2.22f1',
                  'stages': [{'data': {'m_Name': f'stage-{i}', 'farClipPlane': 200.0}} for i in range(9)]}
        altered = copy.deepcopy(stages)
        altered['stages'][4]['data']['farClipPlane'] = 201.0
        self.assertEqual(module.compare(maps, maps, kind='map')['reference_maps'], 5)
        self.assertEqual(module.compare(stages, altered, kind='stage')['differences'][0]['path'], ['stage-4', 'farClipPlane'])

    def test_enemy_and_generation_data_compare_by_name(self):
        for kind, plural, count in (('enemy', 'enemies', 53), ('noise', 'noise', 3),
                                    ('map-parameters', 'map_parameters', 4)):
            data = {'schema': 1, 'unity_version': '2023.2.22f1',
                    plural: [{'data': {'m_Name': f'{kind}-{i}', 'value': i}} for i in range(count)]}
            changed = copy.deepcopy(data)
            changed[plural][0]['data']['value'] += 1
            self.assertEqual(module.compare(data, changed, kind=kind)['differences'][0]['path'], [f'{kind}-0', 'value'])


if __name__ == '__main__':
    unittest.main()
