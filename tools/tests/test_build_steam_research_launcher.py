import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('steam_research_launcher', Path(__file__).resolve().parents[1] / 'build-steam-research-launcher.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class SteamResearchBuildTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root = Path(self.temp.name)
        self.game = self.root / 'game'; (self.game / 'Megabonk_Data').mkdir(parents=True)
        (self.game / 'Megabonk_Data/globalgamemanagers.assets').write_bytes(b'data')
        self.unity = self.root / 'Editor/Unity'; self.unity.parent.mkdir(); self.unity.write_bytes(b'editor')
        self.ripper = self.root / 'ripper'; self.ripper.write_bytes(b'ripper')
        self.jar = self.root / 'json.jar'
        with zipfile.ZipFile(self.jar, 'w') as z: z.writestr('fixture', b'data')
        self.out = self.root / 'output'
        self.argv = ['program', '--game', str(self.game), '--assetripper', str(self.ripper), '--unity', str(self.unity),
                     '--json-jar', str(self.jar), '--output', str(self.out), '--application-id', 'com.example.openbonk']

    def invoke(self, *extra):
        with patch.object(sys, 'argv', self.argv + list(extra)), patch.object(module.importlib.util, 'find_spec', return_value=object()), \
                patch.object(module.research_chain, 'validate_steam_inputs'), \
                contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()): module.main()

    def test_bad_steam_data_fails_before_unity_export(self):
        with patch.object(sys, 'argv', self.argv), patch.object(module.importlib.util, 'find_spec', return_value=object()), \
                patch.object(module.research_chain, 'validate_steam_inputs', side_effect=ValueError('unsupported data')), \
                patch.object(module.subprocess, 'run') as run, contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit): module.main()
            run.assert_not_called()
            self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_check_only_performs_preflight(self, run):
        self.invoke('--check')
        self.assertEqual(run.call_count, 1)
        self.assertIn('--check', run.call_args.args[0]); self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_builds_in_order_and_preserves_workspace_paths(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        self.invoke()
        self.assertEqual(run.call_count, 5)
        commands = [call.args[0] for call in run.call_args_list[1:]]
        self.assertTrue(commands[0][1].endswith('build-local-from-steam.py'))
        self.assertIn('--prepare-only', commands[0])
        self.assertTrue(commands[1][1].endswith('build-unity-runtime.py'))
        self.assertIn('--gradle-project', commands[2])
        self.assertTrue(commands[3][1].endswith('build-local-research-chain.py'))
        self.assertIn(str(self.out / 'steam/build/workspace'), commands[3])
        self.assertTrue((self.out / 'build.log').is_file())

    @patch.object(module.subprocess, 'run')
    def test_failure_stops_and_retains_log(self, run):
        run.side_effect = [subprocess.CompletedProcess([], 0), subprocess.CompletedProcess([], 1)]
        with self.assertRaises(SystemExit): self.invoke()
        self.assertEqual(run.call_count, 2)
        self.assertTrue((self.out / 'build.log').is_file())

    @patch.object(module.subprocess, 'run')
    def test_rewired_flag_is_used_for_both_unity_outputs(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        self.invoke('--include-rewired')
        commands = [call.args[0] for call in run.call_args_list[1:]]
        self.assertIn('--include-rewired', commands[1])
        self.assertIn('--include-rewired', commands[2])

    @patch.object(module.subprocess, 'run')
    def test_dual_build_orders_platforms_and_reuses_linux_tools(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows))
        self.assertEqual(run.call_count, 7)
        windows_cmd = run.call_args_list[-2].args[0]; dual_cmd = run.call_args_list[-1].args[0]
        self.assertEqual(windows_cmd[windows_cmd.index('--platform') + 1], 'windows')
        self.assertEqual(windows_cmd[windows_cmd.index('--host-library') + 1], str(self.out / 'research/native/libopenbonk_texture.so'))
        self.assertEqual(dual_cmd[dual_cmd.index('--windows-research') + 1], str(self.out / 'windows-research'))
        self.assertEqual(dual_cmd[dual_cmd.index('--output') + 1], str(self.out / 'dual-launcher'))

    @patch.object(module.subprocess, 'run')
    def test_quality_option_reaches_both_platforms(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--quality-textures')
        self.assertIn('--quality-textures', run.call_args_list[-3].args[0])
        self.assertIn('--quality-textures', run.call_args_list[-2].args[0])


if __name__ == '__main__': unittest.main()
