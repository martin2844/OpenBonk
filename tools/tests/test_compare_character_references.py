import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('character_references', Path(__file__).resolve().parents[1] / 'compare-character-references.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def document():
    null = {'m_FileID': 0, 'm_PathID': 0}
    rows = []
    for i in range(21):
        data = {name: copy.deepcopy(null) for name in module.FIELDS if name != 'audioFootsteps'}
        data.update(eCharacter=i, icon={'m_FileID': 1, 'm_PathID': i + 1}, audioFootsteps=[copy.deepcopy(null)])
        rows.append({'source_group': 'sharedassets1.assets', 'external_files': ['portrait'], 'data': data})
    return {'schema': 1, 'unity_version': '2023.2.22f1', 'characters': rows}


class CharacterReferenceTests(unittest.TestCase):
    def test_resolves_selected_pointers_and_preserves_null_list_entries(self):
        result = module.targets(document())
        self.assertEqual(result[0]['icon'], ('portrait', 1))
        self.assertEqual(result[0]['audioFootsteps'], [None])
        self.assertIsNone(result[0]['passive'])

    def test_rejects_duplicate_ids_wrong_group_and_invalid_footsteps(self):
        d = document()
        d['characters'][1]['data']['eCharacter'] = 0
        with self.assertRaises(ValueError): module.targets(d)
        d = document()
        d['characters'][0]['source_group'] = 'other'
        with self.assertRaises(ValueError): module.targets(d)
        d = document()
        d['characters'][0]['data']['audioFootsteps'] = {}
        with self.assertRaises(ValueError): module.targets(d)

    def test_detects_changed_script_and_footstep_order(self):
        fields = {name: None for name in module.FIELDS}
        fields['passive'] = {'type': 'MonoBehaviour', 'name': 'passive', 'script': ['assembly', 'namespace', 'class']}
        fields['audioFootsteps'] = ['left', 'right']
        reference = {0: fields}
        candidate = copy.deepcopy(reference)
        candidate[0]['passive']['script'][2] = 'other'
        candidate[0]['audioFootsteps'].reverse()
        self.assertEqual({row['field'] for row in module.compare(reference, candidate)}, {'passive', 'audioFootsteps'})
        with self.assertRaises(ValueError): module.compare(reference, {})

    def test_detects_changed_mesh_buffer_with_same_identity(self):
        fields = {name: None for name in module.FIELDS}
        fields['meshDefault'] = {'type': 'Mesh', 'name': 'model',
                                 'geometry': {'vertex_sha256': 'original', 'index_sha256': 'same'}}
        reference = {0: fields}
        candidate = copy.deepcopy(reference)
        candidate[0]['meshDefault']['geometry']['vertex_sha256'] = 'changed'
        self.assertEqual([row['field'] for row in module.compare(reference, candidate)], ['meshDefault'])


if __name__ == '__main__':
    unittest.main()
