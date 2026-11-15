import importlib.util
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'check-unity-workspace-sources.py'
SPEC = importlib.util.spec_from_file_location('workspace_sources', SCRIPT)
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


class WorkspaceSourceCheck(unittest.TestCase):
    def test_detects_missing_and_changed_sources_but_allows_removed_probe(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source, workspace = root / 'source', root / 'workspace'
            for base in (source, workspace):
                (base / 'Scripts').mkdir(parents=True)
                (base / 'AndroidSplitLauncher').mkdir()
                (base / 'Resources').mkdir()
                (base / 'Scripts/Game.cs').write_text('game')
                (base / 'AndroidSplitLauncher/Launcher.java').write_text('launcher')
                (base / 'Resources/Grass.shader').write_text('grass')
            (source / 'Resources/OpenBonkWaterRawProbe.shader').write_text('optional probe')
            result = module.compare(source, workspace)
            self.assertEqual(result['shaders']['excluded_diagnostics'], ['Resources/OpenBonkWaterRawProbe.shader'])
            self.assertFalse(result['C#']['missing'])
            self.assertFalse(result['Java']['changed'])
            (workspace / 'Scripts/Game.cs').write_text('changed')
            (workspace / 'AndroidSplitLauncher/Launcher.java').unlink()
            result = module.compare(source, workspace)
            self.assertEqual(result['C#']['changed'], ['Scripts/Game.cs'])
            self.assertEqual(result['Java']['missing'], ['AndroidSplitLauncher/Launcher.java'])

    def test_empty_source_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with self.assertRaises(ValueError):
                module.compare(root, root)

    def test_launcher_sources_and_generated_placeholder(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            gradle = root / 'gradle'
            base = gradle / 'unityLibrary/src/main/java/com'
            paths = (
                ('unity/OpenBonk/Assets/AndroidSplitLauncher/Launcher.java', 'megabonk/launcher/Launcher.java'),
                ('unity/OpenBonk/Assets/AndroidSplitLauncher/SplitExpectedData.java', None),
                ('unity/OpenBonk/Assets/AndroidModPicker/Picker.java', 'megabonk/modpicker/Picker.java'),
                ('analysis/data-conversion/android/SteamFoo.java', 'openbonk/dataprep/SteamFoo.java'),
                ('analysis/data-conversion/android/LauncherDiagnosticGenerator.java', 'megabonk/launcher/LauncherDiagnosticGenerator.java'),
            )
            for source, target in paths:
                path = root / source
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(source)
                if target:
                    dest = base / target
                    dest.parent.mkdir(parents=True, exist_ok=True)
                    dest.write_text(source)
            result = module.compare_launcher_sources(root, gradle)
            self.assertEqual(result['total'], 4)
            self.assertEqual(result['generated_placeholders'], ['unity/OpenBonk/Assets/AndroidSplitLauncher/SplitExpectedData.java'])
            self.assertFalse(result['missing'])
            (base / 'openbonk/dataprep/SteamFoo.java').write_text('changed')
            (base / 'megabonk/modpicker/Picker.java').unlink()
            result = module.compare_launcher_sources(root, gradle)
            self.assertEqual(result['changed'], ['analysis/data-conversion/android/SteamFoo.java'])
            self.assertEqual(result['missing'], ['unity/OpenBonk/Assets/AndroidModPicker/Picker.java'])


if __name__ == '__main__':
    unittest.main()
