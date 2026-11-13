import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'mods/pikachu/build_test_package.py'
REFERENCE = ROOT / 'mods/pikachu/study/pikachu-content-local-test.zip'


class PikachuPackageTest(unittest.TestCase):
    def test_package_content_is_reproducible_and_existing_output_is_preserved(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-pikachu-test-') as tmp:
            first, second = (Path(tmp) / name for name in ('first.zip', 'second.zip'))
            for output in (first, second):
                subprocess.run([sys.executable, str(SCRIPT), '--output', str(output)], check=True, capture_output=True)
            self.assertEqual(first.read_bytes(), second.read_bytes())
            with zipfile.ZipFile(first) as actual, zipfile.ZipFile(REFERENCE) as reference:
                self.assertEqual(set(actual.namelist()), set(reference.namelist()))
                for name in actual.namelist():
                    self.assertEqual(actual.read(name), reference.read(name))
                manifest = json.loads(actual.read('manifest.json'))
                for item in manifest['files']:
                    payload = actual.read(item['path'])
                    self.assertEqual(len(payload), item['size'])
                    self.assertEqual(hashlib.sha256(payload).hexdigest(), item['sha256'])
            before = first.read_bytes()
            result = subprocess.run([sys.executable, str(SCRIPT), '--output', str(first)], capture_output=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(first.read_bytes(), before)


if __name__ == '__main__':
    unittest.main()
