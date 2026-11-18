import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

path = Path(__file__).resolve().parents[1] / 'package_mod.py'
spec = importlib.util.spec_from_file_location('package_mod', path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackageModTest(unittest.TestCase):
    def test_reproducible_and_only_launcher_files(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root / 'source'; source.mkdir()
            (source / 'manifest.json').write_text('{"id":"sample-mod","api":1}')
            (source / 'main.lua').write_text('return 1\n')
            (source / 'README.md').write_text('not included')
            a, b = root / 'a.zip', root / 'b.zip'
            module.package(source, a); module.package(source, b)
            self.assertEqual(a.read_bytes(), b.read_bytes())
            with zipfile.ZipFile(a) as archive:
                self.assertEqual(archive.namelist(), ['manifest.json', 'main.lua'])
                self.assertEqual(archive.read('main.lua'), b'return 1\n')

    def test_invalid_manifest_and_empty_lua_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root / 'manifest.json').write_text('{"id":"../bad"}')
            (root / 'main.lua').write_text('return 1')
            with self.assertRaises(ValueError): module.package(root, root / 'mod.zip')
            (root / 'manifest.json').write_text('{"id":"good"}')
            (root / 'main.lua').write_text(' ')
            with self.assertRaises(ValueError): module.package(root, root / 'mod.zip')


if __name__ == '__main__': unittest.main()
