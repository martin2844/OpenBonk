import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('weapon_links', Path(__file__).resolve().parents[1] / 'compare-weapon-upgrade-links.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def inventories():
    header = {'schema': 1, 'unity_version': '2023.2.22f1'}
    upgrades = {**header, 'upgrades': [{'source_group': f'asset-{i}', 'path_id': 1,
                                       'data': {'m_Name': f'upgrade-{i}'}} for i in range(29)]}
    weapons = {**header, 'weapons': [{'source_group': f'weapon-{i}', 'path_id': 1,
                                      'external_files': [f'asset-{i % 29}'],
                                      'data': {'eWeapon': i, 'upgradeData': {'m_FileID': 1, 'm_PathID': 1}}}
                                     for i in range(30)]}
    return weapons, upgrades


class CompareWeaponLinksTests(unittest.TestCase):
    def test_matches_weapon_ids_to_target_names_across_different_groups(self):
        weapons, upgrades = inventories()
        other_weapons, other_upgrades = copy.deepcopy(weapons), copy.deepcopy(upgrades)
        other_weapons['weapons'][3]['external_files'][0] = 'asset-4'
        result = module.compare(weapons, upgrades, other_weapons, other_upgrades)
        self.assertEqual(result['reference_unique_upgrades'], 29)
        self.assertEqual(result['differences'], [{'weapon_id': 3, 'reference': 'upgrade-3', 'candidate': 'upgrade-4'}])

    def test_rejects_missing_or_invalid_targets(self):
        weapons, upgrades = inventories()
        weapons['weapons'][0]['data']['upgradeData']['m_FileID'] = 2
        with self.assertRaises(ValueError):
            module.links(weapons, upgrades)

    def test_map_stage_order_and_unresolved_references(self):
        header = {'schema': 1, 'unity_version': '2023.2.22f1'}
        stages = {**header, 'stages': [{'source_group': f'asset-{i}', 'path_id': 1,
                                       'data': {'m_Name': f'stage-{i}'}} for i in range(9)]}
        maps = {**header, 'maps': [{'source_group': f'map-{i}', 'external_files': ['asset-0', 'asset-1'],
                                   'data': {'eMap': i, 'stages': [{'m_FileID': 1, 'm_PathID': 1},
                                                                  {'m_FileID': 2, 'm_PathID': 1}]}}
                                  for i in range(5)]}
        candidate = copy.deepcopy(maps)
        candidate['maps'][0]['data']['stages'].reverse()
        result = module.compare_maps(maps, stages, candidate, stages)
        self.assertEqual(result['reference_stage_links'], 10)
        self.assertEqual(result['differences'][0]['reference'], ['stage-0', 'stage-1'])
        self.assertEqual(result['differences'][0]['candidate'], ['stage-1', 'stage-0'])
        candidate['maps'][0]['data']['stages'][0]['m_PathID'] = 999
        with self.assertRaises(ValueError):
            module.map_links(candidate, stages)


if __name__ == '__main__':
    unittest.main()
