import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('prepare_unity_characters', Path(__file__).resolve().parents[1] / 'prepare-unity-characters.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CharacterPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        for folder in ('ProjectSettings', 'Assets/Editor', 'Assets/Resources', 'Library/PackageCache'):
            (self.project / folder).mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
        (self.project / 'Assets/Editor/OpenBonkPrepareCharacterResources.cs').write_text('test')
        self.editor = root / 'Unity'
        self.editor.write_text('test')
        self.editor.chmod(0o755)
        self.log = root / 'unity.log'
        self.argv = ['program', '--project', str(self.project), '--unity', str(self.editor), '--log', str(self.log)]

    def invoke(self, extra=()):
        with patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_success(self):
        self.invoke(['--check'])
        self.assertFalse(self.log.exists())
        def run(cmd, env):
            self.assertEqual((self.project / 'Assets/csc.rsp').read_bytes(), module.DEFINES)
            (self.project / 'Assets/csc.rsp.meta').write_text('generated')
            self.log.write_text('Created 21 local character Resources and the fallback prefab.')
            return type('Result', (), {'returncode': 0})()
        with patch.object(module.subprocess, 'run', side_effect=run):
            self.invoke()
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())
        self.assertFalse((self.project / 'Assets/csc.rsp.meta').exists())

    def test_existing_output_is_never_overwritten(self):
        (self.project / 'Assets/Resources/AndroidCharacters').mkdir()
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse(self.log.exists())

    def test_failure_cleans_response_file(self):
        with patch.object(module.subprocess, 'run', return_value=type('Result', (), {'returncode': 1})()):
            with self.assertRaises(SystemExit):
                self.invoke()
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())


if __name__ == '__main__':
    unittest.main()
