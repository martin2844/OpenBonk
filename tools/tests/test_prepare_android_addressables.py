import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('prepare_android_addressables', Path(__file__).resolve().parents[1] / 'prepare-android-addressables.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
FILES = ('catalog.json', 'catalog.json.meta', 'settings.json', 'settings.json.meta', 'AddressablesLink/link.xml', 'AddressablesLink/link.xml.meta')


class AddressablesPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        self.base = self.project / 'Assets/StreamingAssets/aa'
        self.base.mkdir(parents=True)
        self.backup = root / 'backup'
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        for name in FILES:
            path = self.base / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('local ' + name)
        generated = self.project / 'Library/com.unity.addressables/aa/Android'
        generated.mkdir(parents=True)
        for name in ('catalog.json', 'settings.json'):
            (generated / name).write_text('generated ' + name)
        (generated / 'AddressablesLink').mkdir()
        (generated / 'AddressablesLink/link.xml').write_text('generated link')
        self.argv = ['program', '--project', str(self.project), '--backup', str(self.backup)]

    def invoke(self, extra=()):
        with patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_backup(self):
        self.invoke(['--check'])
        self.assertFalse(self.backup.exists())
        self.invoke()
        for name in FILES:
            self.assertFalse((self.base / name).exists())
            self.assertEqual((self.backup / name).read_text(), 'local ' + name)
        self.assertEqual(len(json.loads((self.backup / 'manifest.json').read_text())['files']), 6)

    def test_rejects_without_android_catalog(self):
        (self.project / 'Library/com.unity.addressables/aa/Android/catalog.json').unlink()
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertTrue(all((self.base / name).exists() for name in FILES))
        self.assertFalse(self.backup.exists())

    def test_rollback_after_removal_failure(self):
        original = Path.unlink
        def fail_second(path, *args, **kwargs):
            if path == self.base / 'catalog.json.meta':
                raise OSError('simulated failure')
            return original(path, *args, **kwargs)
        with patch.object(Path, 'unlink', fail_second), self.assertRaises(OSError):
            self.invoke()
        self.assertTrue(all((self.base / name).read_text() == 'local ' + name for name in FILES))
        self.assertTrue(self.backup.is_dir())


if __name__ == '__main__':
    unittest.main()
