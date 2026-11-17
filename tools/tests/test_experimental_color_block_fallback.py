import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

from PIL import Image

path = Path(__file__).resolve().parents[1] / 'experimental-color-block-fallback.py'
spec = importlib.util.spec_from_file_location('color_blocks', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class ColorBlockTests(unittest.TestCase):
    def test_target_selection_is_stable_across_path_ids_and_rejects_ambiguity(self):
        target = SimpleNamespace(m_Name='t_birdo_blue', m_Width=128, m_Height=128,
                                 m_MipCount=8, m_ImageCount=1, m_TextureFormat=45)
        key = ('sharedassets1.assets', 't_birdo_blue')
        self.assertEqual(module.identify_target(key[0], target, set()), key)
        self.assertIsNone(module.identify_target('sharedassets2.assets', target, set()))
        with self.assertRaisesRegex(ValueError, 'duplicate'):
            module.identify_target(key[0], target, {key})
        target.m_Width = 64
        with self.assertRaisesRegex(ValueError, 'metadata'):
            module.identify_target(key[0], target, set())

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
