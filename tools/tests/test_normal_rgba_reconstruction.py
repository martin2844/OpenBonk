import importlib.util
import hashlib
from pathlib import Path
import unittest

path = Path(__file__).resolve().parents[1] / 'normal-rgba-reconstruction.py'
spec = importlib.util.spec_from_file_location('normal_rgba', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class NormalRgbaTests(unittest.TestCase):
    def test_channels_and_clamped_normal(self):
        # X and Y come from alpha/green; out-of-unit vectors clamp Z to zero.
        self.assertEqual(module.reconstruct(bytes((10, 128, 20, 128))), bytes((128, 128, 254, 255)))
        self.assertEqual(module.reconstruct(bytes((10, 255, 20, 255))), bytes((255, 255, 127, 255)))
        self.assertEqual(module.reconstruct(b''), b'')

    def test_rejects_incomplete_or_non_bytes(self):
        with self.assertRaises(ValueError): module.reconstruct(b'abc')
        with self.assertRaises(ValueError): module.reconstruct(bytearray(4))

    def test_all_channel_pairs_match_independent_java_fixture(self):
        data = bytes(v for a in range(256) for g in range(256) for v in (0, g, 0, a))
        self.assertEqual(hashlib.sha256(module.reconstruct(data)).hexdigest(),
                         'b6f97fc538b1bfdf1a8eed0943b5050638f6636288060458b98309728fd40732')


if __name__ == '__main__': unittest.main()
