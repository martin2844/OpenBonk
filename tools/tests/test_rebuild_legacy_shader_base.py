import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import warnings
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'rebuild-legacy-shader-base.py'
spec = importlib.util.spec_from_file_location('legacy_shader_base', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ExtractTest(unittest.TestCase):
    def test_scene_stage_requires_steam_and_shared_materials(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--base', 'base', '--shader-bundles', 'shaders',
                                 '--output', 'output', '--scene-source-obb', 'original.obb'],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--scene-source-obb requires', result.stderr)

    def test_transform_stage_requires_original_scene_obb(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--base', 'base', '--shader-bundles', 'shaders',
                                 '--output', 'output', '--include-scene-transforms'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--include-scene-transforms requires', result.stderr)

    def test_splash_stage_requires_scene_transform_stage(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--base', 'base', '--shader-bundles', 'shaders',
                                 '--output', 'output', '--include-splash'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--include-splash requires', result.stderr)

    def test_preload_stage_requires_splash_stage(self):
        result = subprocess.run([sys.executable, str(SCRIPT), '--base', 'base', '--shader-bundles', 'shaders',
                                 '--output', 'output', '--preload-asset', 'asset'], capture_output=True, text=True)
        self.assertEqual(result.returncode, 2)
        self.assertIn('--preload-asset requires', result.stderr)

    def test_orders_split_assets_numerically(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root / 'base.obb'
            with zipfile.ZipFile(archive, 'w') as out:
                for i in reversed(range(12)):
                    out.writestr('asset.split' + str(i), bytes([i]))
            module.extract(archive, root/'asset', 'asset.split', True)
            self.assertEqual((root/'asset').read_bytes(), bytes(range(12)))

    def test_rejects_missing_and_duplicate_parts(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            archive = root/'base.obb'
            with zipfile.ZipFile(archive, 'w') as out:
                out.writestr('asset.split1', b'a')
            with self.assertRaisesRegex(ValueError, 'incomplete split'):
                module.extract(archive, root/'asset', 'asset.split', True)
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                with zipfile.ZipFile(archive, 'w') as out:
                    out.writestr('asset.split0', b'a')
                    out.writestr('asset.split0', b'b')
            with self.assertRaisesRegex(ValueError, 'duplicate'):
                module.extract(archive, root/'asset', 'asset.split', True)


if __name__ == '__main__':
    unittest.main()
