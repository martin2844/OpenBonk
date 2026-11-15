import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch


SCRIPT = Path(__file__).resolve().parents[1] / 'build-local-from-steam.py'
SPEC = importlib.util.spec_from_file_location('build_local_from_steam', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class SteamBuildTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-steam-flow-test-')
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        game = root / 'game'
        game.mkdir()
        for name in ('Unity', 'AssetRipper', 'lua.so'):
            file = root / name
            file.write_bytes(b'fixture')
            file.chmod(0o700)
        self.out = root / 'output'
        self.argv = [str(SCRIPT), '--game', str(game), '--assetripper', str(root / 'AssetRipper'),
                     '--unity', str(root / 'Unity'), '--lua-library', str(root / 'lua.so'),
                     '--output', str(self.out), '--application-id', 'com.example.steamcheck']
        self.unity_probe = patch.object(MODULE, 'check_unity_editor')
        self.unity_probe.start()
        self.addCleanup(self.unity_probe.stop)

    def run_main(self, *extra):
        with patch.object(sys, 'argv', self.argv + list(extra)), contextlib.redirect_stdout(io.StringIO()), \
                contextlib.redirect_stderr(io.StringIO()):
            MODULE.main()

    @patch.object(MODULE.subprocess, 'run')
    def test_check_does_not_create_output(self, run):
        self.run_main('--check')
        self.assertEqual(run.call_count, 1)
        self.assertIn('--check', run.call_args.args[0])
        self.assertFalse(self.out.exists())

    @patch.object(MODULE.subprocess, 'run')
    def test_export_failure_stops_and_preserves_local_output(self, run):
        run.side_effect = [None, subprocess.CalledProcessError(1, 'export')]
        with self.assertRaises(SystemExit):
            self.run_main()
        self.assertEqual(run.call_count, 2)
        self.assertTrue(self.out.is_dir())

    @patch.object(MODULE.subprocess, 'run')
    def test_export_precedes_build_and_preserves_package_id(self, run):
        self.run_main()
        self.assertEqual(run.call_count, 3)
        self.assertIn('export-steam-unity-project.py', run.call_args_list[1].args[0][1])
        command = run.call_args_list[2].args[0]
        self.assertIn('build-local-unity-apk.py', command[1])
        self.assertIn(str(self.out / 'export/ExportedProject'), command)
        self.assertIn('com.example.steamcheck', command)

    @patch.object(MODULE.subprocess, 'run')
    def test_prepare_only_passed_to_unity_workspace_builder(self, run):
        self.run_main('--prepare-only')
        self.assertIn('--prepare-only', run.call_args_list[2].args[0])

    @patch.object(MODULE.subprocess, 'run')
    def test_rewired_flag_reaches_unity_build(self, run):
        self.run_main('--include-rewired')
        self.assertIn('--include-rewired', run.call_args_list[2].args[0])

    @patch.object(MODULE.subprocess, 'run')
    def test_builds_lua_in_output_when_no_library_is_given(self, run):
        unity = Path(self.argv[self.argv.index('--unity') + 1])
        compiler = unity.parent / 'Data/PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android23-clang'
        compiler.parent.mkdir(parents=True)
        compiler.write_bytes(b'fixture')
        compiler.chmod(0o700)
        index = self.argv.index('--lua-library')
        del self.argv[index:index + 2]
        self.run_main()
        self.assertEqual(run.call_count, 4)
        lua_call = run.call_args_list[1]
        self.assertEqual(lua_call.kwargs['env']['OPENBONK_LUA_OUTPUT'], str(self.out / 'lua'))
        self.assertIn(str(self.out / 'lua/libmegabonk_lua.so'), run.call_args_list[3].args[0])

    def test_editor_probe_runs_through_wrapper_and_checks_version(self):
        self.unity_probe.stop()
        editor = Path(self.argv[self.argv.index('--unity') + 1])
        editor.write_text('#!/bin/sh\n[ "$1" = "-version" ] || exit 1\nprintf "2023.2.22f1\\n"\n')
        wrapper = editor.parent / 'wrapper'
        wrapper.write_text('#!/bin/sh\nexec "$@"\n')
        wrapper.chmod(0o700)
        MODULE.check_unity_editor(editor, str(wrapper))
        editor.write_text('#!/bin/sh\nprintf "2022.3.0f1\\n"\n')
        with self.assertRaisesRegex(ValueError, '2023.2.22f1 is required'):
            MODULE.check_unity_editor(editor, str(wrapper))

    def test_editor_probe_reports_loader_failure(self):
        self.unity_probe.stop()
        editor = Path(self.argv[self.argv.index('--unity') + 1])
        editor.write_text('#!/bin/sh\nprintf "missing shared library\\n" >&2\nexit 127\n')
        with self.assertRaisesRegex(ValueError, 'missing shared library'):
            MODULE.check_unity_editor(editor, None)


if __name__ == '__main__':
    unittest.main()
