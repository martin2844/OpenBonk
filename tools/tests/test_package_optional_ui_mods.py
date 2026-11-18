import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / 'package-optional-ui-mods.py'
SPEC = importlib.util.spec_from_file_location('package_optional_ui_mods', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class PackageOptionalUiModsTest(unittest.TestCase):
    def test_archives_are_reproducible_and_disabled(self):
        with tempfile.TemporaryDirectory() as temp:
            first = MODULE.package(MODULE.ROOT, Path(temp) / 'first')
            second = MODULE.package(MODULE.ROOT, Path(temp) / 'second')
            self.assertEqual([digest for _, digest in first], [digest for _, digest in second])
            for path, _ in first:
                with zipfile.ZipFile(path) as archive:
                    self.assertEqual(archive.namelist(), ['manifest.json', 'main.lua'])
                    self.assertIs(json.loads(archive.read('manifest.json'))['enabled'], False)

    def test_refuses_enabled_manifest(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for name, hook in MODULE.MODS.items():
                source = root / 'mods' / name
                source.mkdir(parents=True)
                (source / 'manifest.json').write_text(json.dumps({'id': name, 'hook': hook, 'api': 1, 'enabled': True}))
                (source / 'main.lua').write_text('return 1')
            with self.assertRaises(ValueError):
                MODULE.package(root, root / 'out')


if __name__ == '__main__':
    unittest.main()
