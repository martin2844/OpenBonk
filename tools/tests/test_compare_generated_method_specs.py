import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('compare_specs', Path(__file__).resolve().parents[1] / 'compare-generated-method-specs.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class CompareGeneratedMethodSpecsTests(unittest.TestCase):
    def test_resolves_reordered_method_indices_but_preserves_generic_indices(self):
        first = 'const Il2CppMethodSpec g_Il2CppMethodSpecTable[2] = {{ 0, 4, -1 }, { 1, -1, 7 },};'
        second = 'const Il2CppMethodSpec g_Il2CppMethodSpecTable[2] = {{ 1, 4, -1 }, { 0, -1, 7 },};'
        self.assertEqual(module.normalized_specs(first, ['a', 'b']), module.normalized_specs(second, ['b', 'a']))
        self.assertNotEqual(module.normalized_specs(first, ['a', 'b']),
                            module.normalized_specs(second.replace('-1, 7', '-1, 8'), ['b', 'a']))

    def test_rejects_incomplete_extra_and_invalid_rows(self):
        for body in ('{0, 1, -1}', '{1, 1, -1},', '{0, -2, -1},', '{0, 1, -1}, unexpected'):
            with self.subTest(body=body), self.assertRaises(ValueError):
                module.normalized_specs('const Il2CppMethodSpec g_Il2CppMethodSpecTable[1] = {' + body + '};', ['a'])
        with self.assertRaises(ValueError): module.normalized_specs('missing', ['a'])


if __name__ == '__main__': unittest.main()
