import subprocess
import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).resolve().parents[1] / 'rebuild-legacy-compact-chain.py'


class CompactChainTests(unittest.TestCase):
    def test_shader_rebuild_requires_project_and_editor(self):
        base = [sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--output', 'output']
        result = subprocess.run(base + ['--rebuild-shader-bundles'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--rebuild-shader-bundles requires --shader-project and --shader-unity', result.stderr)
        result = subprocess.run(base + ['--shader-project', 'project'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('require --rebuild-shader-bundles', result.stderr)

    def test_package_shader_bundles_require_cache(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                                 '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                                 '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--package-shader-bundles', 'package',
                                 '--output', 'output'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--package-shader-bundles requires --shader-bundles', result.stderr)

    def test_launcher_aliases_are_exact_and_do_not_replace_files(self):
        spec = importlib.util.spec_from_file_location('compact_chain', SCRIPT)
        module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
        package_spec = importlib.util.spec_from_file_location('conversion_package', SCRIPT.parent/'package-conversion-plans.py')
        package = importlib.util.module_from_spec(package_spec); package_spec.loader.exec_module(package)
        shared_names = {name for name in package.EXPECTED_NAMES if name.startswith('shared1-') and name.endswith('.bin')}
        already_named = {f'shared1-{stage}.bin' for stage in ('shaders', 'animation', 'cubemap', 'outline', 'final')}
        self.assertEqual(shared_names, {name for name in module.LAUNCHER_PLAN_NAMES.values() if name.startswith('shared1-')} | already_named)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for index, name in enumerate(module.LAUNCHER_PLAN_NAMES):
                (root/name).write_bytes(str(index).encode())
            module.copy_launcher_aliases(root)
            for source, destination in module.LAUNCHER_PLAN_NAMES.items():
                self.assertEqual((root/source).read_bytes(), (root/destination).read_bytes())
            with self.assertRaises(FileExistsError): module.copy_launcher_aliases(root)

    def test_texture_plans_require_mesh_plans_and_library(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                                 '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                                 '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--include-texture-plans',
                                 '--output', 'output'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--include-texture-plans requires', result.stderr)

    def test_material_mesh_plans_require_prerequisites(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                                 '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                                 '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--include-material-mesh-plans',
                                 '--output', 'output'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--include-material-mesh-plans requires', result.stderr)

    def test_layout_requires_localization(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                                 '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                                 '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--include-layout-plan',
                                 '--output', 'output'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--include-layout-plan requires --windows-data', result.stderr)

    def test_audio_plan_requires_both_steam_zips(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--steam-data', 'steam', '--old-apk', 'old.apk',
                                 '--old-obb', 'old.obb', '--new-apk', 'new.apk', '--new-obb', 'new.obb',
                                 '--crunch-decoder', 'decoder', '--rebuild-converted-assets', '--linux-zip', 'linux.zip',
                                 '--output', 'output'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--linux-zip and --windows-zip must be provided together', result.stderr)

    def test_windows_localization_requires_shader_cache(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir(); windows = root/'windows'; windows.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam), '--windows-data', str(windows), '--rebuild-converted-assets']
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'crunch-decoder'):
                path = root/name; path.write_bytes(b'invalid'); args.extend(('--'+name, str(path)))
            output = root/'output'
            result = subprocess.run(args+['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('--windows-data requires a directory and --shader-bundles', result.stderr)
            self.assertFalse(output.exists())

    def test_rejects_invalid_shader_cache_without_creating_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir()
            shader = root/'shader'; shader.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam), '--rebuild-converted-assets', '--shader-bundles', str(shader)]
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'crunch-decoder'):
                path = root/name; path.write_bytes(b'invalid')
                args.extend(('--'+name, str(path)))
            output = root/'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertFalse(output.exists())

    def test_rebuild_mode_rejects_unpinned_inputs(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); steam = root/'steam'; steam.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam), '--rebuild-converted-assets']
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'crunch-decoder'):
                path = root/name; path.write_bytes(b'invalid')
                args.extend(('--'+name, str(path)))
            output = root/'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs: old-apk', result.stderr)
            self.assertFalse(output.exists())

    def test_pinned_inputs_are_checked_before_writing(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            steam = root / 'steam'
            steam.mkdir()
            args = [sys.executable, str(SCRIPT), '--steam-data', str(steam)]
            for name in ('old-apk', 'old-obb', 'new-apk', 'new-obb', 'converted-obb', 'crunch-decoder'):
                path = root / name
                path.write_bytes(b'not the historical input')
                args.extend(('--' + name, str(path)))
            output = root / 'output'
            result = subprocess.run(args + ['--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('pinned input differs: old-apk', result.stderr)
            self.assertFalse(output.exists())
            self.assertEqual(sorted(p.name for p in root.iterdir()), sorted(['steam', 'old-apk', 'old-obb', 'new-apk', 'new-obb', 'converted-obb', 'crunch-decoder']))


if __name__ == '__main__':
    unittest.main()
