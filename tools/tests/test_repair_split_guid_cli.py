import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPT = Path(__file__).resolve().parents[2] / 'analysis/port-inventory/repair-split-guid-probe.py'


class RepairSplitGuidCliTests(unittest.TestCase):
    def test_existing_output_is_never_replaced(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            output = root / 'existing'
            output.mkdir()
            marker = output / 'keep'
            marker.write_text('keep')
            result = subprocess.run([sys.executable, str(SCRIPT), str(root), str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('output must be a new directory', result.stderr)
            self.assertEqual(marker.read_text(), 'keep')

    def test_missing_explicit_runner_fails_before_writing(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for path in ('first.obb', 'second.obb', 'second.apk', 'sdk/zipalign', 'sdk/apksigner', 'jdk/bin/java', 'debug.keystore'):
                file = root / path
                file.parent.mkdir(parents=True, exist_ok=True)
                file.touch()
            output = root / 'output'
            result = subprocess.run([
                sys.executable, str(SCRIPT), str(root), str(output),
                '--build-tools', str(root / 'sdk'), '--java-home', str(root / 'jdk'),
                '--debug-keystore', str(root / 'debug.keystore'), '--runner', str(root / 'missing-runner'),
            ], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('runner not found', result.stderr)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
