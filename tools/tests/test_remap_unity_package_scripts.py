import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('remap_unity_package_scripts', Path(__file__).resolve().parents[1] / 'remap-unity-package-scripts.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RemapTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        self.assets = self.project / 'Assets'
        self.assets.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        dlls = self.project / '.openbonk-original-plugins/Assets/Plugins'
        dlls.mkdir(parents=True)
        self.guid = f'{1:032x}'
        for i, name in enumerate(module.PACKAGE_DLLS, 1):
            (dlls / (name + '.meta')).write_text(f'guid: {i:032x}\n')
        self.target = 'f' * 32
        cache = self.project / 'Library/PackageCache/package/Runtime'
        cache.mkdir(parents=True)
        (cache / 'Component.cs.meta').write_text('guid: ' + self.target + '\n')
        self.fake_script = root / 'remap-unity-package-scripts.py'
        self.fake_script.touch()
        (root / 'unity-package-script-map.json').write_text(json.dumps({module.PACKAGE_DLLS[0]: {'42': self.target}}))
        self.original = f'm_Script: {{fileID: 42, guid: {self.guid}, type: 3}}\n'
        (self.assets / 'a.unity').write_text(self.original)
        (self.assets / 'b.prefab').write_text(self.original)
        self.backup = root / 'backup'
        self.argv = ['program', '--project', str(self.project), '--backup', str(self.backup)]

    def invoke(self, extra=()):
        with patch.object(module, '__file__', str(self.fake_script)), patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_remap(self):
        self.invoke(['--check'])
        self.assertFalse(self.backup.exists())
        self.assertEqual((self.assets / 'a.unity').read_text(), self.original)
        self.invoke()
        for name in ('a.unity', 'b.prefab'):
            self.assertEqual((self.backup / 'Assets' / name).read_text(), self.original)
            self.assertIn(f'fileID: 11500000, guid: {self.target}', (self.assets / name).read_text())

    def test_unmapped_reference_rejected_before_write(self):
        (self.assets / 'b.prefab').write_text(self.original.replace('42', '43'))
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertEqual((self.assets / 'a.unity').read_text(), self.original)
        self.assertFalse(self.backup.exists())

    def test_rolls_back_if_write_fails(self):
        original_write = Path.write_bytes
        def failing_write(path, data):
            if path == self.assets / 'b.prefab':
                raise OSError('simulated failure')
            return original_write(path, data)
        with patch.object(Path, 'write_bytes', failing_write), self.assertRaises(OSError):
            self.invoke()
        self.assertEqual((self.assets / 'a.unity').read_text(), self.original)
        self.assertEqual((self.assets / 'b.prefab').read_text(), self.original)
        self.assertTrue(self.backup.is_dir())


if __name__ == '__main__':
    unittest.main()
