from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[2]/'analysis/data-conversion/generate-android-shared-audio-plan.py'


class SharedAudioPlanTest(unittest.TestCase):
    def test_rejects_unpinned_rebuilt_assets_before_writing(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); assets = root/'assets'; assets.mkdir()
            (assets/'sharedassets1.assets').write_bytes(b'invalid')
            (assets/'sharedassets1.resource').write_bytes(b'invalid')
            for name in ('linux.zip', 'windows.zip'):
                with zipfile.ZipFile(root/name, 'w'): pass
            output = root/'plan.bin'
            result = subprocess.run([sys.executable, str(SCRIPT), '--rebuilt-assets', str(assets),
                                     '--linux-zip', str(root/'linux.zip'), '--windows-zip', str(root/'windows.zip'),
                                     '--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('rebuilt asset differs', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
