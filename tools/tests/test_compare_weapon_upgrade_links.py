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


if __name__ == '__main__':
    unittest.main()
