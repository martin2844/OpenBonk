from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'rebuild-legacy-converted-assets.py'


class ConvertedRebuildTest(unittest.TestCase):
    def test_rejects_unpinned_input_before_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam)]
            for name in ('old-apk', 'old-obb', 'crunch-decoder'):
                file = root/name; file.write_bytes(b'invalid')
                args.extend(('--'+name, str(file)))
            output = root/'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
