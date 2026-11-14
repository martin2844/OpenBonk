import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / 'build-local-unity-apk.py'
SPEC = importlib.util.spec_from_file_location('build_local_unity_apk', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class LocalBuildTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-flow-test-')
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.base = root / 'base'
        for name in ('Assets', 'Packages', 'ProjectSettings'):
            (self.base / name).mkdir(parents=True)
        self.editor = root / 'Unity'
        self.editor.write_text('fixture')
        self.editor.chmod(0o700)
        self.lua = root / 'lib.so'
        self.lua.write_bytes(b'fixture')
        self.out = root / 'out'
        self.argv = [str(SCRIPT), '--base-project', str(self.base), '--unity', str(self.editor),
                     '--lua-library', str(self.lua), '--output', str(self.out), '--application-id', 'com.example.flowcheck']

    def run_main(self, *args):
        with patch.object(sys, 'argv', self.argv + list(args)), contextlib.redirect_stdout(io.StringIO()), \
                contextlib.redirect_stderr(io.StringIO()):
            MODULE.main()

    @patch.object(MODULE.subprocess, 'run')
    def test_check_runs_only_initial_preflights(self, run):
        self.run_main('--check')
        self.assertEqual(len(run.call_args_list), 2)
        self.assertTrue(all('--check' in call.args[0] for call in run.call_args_list))
        self.assertFalse(self.out.exists())

    @patch.object(MODULE.subprocess, 'run')
    def test_failure_stops_and_retains_output(self, run):
        run.side_effect = [None, None, None, subprocess.CalledProcessError(1, 'fixture')]
        with self.assertRaises(SystemExit):
            self.run_main()
        self.assertEqual(len(run.call_args_list), 4)
        self.assertTrue(self.out.is_dir())
        self.assertTrue(self.base.is_dir())

    @patch.object(MODULE.subprocess, 'run')
    def test_rejects_existing_output_and_invalid_application_id(self, run):
        self.out.mkdir()
        with self.assertRaises(SystemExit):
            self.run_main()
        self.assertEqual(run.call_count, 0)
        self.out.rmdir()
        self.argv[-1] = 'invalid/id'
        with self.assertRaises(SystemExit):
            self.run_main()
        self.assertFalse(self.out.exists())

    def test_build_uses_ordered_stages_and_isolated_package(self):
        commands = MODULE.stages(self.base, self.editor, self.lua, self.out, None, 'com.example.flowcheck')
        names = [command[1] for command in commands]
        self.assertLess(names.index('remap-unity-package-scripts.py'), names.index('prepare-unity-characters.py'))
        self.assertLess(names.index('prepare-unity-characters.py'), names.index('prepare-unity-texture-imports.py'))
        self.assertLess(names.index('prepare-unity-texture-imports.py'), names.index('build-unity-runtime.py'))
        self.assertLess(names.index('prepare-android-input.py'), names.index('import-unity-workspace.py'))
        self.assertLess(names.index('prepare-unity-grass-mesh.py'), names.index('import-unity-workspace.py'))
        self.assertLess(names.index('prepare-unity-standard-materials.py'), names.index('import-unity-workspace.py'))
        material_args = commands[names.index('prepare-unity-standard-materials.py')][2]
        self.assertEqual(material_args[material_args.index('--backup') + 1], str(self.out / 'standard-material-backup'))
        self.assertLess(names.index('prepare-unity-manager-placeholders.py'), names.index('import-unity-workspace.py'))
        self.assertLess(names.index('prepare-unity-remove-water-probes.py'), names.index('import-unity-workspace.py'))
        self.assertLess(names.index('rebuild-unity-localization.py'), names.index('prepare-android-addressables.py'))
        self.assertEqual(names[-1], 'build-unity-runtime.py')
        self.assertIn('--embedded', commands[-1][2])
        self.assertIn('com.example.flowcheck', commands[-1][2])

    def test_prepare_only_omits_embedded_build(self):
        commands = MODULE.stages(self.base, self.editor, self.lua, self.out, None, 'com.example.flowcheck', prepare_only=True)
        self.assertEqual(commands[-1][1], 'prepare-android-addressables.py')
        self.assertNotIn('build-unity-runtime.py', [stage[1] for stage in commands])

    def test_rewired_flag_reaches_runtime_stage(self):
        commands = MODULE.stages(self.base, self.editor, self.lua, self.out, None, 'com.example.flowcheck', include_rewired=True)
        self.assertIn('--include-rewired', commands[-1][2])


if __name__ == '__main__':
    unittest.main()
