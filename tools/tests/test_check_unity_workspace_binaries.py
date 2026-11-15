import importlib.util
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'check-unity-workspace-binaries.py'
SPEC = importlib.util.spec_from_file_location('workspace_binaries', SCRIPT)
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


class WorkspaceBinaryCheck(unittest.TestCase):
    def test_matches_inputs_and_detects_missing_or_changed_binaries(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            workspace, export, plugins = (root / name for name in ('workspace', 'export', 'plugins'))
            for directory in (workspace, export, plugins):
                directory.mkdir()
            lua = root / 'lua.so'
            lua.write_bytes(b'lua')
            generated = {Path('Plugins') / name: plugins / name for name in module.PLUGINS}
            generated[Path('Plugins/Android/com.rlabrecque.steamworks.net.dll')] = plugins / module.PLUGINS[2]
            generated[Path('Plugins/Android/arm64-v8a/libmegabonk_lua.so')] = lua
            for source in set(generated.values()):
                source.write_bytes(source.name.encode())
            for relative, source in generated.items():
                target = workspace / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(source.read_bytes())
            exported = Path('Plugins/Example.dll')
            (export / exported).parent.mkdir(parents=True)
            (export / exported).write_bytes(b'export')
            (workspace / exported).write_bytes(b'export')
            result = module.compare(workspace, export, plugins, lua)
            self.assertEqual(result['export'], [exported.as_posix()])
            self.assertEqual(len(result['generated']), 5)
            self.assertFalse(result['missing_workspace'] or result['missing_input'] or result['changed'])
            (workspace / exported).write_bytes(b'changed')
            (workspace / 'Plugins/Unknown.dll').write_bytes(b'unknown')
            (workspace / 'Plugins/Rewired_Core.dll').unlink()
            result = module.compare(workspace, export, plugins, lua)
            self.assertEqual(result['changed'], [exported.as_posix()])
            self.assertEqual(result['missing_input'], ['Plugins/Unknown.dll'])
            self.assertEqual(result['missing_workspace'], ['Plugins/Rewired_Core.dll'])


if __name__ == '__main__':
    unittest.main()
