from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'generate-legacy-shared0-templates.py'


class Shared0TemplatesTest(unittest.TestCase):
    def test_rejects_unpinned_apk_without_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir()
            apk = root/'old.apk'; apk.write_bytes(b'incorrect')
            output = root/'output'
            result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', str(steam),
                                     '--old-apk', str(apk), '--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input or generated template differs', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
