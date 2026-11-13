import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / 'assemble-unity-workspace.py'
SPEC = importlib.util.spec_from_file_location('assemble_unity_workspace', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class AssembleTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-assemble-test-')
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.base, self.overlay, self.out = root / 'base', root / 'overlay', root / 'out'
        for project in (self.base, self.overlay):
            for name in MODULE.PROJECT_DIRS:
                (project / name).mkdir(parents=True)
            (project / 'ProjectSettings/ProjectVersion.txt').write_text(MODULE.VERSION + '\n')
            (project / 'Assets/Foo.cs.meta').write_text('guid: ' + 'a' * 32 + '\n')
        (self.base / 'Assets/Foo.cs').write_text('base')
        (self.base / 'Assets/local.bin').write_bytes(b'local fixture')
        (self.overlay / 'Assets/Foo.cs').write_text('overlay')

    def run_tool(self, *extra):
        args = [str(SCRIPT), '--base-project', str(self.base), '--output', str(self.out), *extra]
        with patch.object(MODULE, 'OVERLAY', self.overlay), patch.object(sys, 'argv', args), \
             contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            MODULE.main()

    def test_check_and_isolated_copy(self):
        self.run_tool('--check')
        self.assertFalse(self.out.exists())
        self.run_tool()
        self.assertEqual((self.out / 'Assets/Foo.cs').read_text(), 'overlay')
        self.assertEqual((self.out / 'Assets/local.bin').read_bytes(), b'local fixture')
        self.assertEqual((self.base / 'Assets/Foo.cs').read_text(), 'base')
        self.assertTrue((self.out / '.openbonk-workspace.json').is_file())
        with self.assertRaises(SystemExit):
            self.run_tool()

    def test_guid_mismatch_does_not_create_output(self):
        (self.overlay / 'Assets/Foo.cs.meta').write_text('guid: ' + 'b' * 32 + '\n')
        with self.assertRaises(SystemExit):
            self.run_tool()
        self.assertFalse(self.out.exists())

    def test_guid_remap_is_opt_in_and_rewrites_local_references(self):
        (self.overlay / 'Assets/Foo.cs.meta').write_text('guid: ' + 'b' * 32 + '\n')
        material = self.base / 'Assets/Material.mat'
        material.write_text('%YAML 1.1\nref: {fileID: 1, guid: ' + 'a' * 32 + ', type: 3}\n')
        self.run_tool('--allow-guid-remap')
        self.assertIn('b' * 32, (self.out / 'Assets/Material.mat').read_text())
        self.assertIn('b' * 32, (self.out / 'Assets/Foo.cs.meta').read_text())
        self.assertIn('a' * 32, material.read_text())

    def test_guid_remap_rejects_binary_references(self):
        (self.overlay / 'Assets/Foo.cs.meta').write_text('guid: ' + 'b' * 32 + '\n')
        (self.base / 'Assets/binary.bin').write_bytes(b'\0guid: ' + b'a' * 32)
        with self.assertRaises(SystemExit):
            self.run_tool('--allow-guid-remap')
        self.assertFalse(self.out.exists())

    def test_guid_remap_rejects_target_owned_elsewhere(self):
        (self.overlay / 'Assets/Foo.cs.meta').write_text('guid: ' + 'b' * 32 + '\n')
        (self.base / 'Assets/Other.meta').write_text('guid: ' + 'b' * 32 + '\n')
        with self.assertRaises(SystemExit):
            self.run_tool('--allow-guid-remap')
        self.assertFalse(self.out.exists())

    def test_symlink_is_rejected(self):
        (self.base / 'Assets/link').symlink_to(self.base / 'Assets/Foo.cs')
        with self.assertRaises(ValueError):
            self.run_tool()
        self.assertFalse(self.out.exists())


if __name__ == '__main__':
    unittest.main()
