import importlib.util
from pathlib import Path
import unittest
from PIL import Image

path=Path(__file__).resolve().parents[1]/'experimental-normal-block-fallback.py'
spec=importlib.util.spec_from_file_location('normal_blocks',path)
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)

class NormalBlockTests(unittest.TestCase):
    def test_only_safe_blocks_are_substituted(self):
        original=Image.new('RGB',(8,4),(128,128,255))
        fast=Image.new('RGB',(8,4),(255,128,128))
        quality=fast.copy();quality.paste((130,128,255),(0,0,4,4))
        result,safe,unsafe,changed=module.blend_mip(b'A'*16,b'F'*16,b'Q'*16,original,fast,quality,module.partial.study.acceptable_normal_image)
        self.assertEqual(result,b'Q'*8+b'A'*8)
        self.assertEqual((safe,unsafe,changed),(1,1,8))
        with self.assertRaises(ValueError):module.blend_mip(b'A',b'F'*16,b'Q'*16,original,fast,quality,module.partial.study.acceptable_normal_image)

    def test_sixteen_byte_blocks_are_not_split(self):
        original=Image.new('RGBA',(8,4),(100,100,100,255))
        fast=Image.new('RGBA',(8,4),(255,255,255,255))
        quality=fast.copy();quality.paste((100,100,100,255),(0,0,4,4))
        result,safe,unsafe,changed=module.blend_mip(b'A'*32,b'F'*32,b'Q'*32,original,fast,quality,
                                                   lambda a,b:a.tobytes()==b.tobytes(),16)
        self.assertEqual(result,b'Q'*16+b'A'*16)
        self.assertEqual((safe,unsafe,changed),(1,1,16))
        with self.assertRaises(ValueError):
            module.blend_mip(b'A'*32,b'F'*32,b'Q'*32,original,fast,quality,lambda a,b:True,12)

if __name__=='__main__':unittest.main()
