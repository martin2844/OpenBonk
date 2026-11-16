from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'generate-legacy-shared-plans.py'


class SharedPlansTest(unittest.TestCase):
    def test_rejects_output_inside_steam_input(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam, mesh, obb, terrain = root/'steam', root/'mesh', root/'source.obb', root/'terrain.assets'
            steam.mkdir()
            mesh.mkdir()
            obb.touch()
            terrain.touch()
            output = steam/'output'
            result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', str(steam), '--android-obb', str(obb),
                                     '--shared1-terrain-asset', str(terrain), '--shared1-mesh-input', str(mesh),
                                     '--output', str(output)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 2)
            self.assertIn('output must be separate', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
