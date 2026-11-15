import importlib.util
from pathlib import Path
import struct
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

    def test_relocates_video_in_preserved_gaps_after_audio_changes(self):
        original = b'head' + b'old' + b'video' + b'keep' + b'tail'
        records = [(1, 4, 3, b'replacement'), (2, 12, 4, None)]
        packed, offsets = module.repack(original, records)
        for start, size in ((0, 4), (7, 5), (16, 4)):
            with self.subTest(start=start):
                relocated = module.relocate_preserved_range(start, size, records, offsets, len(original))
                self.assertEqual(packed[relocated:relocated + size], original[start:start + size])
        for start, size in ((6, 2), (11, 2), (19, 2), (-1, 1)):
            with self.subTest(start=start), self.assertRaises(ValueError):
                module.relocate_preserved_range(start, size, records, offsets, len(original))

    def test_video_offset_field_rejects_ambiguous_or_wrong_layout(self):
        name = b'sharedassets1.resource'
        header = struct.pack('<I', len(name)) + name
        header += bytes((-len(header)) % 4)
        encoded = header + struct.pack('<QQ', 1234, 567)
        body = b'prefix' + encoded + b'suffix'
        field = module.video_offset_field(body, name.decode(), 1234, 567)
        self.assertEqual(struct.unpack_from('<QQ', body, field), (1234, 567))
        with self.assertRaises(ValueError):
            module.video_offset_field(body + encoded, name.decode(), 1234, 567)
        with self.assertRaises(ValueError):
            module.video_offset_field(body, name.decode(), 1234, 568)


if __name__ == '__main__':
    unittest.main()
