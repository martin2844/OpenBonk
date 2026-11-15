import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace

spec = importlib.util.spec_from_file_location('gameplay_checks', Path(__file__).resolve().parents[1] / 'check-unity-gameplay.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class GameplayCheckTests(unittest.TestCase):
    def test_rewired_mode_runs_and_cleans_up_its_response_file(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            project = root / 'project'
            (project / 'Assets').mkdir(parents=True)
            (project / 'ProjectSettings').mkdir()
            (project / '.openbonk-workspace.json').write_text('{"schema": 1, "kind": "openbonk-local-unity-workspace"}')
            (project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
            editor = root / 'Unity'
            editor.write_text('')
            editor.chmod(0o700)
            rsp = project / 'Assets/csc.rsp'
            argv = ['check-unity-gameplay.py', '--project', str(project), '--unity', str(editor),
                    '--output', str(root / 'logs'), '--include-rewired']
            def run(command, env, timeout):
                self.assertEqual(rsp.read_bytes(), module.REWIRED_DEFINES)
                self.assertEqual(env['OPENBONK_NO_REWIRED_CODE'], '0')
                self.assertEqual(env['OPENBONK_NO_REWIRED_PLUGINS'], '0')
                Path(command[command.index('-logFile') + 1]).write_text('ExampleCheck PASS\n')
                return SimpleNamespace(returncode=0)
            with patch.object(module, 'CHECKS', (('ExampleCheck', 'PASS', False),)), \
                 patch.object(module.subprocess, 'run', side_effect=run), \
                 patch('sys.argv', argv):
                module.main()
            self.assertFalse(rsp.exists())

    def test_optional_weapon_checks_are_unique_and_have_matching_editor_entrypoints(self):
        names = [name for name, _, _ in module.WEAPON_CHECKS]
        self.assertEqual(len(names), 29)
        self.assertEqual(len(set(names)), len(names))
        for name, marker, play in module.WEAPON_CHECKS:
            source = module.ROOT / 'unity/OpenBonk/Assets/Editor' / (name + '.cs')
            self.assertTrue(source.is_file(), name)
            self.assertIn(name + ' ' + marker, source.read_text())
            self.assertIsInstance(play, bool)

    def test_requires_successful_exit_and_specific_success_marker(self):
        with tempfile.TemporaryDirectory() as temp:
            log = Path(temp) / 'check.log'
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 0, log)
            log.write_text('AnotherCheck PASS\n')
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 0, log)
            log.write_text('ExampleCheck PASS\n')
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 1, log)
            module.check_result('ExampleCheck', 'PASS', 0, log)


if __name__ == '__main__': unittest.main()
