import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('compare_types', Path(__file__).resolve().parents[1] / 'compare-generated-type-tables.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


def source(index=0, tail='0, IL2CPP_TYPE_CLASS, 0, 0, 0, 0'):
    return (f'Il2CppType TestType = {{ (void*){index}, {tail} }};\n'
            'const Il2CppType* const g_Il2CppTypeTable[1] = {\n&TestType,\n};\n')


class CompareGeneratedTypeTableTests(unittest.TestCase):
    def test_reordered_metadata_types_resolve_to_same_identity(self):
        first = module.normalized_type_table(source(0), [('image', 'ns', 'A'), ('image', 'ns', 'B')])
        reordered = module.normalized_type_table(source(1), [('image', 'ns', 'B'), ('image', 'ns', 'A')])
        self.assertEqual(first, reordered)
        self.assertNotEqual(first, module.normalized_type_table(source(1), [('image', 'ns', 'A'), ('image', 'ns', 'B')]))
        self.assertNotEqual(first, module.normalized_type_table(source(0, '1, IL2CPP_TYPE_CLASS, 0, 0, 0, 0'), [('image', 'ns', 'A')]))

    def test_missing_invalid_and_ambiguous_types_are_rejected(self):
        with self.assertRaises(ValueError): module.normalized_type_table(source(2), [('image', 'ns', 'A')])
        with self.assertRaises(ValueError): module.normalized_type_table(source().replace('&TestType,', '&Other,'), [('image', 'ns', 'A')])
        with self.assertRaises(ValueError): module.normalized_type_table(source().replace('&TestType,', '&TestType, unexpected'), [('image', 'ns', 'A')])
        with self.assertRaises(ValueError): module.normalized_type_table(source() + source(), [('image', 'ns', 'A')])


if __name__ == '__main__': unittest.main()
