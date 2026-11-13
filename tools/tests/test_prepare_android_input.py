import contextlib
import importlib.util
import io
import json
from pathlib import Path
import re
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('prepare_android_input', Path(__file__).resolve().parents[1] / 'prepare-android-input.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class AndroidInputPreparationTests(unittest.TestCase):
    def test_native_axis_calls_have_checked_in_definitions(self):
        root = Path(__file__).resolve().parents[2] / 'unity/OpenBonk/Assets/Scripts/Assembly-CSharp'
        used = set()
        for name in ('AndroidNativeInput.cs', 'InputSetting.cs'):
            used.update(re.findall(r'Input\.GetAxis(?:Raw)?\("([^\"]+)"\)', (root / name).read_text()))
        self.assertTrue(used)
        self.assertEqual(used - set(module.NAMES), set())

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'workspace'
        (self.project / 'ProjectSettings').mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.path = self.project / 'ProjectSettings/InputManager.asset'
        rows = module.definitions(); extra = dict(rows[0], m_Name='UnrelatedAxis')
        body = '  - serializedVersion: 3\n' + ''.join('    '+k+':' + (' '+extra[k] if extra[k] else '')+'\n' for k in module.FIELDS)
        self.original = '%YAML 1.1\nInputManager:\n  m_Axes:\n' + body + '  m_UsePhysicalKeys: 0\n'
        self.path.write_text(self.original); self.backup = self.root / 'input-backup.asset'

    def invoke(self, *extra):
        argv = ['prepare-android-input.py', '--project', str(self.project), *extra]
        with patch.object(sys, 'argv', argv), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()): module.main()

    def test_prepares_idempotently_and_preserves_other_axes(self):
        with self.assertRaises(SystemExit): self.invoke('--check')
        self.invoke('--backup', str(self.backup))
        self.assertEqual(self.backup.read_text(), self.original)
        result = self.path.read_text(); self.assertIn('m_Name: UnrelatedAxis', result)
        module.verify(self.project)
        self.assertEqual(module.render(result, module.definitions()), result)
        self.invoke('--check')
        self.invoke()
        self.assertEqual(self.path.read_text(), result)

    def test_rejects_bad_layout_without_modification(self):
        self.path.write_text('unexpected')
        with self.assertRaises(ValueError): self.invoke('--backup', str(self.backup))
        self.assertFalse(self.backup.exists()); self.assertEqual(self.path.read_text(), 'unexpected')

    def test_rejects_existing_backup(self):
        self.backup.write_text('keep')
        with self.assertRaises(SystemExit): self.invoke('--backup', str(self.backup))
        self.assertEqual(self.backup.read_text(), 'keep'); self.assertEqual(self.path.read_text(), self.original)


if __name__ == '__main__': unittest.main()
