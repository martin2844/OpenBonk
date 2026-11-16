from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'rebuild-legacy-localization-base.py'


class LocalizationBaseTest(unittest.TestCase):
    def test_existing_output_is_preserved(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ('base', 'linux', 'windows', 'output'): (root/name).mkdir()
            marker = root/'output'/'keep'; marker.write_text('keep')
            result = subprocess.run([sys.executable, str(SCRIPT), '--base', str(root/'base'),
                                     '--linux-data', str(root/'linux'), '--windows-data', str(root/'windows'),
                                     '--output', str(root/'output')], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(marker.read_text(), 'keep')


if __name__ == '__main__': unittest.main()
