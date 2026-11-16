import importlib.util
import io
from pathlib import Path
from types import SimpleNamespace
import struct
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'inspect-packed-steam-audio.py'
spec = importlib.util.spec_from_file_location('packed_audio', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackedAudioTests(unittest.TestCase):
    def test_audio_aliases_are_narrow_and_require_matching_metadata(self):
        def clip(name, length, source='sharedassets2.resource'):
            values = {field: 1 for field in module.FIELDS}
            values.update(m_Name=name, m_Length=length, m_Resource=SimpleNamespace(m_Source=source))
            return SimpleNamespace(**values)
        target = clip('Hit1_0', 1.4)
        original = clip('Hit1', 1.4)
        other_group = clip('Hit1', 1.4, 'sharedassets1.resource')
        wrong_duration = clip('Hit1', 0.4)
        self.assertEqual(module.matching_clips({'Hit1': [original, other_group, wrong_duration]}, target, 'sharedassets2'), [original])
        self.assertEqual(module.matching_clips({'Hit1': [original]}, target, 'sharedassets1'), [])
        self.assertEqual(module.matching_clips({'Hit1': [wrong_duration]}, target, 'sharedassets2'), [])
        exact = clip('Hit1_0', 1.4)
        self.assertEqual(module.matching_clips({'Hit1': [original], 'Hit1_0': [exact]}, target, 'sharedassets2'), [exact])
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
