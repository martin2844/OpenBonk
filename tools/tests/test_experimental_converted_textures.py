import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest

spec = importlib.util.spec_from_file_location('converted_textures', Path(__file__).resolve().parents[1] / 'experimental-converted-textures.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TextureConversionTests(unittest.TestCase):
    def test_mip_sizes_round_blocks_up(self):
        self.assertEqual(module.mip_size(45, 5, 3), 16)
        self.assertEqual(module.mip_size(47, 5, 3), 32)
        self.assertEqual(module.mip_size(4, 5, 3), 60)

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
