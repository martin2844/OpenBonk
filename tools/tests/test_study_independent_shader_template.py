import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / 'study-independent-shader-template.py'
spec = importlib.util.spec_from_file_location('independent_shader_study', SCRIPT)
study_module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study_module)


class ShaderObject:
    type = SimpleNamespace(name='Shader')

    def __init__(self, start, data, name='Example'):
        self.byte_start, self.byte_size, self.data, self.name = start, len(data), data, name

    def read_typetree(self):
        return {'m_ParsedForm': {'m_Name': self.name}}

    def get_raw_data(self):
        return self.data


class ShaderStudyTest(unittest.TestCase):
    def test_restores_only_existing_template_bytes(self):
        data = b'\x11\x22\x33\x44'
        target = b'HEAD' + data + b'TAIL'
        template = b'HEAD\x11\x00\x33\x00TAIL'
        source = {('Example', study_module.digest(data)): data}
        with patch.object(study_module.UnityPy, 'load', return_value=SimpleNamespace(objects=[ShaderObject(4, data)])):
            result = study_module.study(target, template, source, [4, 4, 4])
        self.assertEqual(result['matched_objects'], 1)
        self.assertEqual(result['masked_nonzero_bytes'], 2)
        self.assertEqual(result['restored_group_sha256'], study_module.digest(template))
        self.assertEqual(result['compression_estimate']['changed_splits'], 1)

    def test_rejects_invalid_split_layout(self):
        with self.assertRaisesRegex(ValueError, 'invalid serialized group splits'):
            study_module.study(b'1234', b'1234', {}, [2, 1])

    def test_rejects_template_bytes_that_do_not_match_source(self):
        data = b'\x11\x22\x33\x44'
        source = {('Example', study_module.digest(data)): data}
        with patch.object(study_module.UnityPy, 'load', return_value=SimpleNamespace(objects=[ShaderObject(4, data)])):
            with self.assertRaisesRegex(ValueError, 'template differs'):
                study_module.study(b'HEAD' + data, b'HEAD\x11\x55\x33\x44', source)

    def test_rejects_invalid_or_overlapping_object_ranges(self):
        data = b'ABCD'
        source = {('Example', study_module.digest(data)): data}
        with patch.object(study_module.UnityPy, 'load', return_value=SimpleNamespace(objects=[ShaderObject(1, data)])):
            with self.assertRaisesRegex(ValueError, 'invalid serialized shader range'):
                study_module.study(b'HEAD' + data, b'HEAD' + data, source)
        objects = [ShaderObject(4, data), ShaderObject(4, data)]
        with patch.object(study_module.UnityPy, 'load', return_value=SimpleNamespace(objects=objects)):
            with self.assertRaisesRegex(ValueError, 'overlapping shader objects'):
                study_module.study(b'HEAD' + data, b'HEAD' + data, source)


if __name__ == '__main__':
    unittest.main()
