import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('transparent_scrub', Path(__file__).resolve().parents[1] / 'experimental-transparent-atlas-scrub.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TransparentAtlasScrubTest(unittest.TestCase):
    def test_zeroes_only_transparent_interiors_and_preserves_mips(self):
        width = height = 7
        pixels = bytearray([7, 8, 9, 0] * width * height)
        center = (3 * width + 3) * 4
        pixels[center:center + 4] = bytes([30, 40, 50, 255])
        source = bytearray(pixels)
        payload = bytes(pixels) + b'mip payload'
        result = module.scrub(payload, bytes(source) + b'mip payload', width, height)
        near = (3 * width + 2) * 4
        far = (1 * width + 1) * 4
        self.assertEqual(result[center:center + 4], payload[center:center + 4])
        self.assertEqual(result[near:near + 4], payload[near:near + 4])
        self.assertEqual(result[far:far + 4], bytes(4))
        self.assertEqual(result[-11:], b'mip payload')
        self.assertEqual(result[:4], payload[:4])

    def test_rejects_visible_and_alpha_differences(self):
        base = bytearray([1, 2, 3, 0] * 25)
        base[2 * 4 + 3] = 255
        for offset in (2 * 4, 3 * 4 + 3):
            source = bytearray(base); source[offset] ^= 1
            with self.assertRaises(ValueError): module.scrub(bytes(base), bytes(source), 5, 5)
        with self.assertRaises(ValueError): module.scrub(b'bad', b'bad', 5, 5)


if __name__ == '__main__': unittest.main()
