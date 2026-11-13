import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'experimental-packed-steam-audio.py'
spec = importlib.util.spec_from_file_location('packed_audio_repack', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RepackTests(unittest.TestCase):
    def test_replaces_and_realigns_preserving_gaps_and_unmatched(self):
        original = b'GAP!' + b'old' + b'gap' + b'keep' + b'end'
        result, offsets = module.repack(original, [(2, 10, 4, None), (1, 4, 3, b'replacement')])
        self.assertEqual(offsets, {1: 16, 2: 32})
        self.assertEqual(result[:4], b'GAP!')
        self.assertEqual(result[16:27], b'replacement')
        self.assertEqual(result[27:30], b'gap')
        self.assertEqual(result[32:], b'keepend')

    def test_rejects_overlapping_and_out_of_bounds_ranges(self):
        for rows in ([(1, 0, 4, b'x'), (2, 3, 1, b'y')], [(1, 2, 9, b'x')],
                     [(1, 0, 1, b'x'), (1, 1, 1, b'y')]):
            with self.subTest(rows=rows), self.assertRaises(ValueError):
                module.repack(b'12345', rows)


if __name__ == '__main__':
    unittest.main()
