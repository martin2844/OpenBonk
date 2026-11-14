import subprocess
import sys
import tempfile
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'mods/pikachu/build_from_source.py'


class PikachuSourceBuildTest(unittest.TestCase):
    def test_existing_output_is_rejected_without_modifying_it(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-pikachu-source-') as temp:
            root = Path(temp); source = root / 'source.fbx'; source.write_bytes(b'input')
            output = root / 'existing'; output.mkdir(); marker = output / 'keep'; marker.write_bytes(b'keep')
            result = subprocess.run([sys.executable, str(SCRIPT), '--source-fbx', str(source),
                                     '--blender', sys.executable, '--unity', sys.executable,
                                     '--output', str(output)], capture_output=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(marker.read_bytes(), b'keep')
            self.assertEqual(source.read_bytes(), b'input')


if __name__ == '__main__':
    unittest.main()
