import copy
import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('attack_prefabs', Path(__file__).resolve().parents[1] / 'compare-enemy-attack-prefabs.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def document():
    return {'schema': 1, 'unity_version': '2023.2.22f1',
            'enemies': [{'source_group': f'enemy-{i}', 'external_files': ['target-file'],
                         'data': {'m_Name': f'enemy-{i}', 'specialAttacks': [
                             {'attackName': 'Attack', 'attackPrefab': {'m_FileID': 1, 'm_PathID': 10}}]}}
                        for i in range(53)]}


class AttackPrefabTests(unittest.TestCase):
    def test_resolves_local_external_and_null_pointers(self):
        data = document()
        data['enemies'][1]['data']['specialAttacks'][0]['attackPrefab'] = {'m_FileID': 0, 'm_PathID': 5}
        data['enemies'][2]['data']['specialAttacks'][0]['attackPrefab'] = {'m_FileID': 0, 'm_PathID': 0}
        result = module.targets(data)
        self.assertEqual((result[('enemy-0', 0)]['group'], result[('enemy-0', 0)]['path_id']), ('target-file', 10))
        self.assertEqual(result[('enemy-1', 0)]['group'], 'enemy-1')
        self.assertIsNone(result[('enemy-2', 0)]['target'])
        data['enemies'][0]['data']['specialAttacks'][0]['attackPrefab']['m_FileID'] = 2
        with self.assertRaises(ValueError):
            module.targets(data)

    def test_detects_root_component_changes(self):
        reference = {('enemy', 0): {'attack': 'Attack', 'target': 'Root', 'components': [['Transform'], ['MonoBehaviour', 'A', '', 'Strike']]}}
        candidate = copy.deepcopy(reference)
        candidate[('enemy', 0)]['components'][1][-1] = 'OtherStrike'
        result = module.compare(reference, candidate)
        self.assertEqual(result['reference_attacks'], 1)
        self.assertEqual(result['differences'][0]['enemy'], 'enemy')

    def test_detects_child_hierarchy_changes(self):
        tree = {'name': 'Root', 'children': [{'name': 'Child', 'local_position': [0, 1, 0], 'children': []}]}
        reference = {('enemy', 0): {'attack': 'Attack', 'target': 'Root', 'hierarchy': tree}}
        candidate = copy.deepcopy(reference)
        candidate[('enemy', 0)]['hierarchy']['children'][0]['local_position'][1] = 2
        result = module.compare(reference, candidate)
        self.assertEqual(result['reference_object_occurrences'], 2)
        self.assertEqual(len(result['differences']), 1)

    def test_physics_fields_preserve_values_but_skip_object_pointers(self):
        fields = {'m_IsTrigger': True, 'm_Size': {'x': 1.0, 'y': 2.0, 'z': 3.0},
                  'm_Material': {'m_FileID': 1, 'm_PathID': 99}}
        normalized = module.without_pointers(fields)
        self.assertEqual(normalized['m_Size']['y'], 2.0)
        self.assertTrue(normalized['m_IsTrigger'])
        self.assertEqual(normalized['m_Material'], {'$unity_pointer': True})
        changed = copy.deepcopy(fields)
        changed['m_Material']['m_PathID'] = 7
        self.assertEqual(module.without_pointers(changed), normalized)
        changed['m_Size']['y'] = 4.0
        self.assertNotEqual(module.without_pointers(changed), normalized)

    def test_combat_script_fields_are_selected_and_changes_are_detected(self):
        self.assertEqual(module.combat_script_name('Assembly-CSharp', '', 'EnemyGroundSlam'), 'EnemyGroundSlam')
        self.assertIsNone(module.combat_script_name('OtherAssembly', '', 'EnemyGroundSlam'))
        self.assertIsNone(module.combat_script_name('Assembly-CSharp', '', 'AudioPauser'))
        tree = {'name': 'Root', 'children': [],
                'combat': [{'script': 'EnemyGroundSlam', 'fields': {'delay': 1.0}}]}
        reference = {('enemy', 0): {'attack': 'Attack', 'target': 'Root', 'hierarchy': tree}}
        candidate = copy.deepcopy(reference)
        candidate[('enemy', 0)]['hierarchy']['combat'][0]['fields']['delay'] = 2.0
        result = module.compare(reference, candidate)
        self.assertEqual(result['reference_combat_occurrences'], {'EnemyGroundSlam': 1})
        self.assertEqual(len(result['differences']), 1)

    def test_detects_direct_effect_target_hierarchy_change(self):
        effect = {'name': 'Effect', 'local_scale': [4.0, 4.0, 4.0], 'children': []}
        tree = {'name': 'Root', 'children': [], 'combat': [
            {'script': 'EnemyGroundSlam', 'fields': {}, 'effect_targets': {'hitEffect': effect, 'attackEffectPrefab': None}}]}
        reference = {('enemy', 0): {'attack': 'Attack', 'target': 'Root', 'hierarchy': tree}}
        candidate = copy.deepcopy(reference)
        candidate[('enemy', 0)]['hierarchy']['combat'][0]['effect_targets']['hitEffect']['local_scale'][0] = 6.0
        result = module.compare(reference, candidate)
        self.assertEqual(result['reference_effect_occurrences'], {'attackEffectPrefab_null': 1, 'hitEffect_resolved': 1})
        self.assertEqual(len(result['differences']), 1)

    def test_rejects_inventory_from_different_apk_or_obb(self):
        with tempfile.TemporaryDirectory() as temp:
            apk, obb = Path(temp) / 'one.apk', Path(temp) / 'one.obb'
            apk.write_bytes(b'apk')
            obb.write_bytes(b'obb')
            data = document()
            data['input_sha256'] = ['wrong', 'wrong']
            with self.assertRaisesRegex(ValueError, 'does not match'):
                module.read_build(apk, obb, data, Path(temp))


if __name__ == '__main__':
    unittest.main()
