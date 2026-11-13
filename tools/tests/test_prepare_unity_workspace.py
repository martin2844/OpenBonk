import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-workspace.py'
SPEC = importlib.util.spec_from_file_location('prepare_unity_workspace', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class PrepareWorkspaceTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-plugin-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project, self.plugins, self.overlay = (self.root / name for name in ('project', 'plugins', 'overlay'))
        self.base = self.project / 'Assets/Plugins'
        (self.base / 'Android/arm64-v8a').mkdir(parents=True)
        self.plugins.mkdir()
        self.overlay.mkdir()
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(MODULE.MARKER))
        for name in MODULE.PLUGINS:
            (self.base / name).write_bytes(b'original')
            (self.plugins / name).write_bytes(b'generated')
        self.lua = self.root / 'libmegabonk_lua.so'
        self.lua.write_bytes(b'lua')
        for name in ('Android/com.rlabrecque.steamworks.net.dll.meta', 'Android/arm64-v8a/libmegabonk_lua.so.meta'):
            path = self.overlay / 'Assets/Plugins' / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('guid: ' + 'a' * 32 + '\n')
        self.duplicate = self.base / MODULE.PACKAGE_DLLS[0]
        self.duplicate.write_bytes(b'duplicate')
        Path(str(self.duplicate) + '.meta').write_text('old metadata')

    def run_tool(self, *extra):
        args = [str(SCRIPT), '--project', str(self.project), '--plugins', str(self.plugins),
                '--lua-library', str(self.lua), '--remove-duplicate-package-dlls', *extra]
        with patch.object(MODULE, 'OVERLAY', self.overlay), patch.object(sys, 'argv', args), \
             contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            MODULE.main()

    def test_check_and_prepare_with_backups(self):
        self.run_tool('--check')
        self.assertEqual(self.duplicate.read_bytes(), b'duplicate')
        self.assertFalse((self.project / '.openbonk-original-plugins').exists())
        self.run_tool()
        self.assertFalse(self.duplicate.exists())
        self.assertEqual((self.base / MODULE.PLUGINS[0]).read_bytes(), b'generated')
        self.assertEqual((self.base / 'Android/arm64-v8a/libmegabonk_lua.so').read_bytes(), b'lua')
        backup = self.project / '.openbonk-original-plugins'
        self.assertEqual((backup / 'Assets/Plugins' / self.duplicate.name).read_bytes(), b'duplicate')
        manifest = json.loads((backup / 'manifest.json').read_text())
        self.assertIn('Assets/Plugins/Android/arm64-v8a/libmegabonk_lua.so', manifest['created_files'])
        with self.assertRaises(SystemExit):
            self.run_tool()

    def test_backup_failure_does_not_delete_unmodified_files(self):
        with patch.object(MODULE.shutil, 'copy2', side_effect=OSError('fixture failure')):
            with self.assertRaises(OSError):
                self.run_tool()
        self.assertEqual(self.duplicate.read_bytes(), b'duplicate')
        self.assertEqual((self.base / MODULE.PLUGINS[0]).read_bytes(), b'original')
        self.assertFalse((self.project / '.openbonk-original-plugins').exists())

    def test_failure_during_install_restores_changed_files(self):
        original_copyfile = MODULE.shutil.copyfile
        failed = [False]
        def fail_once(source, destination, *args, **kwargs):
            result = original_copyfile(source, destination, *args, **kwargs)
            if Path(destination) == self.base / MODULE.PLUGINS[1] and not failed[0]:
                failed[0] = True
                raise OSError('fixture failure after writing')
            return result
        with patch.object(MODULE.shutil, 'copyfile', side_effect=fail_once):
            with self.assertRaises(OSError):
                self.run_tool()
        self.assertEqual((self.base / MODULE.PLUGINS[0]).read_bytes(), b'original')
        self.assertEqual((self.base / MODULE.PLUGINS[1]).read_bytes(), b'original')
        self.assertEqual(self.duplicate.read_bytes(), b'duplicate')
        self.assertFalse((self.project / '.openbonk-original-plugins').exists())

    def test_missing_input_rejected_without_changes(self):
        self.lua.unlink()
        with self.assertRaises(FileNotFoundError):
            self.run_tool()
        self.assertEqual(self.duplicate.read_bytes(), b'duplicate')


if __name__ == '__main__':
    unittest.main()
