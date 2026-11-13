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


if __name__ == '__main__':
    unittest.main()
