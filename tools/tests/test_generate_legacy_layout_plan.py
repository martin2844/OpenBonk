from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[2]/'analysis/data-conversion/generate-android-shared1-layout-plan.py'


class LayoutPlanTest(unittest.TestCase):
    def test_unpinned_target_asset_is_rejected_without_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); base = root/'base.obb'; target = root/'target.assets'; out = root/'plan.bin'
            with zipfile.ZipFile(base, 'w') as z:
                z.writestr('assets/bin/Data/sharedassets1.assets.split0', b'base')
            target.write_bytes(b'unexpected')
            result = subprocess.run([sys.executable, str(SCRIPT), '--base-obb', str(base), '--target-asset', str(target), '--output', str(out)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('reconstructed target asset differs', result.stderr)
            self.assertFalse(out.exists())


if __name__ == '__main__': unittest.main()
