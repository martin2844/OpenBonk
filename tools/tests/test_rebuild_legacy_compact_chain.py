import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'rebuild-legacy-compact-chain.py'


class CompactChainTests(unittest.TestCase):
    def test_rebuild_mode_rejects_unpinned_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam), '--rebuild-converted-assets']
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'crunch-decoder'):
                path = root/name; path.write_bytes(b'invalid')
                args.extend(('--'+name, str(path)))
            output = root/'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs: old-apk', result.stderr)
            self.assertFalse(output.exists())

    def test_pinned_inputs_are_checked_before_writing(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam = root / 'steam'
            steam.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam)]
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'converted-obb', 'crunch-decoder'):
                path = root / name
                path.write_bytes(b'not the historical input')
                args.extend(('--' + name, str(path)))
            output = root / 'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs: old-apk', result.stderr)
            self.assertFalse(output.exists())
            self.assertEqual(sorted(p.name for p in root.iterdir()), sorted(['steam', 'old-apk', 'old-obb', 'new-apk', 'new-obb', 'converted-obb', 'crunch-decoder']))


if __name__ == '__main__':
    unittest.main()
