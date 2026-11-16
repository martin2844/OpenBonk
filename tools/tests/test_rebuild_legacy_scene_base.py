from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'rebuild-legacy-scene-base.py'


class SceneBaseTest(unittest.TestCase):
    def test_rejects_output_inside_steam_directory(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam, manifests, apk, obb = root/'steam', root/'manifests', root/'base.apk', root/'base.obb'
            steam.mkdir()
            manifests.mkdir()
            apk.touch()
            obb.touch()
            output = steam/'output'
            result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', str(steam), '--apk', str(apk),
                                     '--obb', str(obb), '--manifest-root', str(manifests), '--output', str(output)],
                                    capture_output=True, text=True)
            self.assertEqual(result.returncode, 2)
            self.assertIn('output must be separate', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
