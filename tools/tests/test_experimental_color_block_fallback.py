import importlib.util
from pathlib import Path
import unittest

from PIL import Image

path = Path(__file__).resolve().parents[1] / 'experimental-color-block-fallback.py'
spec = importlib.util.spec_from_file_location('color_blocks', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class ColorBlockTests(unittest.TestCase):
    def test_visible_color_and_alpha_limits(self):
        original = Image.new('RGBA', (4, 4), (120, 80, 40, 255))
        self.assertTrue(module.acceptable_color(original, original.copy(), 0))
        self.assertFalse(module.acceptable_color(original, Image.new('RGBA', (4, 4), (160, 80, 40, 255)), 0))
        self.assertFalse(module.acceptable_color(original, Image.new('RGBA', (4, 4), (120, 80, 40, 200)), 0))

    def test_equal_and_changed_blocks(self):
        image = Image.new('RGBA', (8, 4), (120, 80, 40, 255))
        candidate = image.copy(); candidate.paste((255, 0, 0, 255), (4, 0, 8, 4))
        result, safe, unsafe, _ = module.normal.blend_mip(
            b'A' * 32, b'F' * 32, b'Q' * 32, image, candidate, candidate,
            lambda a, b: module.acceptable_color(a, b, 0), 16)
        self.assertEqual(result, b'Q' * 16 + b'A' * 16)
        self.assertEqual((safe, unsafe), (1, 1))


if __name__ == '__main__': unittest.main()
