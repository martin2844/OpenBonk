import importlib.util
from pathlib import Path
import struct
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'experimental-steam-audio.py'
SPEC = importlib.util.spec_from_file_location('experimental_steam_audio', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class SteamAudioTest(unittest.TestCase):
    def test_patch_size_changes_only_expected_field(self):
        body = b'header' + struct.pack('<Q', 32) + b'tail'
        raw = b'prefix' + body + b'suffix'
        updated = MODULE.patch_size(raw, 6, body, 32, 256)
        self.assertEqual(updated, b'prefixheader' + struct.pack('<Q', 256) + b'tailsuffix')
        with self.assertRaises(ValueError):
            MODULE.patch_size(raw, 6, body, 31, 256)
        with self.assertRaises(ValueError):
            MODULE.patch_size(raw, 7, body, 32, 256)

    def test_fsb_format_rejects_invalid_header(self):
        header = b'FSB5' + struct.pack('<6I', 1, 1, 36, 0, 8, 15) + bytes(32)
        self.assertEqual(MODULE.fsb_format(header), 15)
        for data in (b'FSB5', b'bad!' + header[4:], b'FSB5' + struct.pack('<6I', 1, 2, 36, 0, 8, 15) + bytes(32)):
            with self.assertRaises(ValueError):
                MODULE.fsb_format(data)


if __name__ == '__main__':
    unittest.main()
