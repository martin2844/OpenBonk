import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from PIL import Image

path = Path(__file__).resolve().parents[1] / 'experimental-small-color-block-fallback.py'
spec = importlib.util.spec_from_file_location('small_color_blocks', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class SmallColorBlockTests(unittest.TestCase):
    def test_only_bounded_unrecoverable_blocks_are_changed(self):
        original = Image.new('RGBA', (12, 4), (100, 100, 100, 255))
        close = original.copy(); close.paste((101, 100, 100, 255), (4, 0, 8, 4))
        close.paste((200, 100, 100, 255), (8, 0, 12, 4))
        result, selected, changed = module.replace_mip(b'A'*24, [b'A'*8+b'B'*16], [original, close], 0, 8)
        self.assertEqual(result, b'A'*8+b'B'*8+b'A'*8)
        self.assertEqual((selected, changed), (1, 8))
        transparent = close.copy(); transparent.paste((100, 100, 100, 0), (4, 0, 8, 4))
        self.assertEqual(module.replace_mip(b'A'*8, [b'B'*8], [original.crop((0, 0, 4, 4)), transparent.crop((4, 0, 8, 4))], 0, 8), (b'A'*8, 0, 0))
        with self.assertRaises(ValueError):
            module.replace_mip(b'A', [b'B'*8], [original, close], 0, 8)

    def test_non_color_and_streamed_textures_are_excluded(self):
        target = SimpleNamespace(m_Name='portrait', m_TextureFormat=47, m_ImageCount=1, m_StreamData=None)
        self.assertTrue(module.eligible_target(target, b'A'*64))
        target.m_Name='WaterNormal'
        self.assertFalse(module.eligible_target(target, b'A'*64))
        target.m_Name='portrait'; target.m_StreamData=SimpleNamespace(path='data.resS',size=64)
        self.assertFalse(module.eligible_target(target, b'A'*64))
        target.m_StreamData=None
        self.assertFalse(module.eligible_target(target, b'A'*32))


if __name__ == '__main__': unittest.main()
