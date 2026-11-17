import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch

from PIL import Image

path = Path(__file__).resolve().parents[1] / 'experimental-color-block-fallback.py'
spec = importlib.util.spec_from_file_location('color_blocks', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class ColorBlockTests(unittest.TestCase):
    def test_bullseye_discovery_requires_unique_hashed_target(self):
        target = SimpleNamespace(m_Name='Bullseye_0', m_Width=128, m_Height=128,
                                 m_MipCount=1, m_ImageCount=1, m_TextureFormat=47)
        obj = SimpleNamespace(type=SimpleNamespace(name='Texture2D'), read=lambda: target)
        groups = {'a' * 32: ['first'], 'b' * 32: ['second'], 'sharedassets1.assets': ['ignored']}
        archive = SimpleNamespace(read=lambda part: part.encode())
        def load(data): return SimpleNamespace(objects=[obj] if data == b'first' else [])
        with patch('UnityPy.load', side_effect=load):
            self.assertEqual(module.discover_bullseye(archive, groups), ('a' * 32, 'Bullseye_0'))
        with patch('UnityPy.load', return_value=SimpleNamespace(objects=[obj])):
            with self.assertRaisesRegex(ValueError, 'ambiguous'): module.discover_bullseye(archive, groups)
        with patch('UnityPy.load', side_effect=ValueError('unreadable group')):
            with self.assertRaisesRegex(ValueError, 'unreadable group'): module.discover_bullseye(archive, groups)
        target.m_Width = 64
        with patch('UnityPy.load', side_effect=load):
            with self.assertRaisesRegex(ValueError, 'metadata'): module.discover_bullseye(archive, groups)

    def test_emission_alias_requires_unique_visually_bounded_source(self):
        image = Image.new('RGBA', (4, 4), (120, 80, 40, 255))
        target = SimpleNamespace(m_Name='Emission_1', m_MipCount=1, m_ImageCount=1, m_ColorSpace=0, image=image)
        good = SimpleNamespace(m_TextureFormat=4, m_MipCount=1, m_ImageCount=1, m_ColorSpace=0, image=image.copy())
        bad = SimpleNamespace(m_TextureFormat=4, m_MipCount=1, m_ImageCount=1, m_ColorSpace=0, image=Image.new('RGBA', (4, 4), (0, 0, 0, 255)))
        first, second = ('sharedassets1', 1, good), ('sharedassets1', 2, bad)
        with patch.object(module.partial, 'candidate_sources', return_value=[first, second]):
            self.assertEqual(module.select_source(target, {}, 'sharedassets1.assets', object()), first)
            bad.image = image.copy()
            self.assertIsNone(module.select_source(target, {}, 'sharedassets1.assets', object()))
            good.m_MipCount = 2; bad.m_MipCount = 2
            self.assertIsNone(module.select_source(target, {}, 'sharedassets1.assets', object()))

    def test_alias_requires_encoded_byte_match(self):
        target = SimpleNamespace(m_Name='t_ghost_red_0', image_data=b'pixels')
        with patch.object(module.partial, 'candidate_sources', return_value=['candidate']) as candidates, \
             patch.object(module.partial.study, 'select_source') as metadata_only, \
             patch.object(module.partial, 'partial_etc2_source', return_value=None) as match:
            self.assertIsNone(module.select_source(target, {}, 'sharedassets1.assets', object()))
        self.assertEqual(candidates.call_args.args[-1], 't_ghost_red')
        metadata_only.assert_not_called()
        self.assertEqual(match.call_args.args[1], b'pixels')

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
