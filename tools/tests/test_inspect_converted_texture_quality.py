import importlib.util
from pathlib import Path
import unittest
from PIL import Image

spec = importlib.util.spec_from_file_location('texture_quality', Path(__file__).resolve().parents[1] / 'inspect-converted-texture-quality.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class VisibleQualityTests(unittest.TestCase):
    def test_manifest_includes_standalone_and_rejects_unsafe_groups(self):
        standalone = 'a' * 32
        rows = [{'group': 'sharedassets1', 'target_id': 1}, {'group': standalone, 'target_id': 1}]
        self.assertEqual(set(module.group_manifest(rows)), {'sharedassets1', standalone})
        for invalid in (rows + [rows[0]], [{'group': '../outside', 'target_id': 1}],
                        [{'group': standalone, 'target_id': True}]):
            with self.assertRaises(ValueError): module.group_manifest(invalid)

    def test_transparent_rgb_does_not_inflate_visible_error(self):
        a = Image.new('RGBA', (2, 2), (0, 0, 0, 0))
        b = Image.new('RGBA', (2, 2), (255, 100, 50, 0))
        metrics = module.visible_metrics(a, b)
        self.assertEqual(metrics['alpha_mean_error'], 0)
        for background in metrics['composited']:
            self.assertEqual(background['mean_rgb'], [0, 0, 0])
            self.assertEqual(background['fraction_any_channel_over_32'], 0)

    def test_localized_visible_and_alpha_defects_are_counted(self):
        a = Image.new('RGBA', (2, 2), (0, 0, 0, 255))
        b = a.copy(); b.putpixel((0, 0), (255, 0, 0, 255)); b.putpixel((1, 1), (0, 0, 0, 0))
        metrics = module.visible_metrics(a, b)
        self.assertEqual(metrics['alpha_fraction_over_32'], 0.25)
        self.assertEqual(metrics['composited'][0]['fraction_any_channel_over_32'], 0.25)
        self.assertEqual(metrics['composited'][1]['fraction_any_channel_over_32'], 0.5)


if __name__ == '__main__': unittest.main()
