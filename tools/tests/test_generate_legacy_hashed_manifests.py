from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'generate-legacy-hashed-manifests.py'


class HashedManifestTest(unittest.TestCase):
    def test_rejects_unpinned_obb_before_writing_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam, obb, output = root/'steam', root/'base.obb', root/'output'
            steam.mkdir()
            obb.write_bytes(b'not the pinned OBB')
            result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', str(steam),
                                     '--expanded-obb', str(obb), '--output', str(output)],
                                    capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('incompatible expanded shared OBB', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
