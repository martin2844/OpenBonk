import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('probe_preparation', Path(__file__).resolve().parents[1] / 'prepare-unity-remove-water-probes.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)

class WaterProbePreparationTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'workspace'; self.assets = self.project / 'Assets'
        self.resource = self.assets / 'Resources'; self.resource.mkdir(parents=True)
        self.source = self.root / 'checkout/unity/OpenBonk/Assets/Resources'; self.source.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        for i, name in enumerate(module.NAMES):
            data = ('shader ' + name).encode(); meta = ('guid: ' + str(i + 1) * 32 + '\n').encode()
            for path in (self.resource, self.source):
                (path / name).write_bytes(data); (path / (name + '.meta')).write_bytes(meta)
        self.backup = self.root / 'backup'
        self.root_patch = patch.object(module, 'ROOT', self.root / 'checkout'); self.root_patch.start(); self.addCleanup(self.root_patch.stop)

    def run_tool(self, *args):
        with patch.object(sys, 'argv', ['tool', '--project', str(self.project), *args]), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()): module.main()

    def test_moves_only_expected_files_and_is_idempotent(self):
        keep = self.resource / 'game.shader'; keep.write_text('keep')
        with self.assertRaises(SystemExit): self.run_tool('--check')
        self.run_tool('--backup', str(self.backup)); self.run_tool('--check'); self.run_tool()
        self.assertEqual(keep.read_text(), 'keep')
        for name in module.NAMES:
            self.assertFalse((self.resource / name).exists())
            self.assertEqual((self.backup / name).read_bytes(), (self.source / name).read_bytes())

    def test_rejects_serialized_and_unexpected_code_references(self):
        ref = self.assets / 'test.mat'; ref.write_text('guid: ' + '1' * 32)
        with self.assertRaises(ValueError): self.run_tool('--backup', str(self.backup))
        self.assertFalse(self.backup.exists()); ref.unlink()
        ref = self.assets / 'unexpected.cs'; ref.write_text(module.NAMES[0][:-7])
        with self.assertRaises(ValueError): self.run_tool('--backup', str(self.backup))
        self.assertTrue((self.resource / module.NAMES[0]).exists())

    def test_rejects_modified_or_incomplete_shader_set(self):
        target = self.resource / module.NAMES[0]; target.write_text('changed')
        with self.assertRaises(ValueError): self.run_tool('--backup', str(self.backup))
        target.unlink()
        with self.assertRaises(ValueError): self.run_tool('--backup', str(self.backup))
        self.assertFalse(self.backup.exists())

if __name__ == '__main__': unittest.main()
