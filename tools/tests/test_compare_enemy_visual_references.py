import copy
import importlib.util
from pathlib import Path
from types import SimpleNamespace
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

    def test_detects_animation_fps_and_frame_order(self):
        reference = {'enemy': {'material': None, 'animation': {'fps': 7,
                     'animations': [{'name': 'run', 'meshes': ['frame1', 'frame2']}]}}}
        candidate = copy.deepcopy(reference)
        candidate['enemy']['animation']['animations'][0]['meshes'].reverse()
        self.assertEqual(module.compare(reference, candidate)[0]['field'], 'animation')
        candidate = copy.deepcopy(reference)
        candidate['enemy']['animation']['fps'] = 8
        self.assertEqual(module.compare(reference, candidate)[0]['field'], 'animation')

    def test_mesh_buffer_signature_rejects_missing_and_streamed_data(self):
        channel = SimpleNamespace(stream=0, offset=0, format=0, dimension=3)
        mesh = SimpleNamespace(m_Name='frame', m_MeshCompression=0, m_StreamData=None,
                               m_IndexBuffer=[0, 1, 2], m_IndexFormat=0,
                               m_VertexData=SimpleNamespace(m_DataSize=b'abc', m_VertexCount=1, m_Channels=[channel]))
        signature = module.mesh_signature(mesh)
        mesh.m_VertexData.m_DataSize = b'abd'
        self.assertNotEqual(signature, module.mesh_signature(mesh))
        mesh.m_VertexData.m_DataSize = b''
        with self.assertRaises(ValueError):
            module.mesh_signature(mesh)
        mesh.m_VertexData.m_DataSize = b'abc'
        mesh.m_StreamData = SimpleNamespace(path='mesh.resS')
        with self.assertRaises(ValueError):
            module.mesh_signature(mesh)


if __name__ == '__main__':
    unittest.main()
