import importlib.util
from collections import Counter
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('generated_methods', Path(__file__).resolve().parents[1] / 'compare-generated-method-signatures.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class GeneratedMethodSignatureTests(unittest.TestCase):
    def test_type_indices_can_move_when_corresponding_definitions_match(self):
        identity = (b'Game.dll', b'Game', b'Fox', b'Run', 1, 3, 4, 5, 1)
        first = [(identity, 0, -1, ((b'speed', 4, 1),))]
        second = [(identity, 1, -1, ((b'speed', 8, 0),))]
        one = [('ret', 'body1'), ('arg', 'body2')]; two = list(reversed(one))
        self.assertEqual(module.signatures(first, one), module.signatures(second, two))
        two[0] = ('arg', 'changed')
        self.assertNotEqual(module.signatures(first, one), module.signatures(second, two))

    def test_invalid_type_indices_fail_closed(self):
        identity = (b'Game.dll', b'', b'Fox', b'Run', 1, 0, 0, 0, 0)
        for index in (-1, 1):
            with self.assertRaises(ValueError): module.signatures([(identity, index, -1, ())], [('type', 'body')])
        with self.assertRaises(ValueError): module.signatures([(identity, 0, -2, ())], [('type', 'body')])

    def test_parameter_names_flags_and_generic_presence_are_retained(self):
        identity = (b'Game.dll', b'', b'Fox', b'Run', 1, 3, 4, 5, 1)
        table = [('ret', 'body1'), ('arg', 'body2')]
        original = (identity, 0, -1, ((b'speed', 4, 1),))
        changed_name = (identity, 0, -1, ((b'value', 4, 1),))
        changed_generic = (identity, 0, 0, original[3])
        changed_flags = (identity[:5] + (7,) + identity[6:], 0, -1, original[3])
        for changed in (changed_name, changed_generic, changed_flags):
            self.assertNotEqual(module.signatures([original], table), module.signatures([changed], table))
        a = Counter(module.signatures([original], table)); b = Counter()
        self.assertEqual(module.unmatched_owners(a, b), [{'image': 'Game.dll', 'namespace': '', 'type': 'Fox', 'count': 1}])


if __name__ == '__main__': unittest.main()
