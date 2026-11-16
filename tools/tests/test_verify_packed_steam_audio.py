import importlib.util
from pathlib import Path
import struct
import unittest


spec = importlib.util.spec_from_file_location('verify_packed_audio', Path(__file__).resolve().parents[1] / 'verify-packed-steam-audio.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class Video:
    byte_start = 0

    def __init__(self, offset, size=5):
        self.source = 'sharedassets1.resource'
        self.offset = offset
        self.size = size
        name = self.source.encode()
        prefix = struct.pack('<I', len(name)) + name
        prefix += bytes((-len(prefix)) % 4)
        self.body = prefix + struct.pack('<QQ', offset, size) + b'unchanged'

    def get_raw_data(self):
        return self.body

    def read_typetree(self):
        return {'m_ExternalResources': {'m_Source': self.source, 'm_Offset': self.offset, 'm_Size': self.size}}


class PackedAudioVideoTests(unittest.TestCase):
    def test_accepts_relocated_unchanged_video(self):
        old, new = Video(4), Video(8)
        result = module.normalize_video_offsets(old.body, new.body, b'headvideo', b'new data' + b'video',
                                                'sharedassets1', {1: old}, {1: new})
        self.assertEqual(result, old.body)

    def test_rejects_modified_video_payload(self):
        old, new = Video(4), Video(8)
        with self.assertRaisesRegex(ValueError, 'video payload changed'):
            module.normalize_video_offsets(old.body, new.body, b'headvideo', b'new data' + b'other',
                                           'sharedassets1', {1: old}, {1: new})


if __name__ == '__main__':
    unittest.main()
