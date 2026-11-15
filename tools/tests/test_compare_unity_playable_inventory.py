from collections import Counter, defaultdict
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('playable_inventory', Path(__file__).resolve().parents[1] / 'compare-unity-playable-inventory.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PlayableInventoryTests(unittest.TestCase):
    def test_script_resolution_distinguishes_local_global_and_unknown_references(self):
        local = {7: ('Unity.Localization', 'LocalizationSettings')}
        global_scripts = {7: ('Assembly-CSharp', 'CharacterData')}
        self.assertEqual(module.script_identity(0, 7, None, local, global_scripts), local[7])
        self.assertEqual(module.script_identity(1, 7, 'globalgamemanagers.assets', local, global_scripts), global_scripts[7])
        self.assertIsNone(module.script_identity(1, 7, 'other.assets', local, global_scripts))
        self.assertIsNone(module.script_identity(0, 8, None, local, global_scripts))

    def test_counts_duplicate_names_and_both_sides_of_difference(self):
        reference = defaultdict(Counter, {'WeaponData': Counter({'same': 2, 'old': 1})})
        candidate = defaultdict(Counter, {'WeaponData': Counter({'same': 1, 'new': 1})})
        result = module.compare(reference, candidate)
        self.assertEqual(result['WeaponData'], {'reference_count': 3, 'candidate_count': 2,
                                                'reference_only': {'old': 1, 'same': 1},
                                                'candidate_only': {'new': 1}})
        self.assertEqual(result['ItemData']['reference_count'], 0)
        self.assertEqual(result['ItemData']['candidate_count'], 0)


if __name__ == '__main__':
    unittest.main()
