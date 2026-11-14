import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

spec = importlib.util.spec_from_file_location('partial_rgba', Path(__file__).resolve().parents[1] / 'experimental-partial-rgba-plan.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class PartialRgbaPlanTests(unittest.TestCase):
    def test_split_fragments_preserve_different_and_zero_bytes(self):
        parts = [SimpleNamespace(filename='one', file_size=5), SimpleNamespace(filename='two', file_size=5)]
        original = b'A\x00CDE'; converted = b'AxCDz'
        rows = module.fragment_rows(parts, 3, original, converted, 0)
        self.assertEqual(sum(row['masked_bytes'] for row in rows), 3)
        converted_by_id = {0: converted}
        first, second = b'123' + original[:2], original[2:] + b'89'
        masked_first = module.apply(first, rows[:1], converted_by_id)
        masked_second = module.apply(second, rows[1:], converted_by_id)
        self.assertEqual(masked_first, b'123\x00\x00')
        self.assertEqual(masked_second, b'\x00\x00E89')
        self.assertEqual(module.apply(masked_first, rows[:1], converted_by_id, True), first)
        self.assertEqual(module.apply(masked_second, rows[1:], converted_by_id, True), second)

    def test_rejects_changed_converted_bytes_or_template(self):
        parts = [SimpleNamespace(filename='one', file_size=5)]
        rows = module.fragment_rows(parts, 0, b'ABCDE', b'ABCxy', 0)
        masked = module.apply(b'ABCDE', rows, {0: b'ABCxy'})
        with self.assertRaises(ValueError): module.apply(masked, rows, {0: b'XBCxy'}, True)
        with self.assertRaises(ValueError): module.apply(b'X' + masked[1:], rows, {0: b'ABCxy'}, True)
        with self.assertRaises(ValueError): module.apply(masked, rows + rows, {0: b'ABCxy'}, True)

    def test_rejects_nonzero_padding_and_out_of_bounds(self):
        parts = [SimpleNamespace(filename='one', file_size=5)]
        rows = module.fragment_rows(parts, 0, b'ABCDE', b'ABCxy', 0)
        bad = dict(rows[0], mask='/w==')
        with self.assertRaises(ValueError): module.apply(b'ABCDE', [bad], {0: b'ABCxy'})
        with self.assertRaises(ValueError): module.fragment_rows(parts, 3, b'ABCD', b'ABCD', 0)


if __name__ == '__main__': unittest.main()
