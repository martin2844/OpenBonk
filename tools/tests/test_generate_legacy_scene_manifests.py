from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'generate-legacy-scene-manifests.py'


class SceneManifestsTest(unittest.TestCase):
    def test_rejects_unpinned_obb_before_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam = root/'steam'
            steam.mkdir()
            paths = [root/name for name in ('old.obb', 'particle.obb', 'reference.obb')]
            for path in paths:
                path.write_bytes(b'invalid')
            output = root/'output'
            result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', str(steam), '--old-obb', str(paths[0]),
                                     '--particle-obb', str(paths[1]), '--reference-obb', str(paths[2]), '--output', str(output)],
                                    capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
