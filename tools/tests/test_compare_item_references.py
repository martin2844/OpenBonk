import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('item_references', Path(__file__).resolve().parents[1] / 'compare-item-references.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ItemReferenceTests(unittest.TestCase):
    def test_pointer_resolution_uses_the_source_file_table(self):
        row = {'source_group': 'local', 'external_files': ['external']}
        self.assertEqual(module.pointer_key(row, {'m_FileID': 0, 'm_PathID': 7}), ('local', 7))
        self.assertEqual(module.pointer_key(row, {'m_FileID': 1, 'm_PathID': 7}), ('external', 7))
        self.assertIsNone(module.pointer_key(row, {'m_FileID': 0, 'm_PathID': 0}))
        with self.assertRaises(ValueError):
            module.pointer_key(row, {'m_FileID': 2, 'm_PathID': 7})

    def test_comparison_detects_changed_target_and_missing_item(self):
        reference = {1: {'icon': {'type': 'Texture2D', 'name': 'icon'}, 'unlockRequirement': None}}
        candidate = {1: {'icon': {'type': 'Texture2D', 'name': 'other'}, 'unlockRequirement': None}}
        self.assertEqual(module.compare(reference, candidate)[0]['field'], 'icon')
        with self.assertRaises(ValueError):
            module.compare(reference, {})

    def test_comparison_detects_changed_unlock_script(self):
        reference = {1: {'icon': None, 'unlockRequirement': {'type': 'MonoBehaviour', 'name': 'unlock',
                                                               'script': ['Assembly-CSharp', '', 'Achievement']}}}
        candidate = {1: {'icon': None, 'unlockRequirement': {'type': 'MonoBehaviour', 'name': 'unlock',
                                                               'script': ['Assembly-CSharp', '', 'Other']}}}
        self.assertEqual(module.compare(reference, candidate)[0]['field'], 'unlockRequirement')

    def test_unlock_fields_preserve_scalars_and_ignore_unstable_pointers(self):
        reference = {'targetValue': 5, 'nested': [{'m_FileID': 0, 'm_PathID': 12}]}
        candidate = {'targetValue': 5, 'nested': [{'m_FileID': 1, 'm_PathID': 99}]}
        self.assertEqual(module.without_pointers(reference), module.without_pointers(candidate))
        candidate['targetValue'] = 6
        self.assertNotEqual(module.without_pointers(reference), module.without_pointers(candidate))


if __name__ == '__main__':
    unittest.main()
