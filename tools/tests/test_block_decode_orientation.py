"""Regression tests for ETC2 byte order and full-image block quality bounds."""
import importlib.util
from pathlib import Path
import unittest
from PIL import ImageChops, ImageStat

ROOT = Path(__file__).resolve().parents[1]

def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / filename)
    result = importlib.util.module_from_spec(spec); spec.loader.exec_module(result)
    return result

normal = load('normal_orientation', 'experimental-normal-block-fallback.py')
color = load('color_orientation', 'experimental-color-block-fallback.py')
small = load('small_orientation', 'experimental-small-color-block-fallback.py')


class BlockDecodeOrientationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        from UnityPy.export import Texture2DConverter
        cls.converter = Texture2DConverter

    def decode(self, payload, width, height, fmt, mode='RGBA'):
        return normal.partial.decode_block_image(self.converter, payload, width, height, fmt, None, 13, mode)

    def test_non_multiple_of_four_height_matches_serialized_rows(self):
        blocks = [bytes.fromhex(value) for value in (
            '6060600200000000', 'c810100200000000', '40a0400200000000', '4040a00200000000')]
        whole = self.decode(b''.join(blocks), 8, 6, 45, 'RGB')
        for index, block in enumerate(blocks):
            tile = self.decode(block, 4, 4, 45, 'RGB')
            x, y = (index % 2) * 4, (index // 2) * 4
            height = min(4, 6 - y)
            self.assertEqual(whole.crop((x, y, x + 4, y + height)).tobytes(), tile.crop((0, 0, 4, height)).tobytes())

    def test_replaced_blocks_satisfy_full_image_color_and_alpha_bounds(self):
        # Synthetic ETC2 blocks: safe encoder variants in the upper row, and
        # a large color/alpha difference in the lower row. These are not game assets.
        for fmt, original_block, safe_block, unsafe_block in (
            (45, '6060600200000000', 'b2641cb364cb2c99', 'c810100200000000'),
            (47, 'ff000000000000006060600200000000', 'ff00000000000000b2641cb364cb2c99',
             '0000000000000000c810100200000000')):
            old = bytes.fromhex(original_block) * 4
            safe, unsafe = bytes.fromhex(safe_block), bytes.fromhex(unsafe_block)
            candidate = safe * 2 + unsafe * 2
            block_size = len(safe)
            reference, alternative = self.decode(old, 8, 8, fmt), self.decode(candidate, 8, 8, fmt)
            # Each of the three consumers uses the same unflipped decode helper.
            rgb_result, _, _, _ = normal.blend_mip(old, candidate, candidate, reference, alternative, alternative,
                                                    lambda a, b: color.acceptable_color(a, b, 0), block_size)
            color_result, _, _ = small.replace_mip(old, [candidate], [reference, alternative], 0, block_size)
            rejected = unsafe * 4
            extra_result, _, _ = color.fill_safe_extra_blocks(old, old, rejected, rejected, reference,
                                                               [(candidate, alternative)], lambda a, b: color.acceptable_color(a, b, 0), block_size)
            results = [rgb_result, color_result, extra_result]
            if fmt == 45:
                normal_result, _, _, _ = normal.blend_mip(old, candidate, candidate, reference, alternative, alternative,
                                                           normal.partial.study.acceptable_normal_image, block_size)
                extra_normal, _, _ = normal.fill_safe_extra_blocks(old, old, rejected, rejected, reference,
                                                                    [(candidate, alternative)], normal.partial.study.acceptable_normal_image)
                self.assertTrue(normal.partial.study.acceptable_normal_image(reference, self.decode(normal_result, 8, 8, fmt)))
                self.assertTrue(normal.partial.study.acceptable_normal_image(reference, self.decode(extra_normal, 8, 8, fmt)))
                results += [normal_result, extra_normal]
            for result in results:
                self.assertEqual(result, safe * 2 + bytes.fromhex(original_block) * 2)
                restored = self.decode(result, 8, 8, fmt)
                self.assertTrue(small.partial.study.acceptable_bounded_mip(reference, restored, 0))
                alpha_error = ImageChops.difference(reference.getchannel('A'), restored.getchannel('A'))
                self.assertLessEqual(ImageStat.Stat(alpha_error).mean[0], 1)
                self.assertLessEqual(alpha_error.getextrema()[1], 8)


if __name__ == '__main__': unittest.main()
