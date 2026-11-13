import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('import_unity_workspace', Path(__file__).resolve().parents[1] / 'import-unity-workspace.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ImportWorkspaceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        for folder in ('ProjectSettings', 'Assets', '.openbonk-original-plugins'):
            (self.project / folder).mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        (self.project / '.openbonk-original-plugins/manifest.json').write_text('{}')
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
        self.editor = root / 'Unity'
        self.editor.write_text('test')
        self.editor.chmod(0o755)
        self.log = root / 'unity.log'
        self.argv = ['program', '--project', str(self.project), '--unity', str(self.editor), '--log', str(self.log)]

    def invoke(self, extra=()):
        with patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_success_and_cleanup(self):
        self.invoke(['--check'])
        self.assertFalse(self.log.exists())
        def run(cmd, env):
            self.assertEqual((self.project / 'Assets/csc.rsp').read_bytes(), module.DEFINES)
            (self.project / 'Assets/csc.rsp.meta').write_text('generated')
            (self.project / 'Library/PackageCache').mkdir(parents=True)
            self.log.write_text('Exiting batchmode successfully now!')
            return type('Result', (), {'returncode': 0})()
        with patch.object(module.subprocess, 'run', side_effect=run):
            self.invoke()
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())
        self.assertFalse((self.project / 'Assets/csc.rsp.meta').exists())

    def test_fails_if_unity_reports_compiler_error(self):
        def run(cmd, env):
            self.log.write_text('Assets/Test.cs(1,2): error CS1234: fail\nExiting batchmode successfully now!')
            return type('Result', (), {'returncode': 0})()
        with patch.object(module.subprocess, 'run', side_effect=run), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_existing_response_file_is_protected(self):
        (self.project / 'Assets/csc.rsp').write_text('user data')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertEqual((self.project / 'Assets/csc.rsp').read_text(), 'user data')


if __name__ == '__main__':
    unittest.main()
