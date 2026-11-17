import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]


class PackageExampleModsTest(unittest.TestCase):
    def test_reproducible_packages_and_attribution(self):
        with tempfile.TemporaryDirectory() as temp:
            first, second = Path(temp) / 'first', Path(temp) / 'second'
            for output in (first, second):
                subprocess.run([sys.executable, str(ROOT / 'tools/package-example-mods.py'),
                                '--output', str(output)], check=True, capture_output=True)
            for mod in ('instant-collect', 'unlock-everything'):
                a, b = first / (mod + '.zip'), second / (mod + '.zip')
                self.assertEqual(hashlib.sha256(a.read_bytes()).digest(), hashlib.sha256(b.read_bytes()).digest())
                with zipfile.ZipFile(a) as package:
                    self.assertEqual(package.namelist(), ['manifest.json', 'main.lua'])
                    self.assertEqual(json.loads(package.read('manifest.json'))['id'], mod)
                    script = package.read('main.lua').decode('utf-8')
                    self.assertIn('return 1', script)
                    self.assertIn('lukeod' if mod == 'unlock-everything' else 'China1', script)
                    if mod == 'unlock-everything':
                        self.assertIn('Permission is hereby granted', script)
                        self.assertIn('THE SOFTWARE IS PROVIDED "AS IS"', script)


if __name__ == '__main__':
    unittest.main()
