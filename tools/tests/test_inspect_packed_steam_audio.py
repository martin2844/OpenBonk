import importlib.util
import io
from pathlib import Path
import struct
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'inspect-packed-steam-audio.py'
spec = importlib.util.spec_from_file_location('packed_audio', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackedAudioTests(unittest.TestCase):
    def test_fsb_header_and_bounds(self):
        clip = bytearray(72)
        clip[:4] = b'FSB5'
        struct.pack_into('<6I', clip, 4, 1, 1, 4, 0, 8, 7)
        self.assertEqual(module.fsb_codec(clip), 7)
        struct.pack_into('<I', clip, 20, 9)
        with self.assertRaises(ValueError):
            module.fsb_codec(clip)

    def test_splits_are_numeric_and_contiguous(self):
        data = io.BytesIO()
        with zipfile.ZipFile(data, 'w') as z:
            for number in (2, 0, 1):
                z.writestr(module.PREFIX + 'sharedassets1.assets.split' + str(number), str(number))
        with zipfile.ZipFile(data) as z:
            self.assertEqual(module.split_data(z, 'sharedassets1'), b'012')
        data = io.BytesIO()
        with zipfile.ZipFile(data, 'w') as z:
            z.writestr(module.PREFIX + 'sharedassets1.assets.split1', b'x')
        with zipfile.ZipFile(data) as z, self.assertRaises(ValueError):
            module.split_data(z, 'sharedassets1')


if __name__ == '__main__':
    unittest.main()
