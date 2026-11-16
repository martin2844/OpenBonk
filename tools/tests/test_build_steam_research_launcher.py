import contextlib
import importlib.util
import io
from pathlib import Path
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('steam_research_launcher', Path(__file__).resolve().parents[1] / 'build-steam-research-launcher.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class SteamResearchBuildTests(unittest.TestCase):
    @patch.object(module.subprocess, 'run')
    def test_version_overrides_reach_both_chains_and_dual_packager(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--version-code', '2', '--version-name', '0.1.1-beta')
        for call in run.call_args_list[-3:]:
            command = call.args[0]
            self.assertEqual(command[command.index('--version-code') + 1], '2')
            self.assertEqual(command[command.index('--version-name') + 1], '0.1.1-beta')

    @patch.object(module.subprocess, 'run')
    def test_invalid_android_version_is_rejected(self, run):
        for args in (('--version-code', '0'), ('--version-code', '2100000001'),
                     ('--version-name', ''), ('--version-name', 'bad"name')):
            with self.assertRaises(SystemExit): self.invoke(*args)

    @patch.object(module.subprocess, 'run')
    def test_final_apk_version_verification(self, run):
        run.return_value = subprocess.CompletedProcess([], 0, "package: name='com.example.app' versionCode='2' versionName='0.1.1-beta'\n")
        module.check_apk_version('aapt2', 'app.apk', 2, '0.1.1-beta')
        with self.assertRaises(ValueError): module.check_apk_version('aapt2', 'app.apk', 3, '0.1.1-beta')
        with self.assertRaises(ValueError): module.check_apk_version('aapt2', 'app.apk', 2, 'wrong')
        run.return_value.stdout = 'not an APK\n'
        with self.assertRaises(ValueError): module.check_apk_version('aapt2', 'app.apk', 2, None)

    @patch.object(module.subprocess, 'run')
    def test_online_flag_reaches_both_platforms_and_dual_packager(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--gradle-online')
        for call in run.call_args_list[-3:]: self.assertIn('--gradle-online', call.args[0])

    @patch.object(module.subprocess, 'run')
    def test_animation_curve_option_reaches_both_platforms(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--animation-curve-fallback', '--animation-curve-relaxed')
        chains = [call.args[0] for call in run.call_args_list if any(str(x).endswith('build-local-research-chain.py') for x in call.args[0])]
        self.assertEqual(len(chains), 2)
        for command in chains:
            self.assertIn('--animation-curve-fallback', command)
            self.assertIn('--animation-curve-relaxed', command)

    @patch.object(module.subprocess, 'run')
    def test_extended_animation_bound_reaches_both_platforms(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--animation-curve-fallback', '--animation-curve-extended', '--animation-curve-hybrid')
        chains = [call.args[0] for call in run.call_args_list if any(str(x).endswith('build-local-research-chain.py') for x in call.args[0])]
        self.assertEqual(len(chains), 2)
        for command in chains:
            self.assertIn('--animation-curve-extended', command)
            self.assertIn('--animation-curve-hybrid', command)

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup); self.root = Path(self.temp.name)
        self.game = self.root / 'game'; (self.game / 'Megabonk_Data').mkdir(parents=True)
        (self.game / 'Megabonk_Data/globalgamemanagers.assets').write_bytes(b'data')
        self.unity = self.root / 'Editor/Unity'; self.unity.parent.mkdir(); self.unity.write_bytes(b'editor')
        self.ripper = self.root / 'ripper'; self.ripper.write_bytes(b'ripper')
        self.jar = self.root / 'json.jar'
        with zipfile.ZipFile(self.jar, 'w') as z:
            for name in ('JSONObject', 'JSONArray', 'JSONTokener', 'JSONException'):
                z.writestr('org/json/' + name + '.class', b'fixture')
        android = self.unity.parent / 'Data/PlaybackEngines/AndroidPlayer'
        for name in ('OpenJDK/bin/java', 'OpenJDK/bin/javac', 'SDK/build-tools/34.0.0/aapt2'):
            path = android / name; path.parent.mkdir(parents=True, exist_ok=True); path.write_bytes(b'fixture'); path.chmod(0o755)
        compiler = android / 'NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        compiler.parent.mkdir(parents=True); compiler.write_bytes(b'fixture'); compiler.chmod(0o755)
        self.out = self.root / 'output'
        self.argv = ['program', '--game', str(self.game), '--assetripper', str(self.ripper), '--unity', str(self.unity),
                     '--json-jar', str(self.jar), '--output', str(self.out), '--application-id', 'com.example.openbonk']

    def invoke(self, *extra):
        with patch.object(sys, 'argv', self.argv + list(extra)), patch.object(module, 'check_conversion_imports'), \
                patch.object(module.research_chain, 'validate_steam_inputs'), \
                patch.object(module, 'check_online_apk'), patch.object(module, 'check_apk_version'), \
                contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()): module.main()

    def test_final_permission_check_requires_online_permissions(self):
        base = "package: com.example.openbonk\n"
        online = base + "uses-permission: name='android.permission.INTERNET'\nuses-permission: name='android.permission.REQUEST_INSTALL_PACKAGES'\n"
        with patch.object(module.subprocess, 'run', return_value=subprocess.CompletedProcess([], 0, stdout=online)):
            module.check_online_apk(self.ripper, self.root / 'app.apk', 'com.example.openbonk')
        with patch.object(module.subprocess, 'run', return_value=subprocess.CompletedProcess([], 0, stdout=base)):
            with self.assertRaisesRegex(ValueError, 'missing required permission'):
                module.check_online_apk(self.ripper, self.root / 'app.apk', 'com.example.openbonk')
        with patch.object(module.subprocess, 'run', return_value=subprocess.CompletedProcess([], 0, stdout=online)):
            with self.assertRaisesRegex(ValueError, 'package does not match'):
                module.check_online_apk(self.ripper, self.root / 'app.apk', 'com.example.other')

    def test_bad_steam_data_fails_before_unity_export(self):
        with patch.object(sys, 'argv', self.argv), patch.object(module, 'check_conversion_imports'), \
                patch.object(module.research_chain, 'validate_steam_inputs', side_effect=ValueError('unsupported data')), \
                patch.object(module.subprocess, 'run') as run, contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit): module.main()
            run.assert_not_called()
            self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_missing_native_conversion_library_fails_before_export(self, run):
        with patch.object(sys, 'argv', self.argv), \
                patch.object(module, 'check_conversion_imports', side_effect=ValueError('missing native library')), \
                contextlib.redirect_stderr(io.StringIO()):
            with self.assertRaises(SystemExit): module.main()
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    def test_conversion_import_error_identifies_module(self):
        with patch.object(module.importlib, 'import_module', side_effect=OSError('missing native library')):
            with self.assertRaisesRegex(ValueError, 'UnityPy.export.Texture2DConverter.*missing native library'):
                module.check_conversion_imports()

    @patch.object(module.subprocess, 'run')
    def test_bad_json_jar_fails_before_export(self, run):
        with zipfile.ZipFile(self.jar, 'w') as jar: jar.writestr('unrelated', b'data')
        with self.assertRaises(SystemExit): self.invoke('--check')
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_missing_android_tools_fail_before_export(self, run):
        android = self.unity.parent / 'Data/PlaybackEngines/AndroidPlayer'
        for name in ('OpenJDK/bin/java', 'OpenJDK/bin/javac', 'SDK/build-tools/34.0.0/aapt2'):
            path = android / name
            path.unlink()
            with self.assertRaises(SystemExit): self.invoke('--check')
            run.assert_not_called()
            self.assertFalse(self.out.exists())
            path.write_bytes(b'fixture'); path.chmod(0o755)

    @patch.object(module.subprocess, 'run')
    def test_nonexecutable_android_tools_fail_before_export(self, run):
        android = self.unity.parent / 'Data/PlaybackEngines/AndroidPlayer'
        for name in ('OpenJDK/bin/java', 'OpenJDK/bin/javac', 'SDK/build-tools/34.0.0/aapt2'):
            path = android / name
            path.chmod(0o644)
            with self.subTest(tool=name), self.assertRaises(SystemExit): self.invoke('--check')
            run.assert_not_called()
            self.assertFalse(self.out.exists())
            path.chmod(0o755)

    @patch.object(module.subprocess, 'run')
    def test_missing_ndk_cxx_fails_before_export(self, run):
        compiler = self.unity.parent / 'Data/PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        compiler.unlink()
        with self.assertRaises(SystemExit): self.invoke('--check')
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_missing_host_cxx_fails_before_export(self, run):
        with patch.object(module.shutil, 'which', return_value=None):
            with self.assertRaises(SystemExit): self.invoke('--check')
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_check_only_performs_preflight(self, run):
        self.invoke('--check')
        self.assertEqual(run.call_count, 1)
        self.assertIn('--check', run.call_args.args[0]); self.assertFalse(self.out.exists())

    def test_failed_nested_preflight_reports_error_without_creating_output(self):
        errors = io.StringIO()
        with patch.object(sys, 'argv', self.argv + ['--check']), patch.object(module, 'check_conversion_imports'), \
                patch.object(module.research_chain, 'validate_steam_inputs'), \
                patch.object(module.subprocess, 'run', side_effect=subprocess.CalledProcessError(2, ['preflight'])), \
                contextlib.redirect_stderr(errors):
            with self.assertRaises(SystemExit): module.main()
        self.assertIn('Unity/export preflight failed (status 2)', errors.getvalue())
        self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_noexec_output_fails_before_export_and_leaves_no_output(self, run):
        with patch.object(module, 'output_allows_execution', return_value=False):
            with self.assertRaises(SystemExit): self.invoke('--check')
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    def test_execution_probe_leaves_no_file(self):
        before = set(self.root.iterdir())
        self.assertTrue(module.output_allows_execution(self.root))
        self.assertEqual(before, set(self.root.iterdir()))

    def test_missing_path_reports_the_argument_before_export(self):
        self.ripper.unlink()
        errors = io.StringIO()
        with patch.object(sys, 'argv', self.argv), patch.object(module.subprocess, 'run') as run, \
                contextlib.redirect_stderr(errors):
            with self.assertRaises(SystemExit): module.main()
        self.assertIn('cannot access --assetripper', errors.getvalue())
        run.assert_not_called()
        self.assertFalse(self.out.exists())

    @patch.object(module.subprocess, 'run')
    def test_dual_build_warns_about_low_disk_space_without_blocking_check(self, run):
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        errors = io.StringIO()
        with patch.object(sys, 'argv', self.argv + ['--windows-game', str(windows), '--check']), \
                patch.object(module, 'check_conversion_imports'), \
                patch.object(module.research_chain, 'validate_steam_inputs'), \
                patch.object(module.shutil, 'disk_usage', return_value=SimpleNamespace(free=1)), \
                contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(errors):
            module.main()
        self.assertIn('dual-platform build may need at least 35 GiB', errors.getvalue())
        self.assertEqual(run.call_count, 1)
        self.assertFalse(self.out.exists())

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
        self.invoke('--windows-game', str(windows), '--quality-textures', '--shared0-fonts', '--fox-fallback', '--bounded-fallback', '--normal-fallback', '--font-fallback', '--resource-fallback', '--nodraw-fallback', '--atlas-fallback', '--lightmap-fallback', '--emission-fallback', '--shadow-fallback', '--emoji-fallback')
        self.assertIn('--quality-textures', run.call_args_list[-3].args[0])
        self.assertIn('--quality-textures', run.call_args_list[-2].args[0])
        self.assertIn('--normal-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--normal-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--font-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--font-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--resource-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--resource-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--nodraw-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--nodraw-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--atlas-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--atlas-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--lightmap-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--lightmap-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--emission-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--emission-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--fox-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--fox-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--bounded-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--bounded-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--shadow-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--shadow-fallback', run.call_args_list[-2].args[0])
        self.assertIn('--emoji-fallback', run.call_args_list[-3].args[0])
        self.assertIn('--emoji-fallback', run.call_args_list[-2].args[0])

    @patch.object(module.subprocess, 'run')
    def test_shared0_relocation_precedes_both_platforms(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--shared0-fonts')
        commands = [call.args[0] for call in run.call_args_list]
        relocate, linux, windows, dual = commands[-4:]
        self.assertTrue(relocate[1].endswith('relocate-unity-shared0.py'))
        for command in (linux, windows):
            self.assertIn('--shared0-fonts', command)
            self.assertEqual(command[command.index('--runtime-obb') + 1], str(self.out / 'relocated/runtime.obb'))
            self.assertEqual(command[command.index('--runtime-apk') + 1], str(self.out / 'relocated/runtime.apk'))
        self.assertEqual(dual[dual.index('--gradle-project') + 1], str(self.out / 'relocated/gradle'))

    @patch.object(module.subprocess, 'run')
    def test_partial_rgba_reaches_both_platforms_and_dual_packager(self, run):
        run.return_value = subprocess.CompletedProcess([], 0)
        windows = self.root / 'windows'; (windows / 'Megabonk_Data').mkdir(parents=True)
        self.invoke('--windows-game', str(windows), '--partial-rgba', '--partial-etc2', '--partial-etc2-small', '--transparent-atlas-scrub', '--quality-textures')
        self.assertIn('--partial-rgba', run.call_args_list[-3].args[0])
        self.assertIn('--partial-rgba', run.call_args_list[-2].args[0])
        self.assertIn('--partial-etc2', run.call_args_list[-3].args[0])
        self.assertIn('--partial-etc2', run.call_args_list[-2].args[0])
        self.assertIn('--partial-etc2-small', run.call_args_list[-3].args[0])
        self.assertIn('--partial-etc2-small', run.call_args_list[-2].args[0])
        self.assertIn('--transparent-atlas-scrub', run.call_args_list[-3].args[0])
        self.assertIn('--transparent-atlas-scrub', run.call_args_list[-2].args[0])
        self.assertIn('--partial-rgba-plan', run.call_args_list[-1].args[0])

    @patch.object(module.subprocess, 'run')
    def test_font_fallback_requires_quality(self, run):
        with self.assertRaises(SystemExit): self.invoke('--font-fallback')
        with self.assertRaises(SystemExit): self.invoke('--resource-fallback')
        with self.assertRaises(SystemExit): self.invoke('--nodraw-fallback')
        with self.assertRaises(SystemExit): self.invoke('--lightmap-fallback')
        with self.assertRaises(SystemExit): self.invoke('--fox-fallback')
        with self.assertRaises(SystemExit): self.invoke('--bounded-fallback')
        with self.assertRaises(SystemExit): self.invoke('--animation-curve-relaxed')
        with self.assertRaises(SystemExit): self.invoke('--animation-curve-extended')
        with self.assertRaises(SystemExit): self.invoke('--animation-curve-hybrid')
        with self.assertRaises(SystemExit): self.invoke('--animation-curve-fallback', '--animation-curve-relaxed', '--animation-curve-extended')
        with self.assertRaises(SystemExit): self.invoke('--atlas-fallback', '--transparent-atlas-scrub')
        with self.assertRaises(SystemExit): self.invoke('--shadow-fallback')
        with self.assertRaises(SystemExit): self.invoke('--emoji-fallback')
        with self.assertRaises(SystemExit): self.invoke('--partial-etc2')
        with self.assertRaises(SystemExit): self.invoke('--partial-rgba', '--partial-etc2')
        with self.assertRaises(SystemExit): self.invoke('--quality-textures', '--partial-etc2')
        with self.assertRaises(SystemExit): self.invoke('--partial-etc2-small')
        run.assert_not_called()


if __name__ == '__main__': unittest.main()
