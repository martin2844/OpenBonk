import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'compare-postprocess-shaders.py'
spec = importlib.util.spec_from_file_location('postprocess_comparison', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ShaderComparisonTest(unittest.TestCase):
    def test_exact_bytes_and_missing_or_different_shaders_are_reported(self):
        class Object:
            type = SimpleNamespace(name='Shader')

            def __init__(self, name, data):
                self.name, self.data, self.byte_size = name, data, len(data)

            def read_typetree(self):
                return {'m_ParsedForm': {'m_Name': self.name}}

            def get_raw_data(self):
                return self.data

        source = module.shader_objects([Object('same', b'abc'), Object('different', b'abc'), Object('missing', b'abc')])
        target = module.shader_objects([Object('same', b'abc'), Object('different', b'xyz')])
        rows = {row['name']: row for row in module.compare(source, target)}
        self.assertEqual(rows['same']['matching_objects'], 1)
        self.assertEqual(rows['different']['matching_objects'], 0)
        self.assertEqual(rows['missing']['target_objects'], 0)


if __name__ == '__main__':
    unittest.main()
