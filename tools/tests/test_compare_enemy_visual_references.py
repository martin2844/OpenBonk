import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('enemy_visual_references', Path(__file__).resolve().parents[1] / 'compare-enemy-visual-references.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def inventory():
    return {'schema': 1, 'unity_version': '2023.2.22f1', 'enemies': [
        {'source_group': 'local', 'external_files': ['external'], 'data': {
            'm_Name': 'enemy' + str(i), 'material': {'m_FileID': 1, 'm_PathID': i + 1},
            'animation': {'m_FileID': 0, 'm_PathID': i + 10}}} for i in range(53)]}


class EnemyVisualReferenceTests(unittest.TestCase):
    def test_resolves_each_pointer_against_its_source_table(self):
        targets = module.targets(inventory())
        self.assertEqual(targets['enemy0']['material'], ('external', 1))
        self.assertEqual(targets['enemy0']['animation'], ('local', 10))

    def test_rejects_duplicate_names_and_bad_pointer(self):
        document = inventory()
        document['enemies'][1]['data']['m_Name'] = 'enemy0'
        with self.assertRaises(ValueError):
            module.targets(document)
        document = inventory()
        document['enemies'][0]['data']['animation']['m_FileID'] = 2
        with self.assertRaises(ValueError):
            module.targets(document)

    def test_detects_changed_material_and_animation_script(self):
        reference = {'enemy': {'material': {'type': 'Material', 'name': 'skin'},
                               'animation': {'type': 'MonoBehaviour', 'name': 'walk', 'script': ['a', 'b', 'c']}}}
        candidate = copy.deepcopy(reference)
        candidate['enemy']['material']['name'] = 'other'
        candidate['enemy']['animation']['script'][2] = 'different'
        self.assertEqual({row['field'] for row in module.compare(reference, candidate)}, {'material', 'animation'})
        with self.assertRaises(ValueError):
            module.compare(reference, {})


if __name__ == '__main__':
    unittest.main()
