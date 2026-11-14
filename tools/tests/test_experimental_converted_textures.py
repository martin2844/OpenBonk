import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

spec = importlib.util.spec_from_file_location('converted_textures', Path(__file__).resolve().parents[1] / 'experimental-converted-textures.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TextureConversionTests(unittest.TestCase):
    def test_only_known_case_collisions_map_to_steam_names(self):
        self.assertEqual(module.collision_source('bush__openbonk_8cd6c6a0'), 'bush')
        self.assertEqual(module.collision_source('retaliation__openbonk_56e12a7d'), 'retaliation')
        for name in ('bush', 'Bush__openbonk_8cd6c6a0', 'bush__openbonk_8cd6c6a0extra', 'skull__openbonk_8cd6c6a0', None):
            self.assertIsNone(module.collision_source(name))

    def test_collision_mip_rejects_large_error_and_different_dimensions(self):
        from PIL import Image
        original = Image.new('RGBA', (2, 2), (100, 100, 100, 255))
        self.assertTrue(module.acceptable_collision_mip(original, original.copy()))
        self.assertFalse(module.acceptable_collision_mip(original, Image.new('RGBA', (2, 2), (120, 100, 100, 255))))
        self.assertFalse(module.acceptable_collision_mip(original, Image.new('RGBA', (1, 2), (100, 100, 100, 255))))
    def test_sdf_gate_limits_threshold_crossings_and_outliers(self):
        from PIL import Image
        original = Image.new('RGBA', (10, 10), (255, 255, 255, 128))
        changed = original.copy(); changed.putpixel((0, 0), (255, 255, 255, 127))
        self.assertTrue(module.acceptable_sdf_image(original, changed))
        for x in (1, 2): changed.putpixel((x, 0), (255, 255, 255, 127))
        self.assertFalse(module.acceptable_sdf_image(original, changed))
        changed = original.copy(); changed.putpixel((0, 0), (255, 255, 255, 139))
        self.assertFalse(module.acceptable_sdf_image(original, changed))

    def test_normal_direction_gate_rejects_large_rotation_and_degenerate_vectors(self):
        from PIL import Image
        original = Image.new('RGB', (2, 1), (128, 128, 255))
        nearby = Image.new('RGB', (2, 1), (130, 128, 255))
        self.assertTrue(module.acceptable_normal_image(original, nearby))
        nearby.putpixel((1, 0), (255, 128, 128))
        self.assertFalse(module.acceptable_normal_image(original, nearby))
        self.assertFalse(module.acceptable_normal_image(original, Image.new('RGB', (2, 1), (128, 128, 128))))

    def test_quality_fallback_only_runs_for_eligible_high_error(self):
        high, low = [11, 0, 0, 0], [1, 0, 0, 0]
        self.assertTrue(module.should_try_quality(high, object(), False, 47, 1024, 1024))
        for args in ((low, object(), False, 47, 1024, 1024), (high, None, False, 47, 1024, 1024),
                     (high, object(), True, 47, 1024, 1024), (high, object(), False, 45, 1024, 1024),
                     (high, object(), False, 47, 4096, 1024)):
            self.assertFalse(module.should_try_quality(*args))

    def test_quality_gate_checks_visible_pixels_and_alpha(self):
        from PIL import Image
        original = Image.new('RGBA', (4, 4), (100, 100, 100, 255))
        self.assertTrue(module.acceptable_quality_image(original, original.copy()))
        changed = original.copy(); changed.putpixel((0, 0), (200, 100, 100, 255))
        self.assertFalse(module.acceptable_quality_image(original, changed))
        changed = original.copy(); changed.putpixel((0, 0), (100, 100, 100, 0))
        self.assertFalse(module.acceptable_quality_image(original, changed))

    def test_texture_error_gate_rejects_large_or_invalid_errors(self):
        self.assertTrue(module.acceptable_texture_error([0.0, 2.0, 10.0, 0.0]))
        for error in ([0, 0, 10.01, 0], [0, 0, float('nan'), 0], [0, 0, -1, 0], [0, 0, 0]):
            self.assertFalse(module.acceptable_texture_error(error))

    def test_shared0_sdf_accepts_only_known_atlas_layouts(self):
        texture = SimpleNamespace(m_Name='NotoSansSC-Bold SDF Atlas 19', m_Width=1024, m_Height=1024,
                                  m_MipCount=1, m_ImageCount=1, m_TextureFormat=47)
        self.assertTrue(module.shared0_sdf_target(texture))
        for name in ('NotoSansSC-Bold SDF Atlas 01', 'NotoSansSC-Bold SDF Atlas 0', 'Unrelated SDF Atlas'):
            texture.m_Name = name
            self.assertFalse(module.shared0_sdf_target(texture))
        texture.m_Name = 'alagard SDF Atlas'
        self.assertTrue(module.shared0_sdf_target(texture))
        texture.m_MipCount = 2
        self.assertFalse(module.shared0_sdf_target(texture))

    def test_shared0_sdf_retries_only_failed_candidate_with_encoder(self):
        from PIL import Image
        original = Image.new('RGBA', (4, 4), (255, 255, 255, 128))
        bad = Image.new('RGBA', (4, 4), (255, 255, 255, 0))
        self.assertTrue(module.should_retry_shared0_sdf(True, object(), original, bad))
        self.assertFalse(module.should_retry_shared0_sdf(False, object(), original, bad))
        self.assertFalse(module.should_retry_shared0_sdf(True, None, original, bad))
        self.assertFalse(module.should_retry_shared0_sdf(True, object(), original, original.copy()))

    def test_duplicate_sources_must_have_identical_payload_and_format(self):
        class Texture:
            m_MipCount, m_ImageCount, m_ColorSpace = 1, 1, 0
            def __init__(self, data, fmt=12): self.data, self.m_TextureFormat = data, fmt
            def get_image_data(self): return self.data
        target = Texture(b'target')
        first, same, different = Texture(b'same'), Texture(b'same'), Texture(b'different')
        self.assertIs(module.select_source(target, [first, same]), first)
        self.assertIsNone(module.select_source(target, [first, different]))
        self.assertIsNone(module.select_source(target, [first, Texture(b'same', 10)]))
        different.m_MipCount = 2
        self.assertIs(module.select_source(target, [first, different]), first)

    def test_forest_source_requires_unique_matching_payload_and_metadata(self):
        class Texture:
            m_Width, m_Height, m_MipCount, m_ImageCount, m_ColorSpace = 128, 128, 8, 1, 1
            def __init__(self, name, data, fmt):
                self.m_Name, self.data, self.m_TextureFormat = name, data, fmt
            def get_image_data(self): return self.data
        target = Texture('Forest1_TopGrass_0', b'target', 45)
        first = Texture('Forest1_TopGrass', b'one', 10)
        other = Texture('Forest1_TopGrass', b'two', 10)
        self.assertIs(module.select_forest_source(target, [first, other], matches=lambda t: t.data == b'two'), other)
        self.assertIsNone(module.select_forest_source(target, [first, other], matches=lambda t: True))
        self.assertIsNone(module.select_forest_source(target, [first], matches=lambda t: False))
        other.m_MipCount = 7
        self.assertIsNone(module.select_forest_source(target, [other], matches=lambda t: True))
        target.m_Name = 'Unrelated'
        self.assertIsNone(module.select_forest_source(target, [first], matches=lambda t: True))

    def test_shadow_fallback_requires_one_strict_visual_match(self):
        from PIL import Image
        class Texture:
            m_Name, m_Width, m_Height, m_MipCount, m_ImageCount, m_ColorSpace = 'DropShadow', 512, 512, 1, 1, 1
            def __init__(self, shade, fmt):
                self.image = Image.new('RGBA', (512, 512), (0, 0, 0, shade)); self.m_TextureFormat = fmt
        target, good, bad = Texture(100, 47), Texture(100, 12), Texture(200, 12)
        self.assertIs(module.select_shadow_source(target, [good, bad]), good)
        self.assertIsNone(module.select_shadow_source(target, [good, Texture(100, 12)]))
        self.assertIsNone(module.select_shadow_source(target, [bad]))
        target.m_Name = 'Other'
        self.assertIsNone(module.select_shadow_source(target, [good]))

    def test_mip_sizes_round_blocks_up(self):
        self.assertEqual(module.mip_size(45, 5, 3), 16)
        self.assertEqual(module.mip_size(34, 5, 3), 16)
        self.assertEqual(module.mip_size(47, 5, 3), 32)
        self.assertEqual(module.mip_size(4, 5, 3), 60)

    def test_normal_image_uses_alpha_and_green_and_reconstructs_z(self):
        from PIL import Image
        source = Image.new('RGBA', (2, 1))
        source.putdata([(17, 128, 23, 128), (99, 0, 44, 0)])
        result = module.normal_image(source)
        self.assertEqual([result.getpixel((x, 0)) for x in range(2)], [(128, 128, 255, 255), (0, 0, 128, 255)])

    def test_rejects_short_and_surplus_mip_data(self):
        self.assertEqual(len(module.mip_slices(bytes(48), 45, 8, 8, 3)), 3)
        for n in (47, 49):
            with self.assertRaises(ValueError): module.mip_slices(bytes(n), 45, 8, 8, 3)

    def test_orders_splits_and_rejects_missing(self):
        class Archive:
            def __init__(self, nums):
                self.items = [SimpleNamespace(filename=module.PREFIX + 'sharedassets1.assets.split' + str(n)) for n in nums]
            def infolist(self): return self.items
        self.assertEqual(len(module.parts(Archive([2, 0, 1]), 'sharedassets1')), 3)
        with self.assertRaises(ValueError): module.parts(Archive([0, 2]), 'sharedassets1')


if __name__ == '__main__': unittest.main()
