from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]/'analysis/data-conversion'


class MeshStageTests(unittest.TestCase):
    def test_invalid_base_is_rejected_without_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); base = root/'base'; base.write_bytes(b'invalid')
            for stage in ('metadata', 'flags', 'stream'):
                with self.subTest(stage=stage):
                    output = root/(stage+'.bin')
                    script = ROOT/('generate-android-shared1-mesh-'+stage+'-plan.py')
                    result = subprocess.run([sys.executable, str(script), '--base', str(base),
                                             '--target-asset', str(root/'target'), '--output', str(output)],
                                            capture_output=True, text=True)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn('invalid base', result.stderr)
                    self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
