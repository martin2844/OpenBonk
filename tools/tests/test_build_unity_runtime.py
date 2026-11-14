import json
import importlib.util
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'build-unity-runtime.py'
PLUGINS = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')
spec = importlib.util.spec_from_file_location('input_axes', SCRIPT.parent / 'prepare-android-input.py')
input_axes = importlib.util.module_from_spec(spec)
spec.loader.exec_module(input_axes)
spec = importlib.util.spec_from_file_location('standard_materials', SCRIPT.parent / 'prepare-unity-standard-materials.py')
standard_materials = importlib.util.module_from_spec(spec)
spec.loader.exec_module(standard_materials)


class RuntimeBuildTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-runtime-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        (self.project / 'Assets').mkdir(parents=True)
        (self.project / 'Assets/Mesh').mkdir()
        (self.project / 'Assets/Mesh/GrassQuads.asset').write_text('Mesh:\n  m_Name: GrassQuads\n  m_IsReadable: 1\n  m_IndexBuffer: 001122\n  m_VertexData:\n    m_VertexCount: 8\n')
        (self.project / 'Assets/Material').mkdir()
        for name in standard_materials.NAMES:
            (self.project / 'Assets/Material' / (name + '.mat')).write_bytes(
                b'Material:\n  m_Name: ' + name.encode() + b'\n' + standard_materials.STANDARD + b'\n  m_EnableInstancingVariants: 0\n')
        (self.project / 'ProjectSettings').mkdir()
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
        empty = '%YAML 1.1\nInputManager:\n  m_Axes:\n  m_UsePhysicalKeys: 0\n'
        (self.project / 'ProjectSettings/InputManager.asset').write_text(input_axes.render(empty, input_axes.definitions()))
        (self.project / '.openbonk-workspace.json').write_text(json.dumps({'schema': 1, 'kind': 'openbonk-local-unity-workspace'}))
        self.editor = self.root / 'Editor/Unity'
        self.editor.parent.mkdir()
        aapt = self.editor.parent / 'Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/34.0.0/aapt2'
        aapt.parent.mkdir(parents=True)
        aapt.write_text('fixture')
        self.plugins = self.root / 'plugins'
        self.plugins.mkdir()
        for name in PLUGINS:
            (self.plugins / name).write_bytes(b'local plugin fixture')
        self.out = self.root / 'result'

    def write_editor(self, body):
        self.editor.write_text('#!' + sys.executable + '\nimport sys\nfrom pathlib import Path\nPath(sys.argv[sys.argv.index("-logFile")+1]).write_text("")\n' + body)
        self.editor.chmod(0o700)

    def run_tool(self, *extra):
        return subprocess.run([sys.executable, str(SCRIPT), '--project', str(self.project),
            '--unity', str(self.editor), '--plugins', str(self.plugins), '--build-root', str(self.out), *extra],
            text=True, capture_output=True)

    def test_check_success_and_no_overwrite(self):
        self.write_editor("import os,sys\nfrom pathlib import Path\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android'\n(Path(sys.argv[sys.argv.index('-projectPath')+1])/'Assets/csc.rsp.meta').write_text('generated')\n(p/'Megabonk-arm64-runtime-research.apk').write_bytes(b'apk')\n(p/'Megabonk-arm64-runtime-research.main.obb').write_bytes(b'obb')\n")
        self.assertEqual(self.run_tool('--check').returncode, 0)
        self.assertFalse(self.out.exists())
        result = self.run_tool()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.out / 'android/Megabonk-arm64-runtime-research.apk').read_bytes(), b'apk')
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())
        self.assertFalse((self.project / 'Assets/csc.rsp.meta').exists())
        self.assertNotEqual(self.run_tool().returncode, 0)

    def test_failed_unity_preserves_output_and_cleans_response_file(self):
        self.write_editor('import sys\nsys.exit(7)\n')
        result = self.run_tool()
        self.assertNotEqual(result.returncode, 0)
        self.assertTrue(self.out.is_dir())
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_include_rewired_uses_the_plugin_build_flags(self):
        self.write_editor("import os,sys\nfrom pathlib import Path\nassert os.environ['OPENBONK_NO_REWIRED_CODE'] == '0'\nassert os.environ['OPENBONK_NO_REWIRED_PLUGINS'] == '0'\nassert os.environ['OPENBONK_NO_REWIRED_SCENE'] == '1'\nassert (Path(sys.argv[sys.argv.index('-projectPath')+1])/'Assets/csc.rsp').read_text() == '-define:OPENBONK_BETA_NO_PROBES\\n'\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android'\n(p/'Megabonk-arm64-launcher-test.apk').write_bytes(b'apk')\n")
        result = self.run_tool('--embedded', '--include-rewired')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_existing_response_file_is_preserved(self):
        self.write_editor('pass\n')
        rsp = self.project / 'Assets/csc.rsp'
        rsp.write_text('keep')
        self.assertNotEqual(self.run_tool().returncode, 0)
        self.assertEqual(rsp.read_text(), 'keep')
        self.assertFalse(self.out.exists())

    def test_missing_localization_settings_in_build_log_fails(self):
        self.write_editor("import os,sys\nfrom pathlib import Path\nPath(sys.argv[sys.argv.index('-logFile')+1]).write_text('Exception: There is no active LocalizationSettings.')\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android'\n(p/'Megabonk-arm64-launcher-test.apk').write_bytes(b'apk')\n")
        result = self.run_tool('--embedded')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('without localization settings', result.stderr)
        self.assertTrue(self.out.is_dir())
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_gradle_export_requires_expected_files(self):
        self.write_editor("import os,sys\nfrom pathlib import Path\nassert sys.argv[sys.argv.index('-executeMethod')+1] == 'NativeAndroidBuild.ExportArm64RuntimeGradleProject'\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android/OpenBonk-gradle-project'\nfor f in ('settings.gradle','launcher/build.gradle','unityLibrary/build.gradle'):\n q=p/f; q.parent.mkdir(parents=True,exist_ok=True); q.write_text('fixture')\n")
        result = self.run_tool('--gradle-project')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((self.out / 'android/OpenBonk-gradle-project/settings.gradle').is_file())
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_isolated_application_id_is_passed_to_unity(self):
        self.write_editor("import os\nfrom pathlib import Path\nassert os.environ['OPENBONK_RUNTIME_APPLICATION_ID'] == 'com.example.cleancheck'\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android'\n(p/'Megabonk-arm64-runtime-research.apk').write_bytes(b'apk')\n(p/'Megabonk-arm64-runtime-research.main.obb').write_bytes(b'obb')\n")
        result = self.run_tool('--application-id', 'com.example.cleancheck')
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_invalid_application_id_is_rejected(self):
        self.write_editor('pass\n')
        result = self.run_tool('--application-id', 'invalid/id')
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.out.exists())

    def test_missing_android_axes_are_rejected_before_build(self):
        self.write_editor('pass\n')
        (self.project / 'ProjectSettings/InputManager.asset').write_text('%YAML 1.1\nInputManager:\n  m_Axes:\n  m_UsePhysicalKeys: 0\n')
        result = self.run_tool()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Android input axes are missing or differ', result.stderr)
        self.assertFalse(self.out.exists())

    def test_unreadable_grass_is_rejected_before_build(self):
        self.write_editor('pass\n')
        path = self.project / 'Assets/Mesh/GrassQuads.asset'
        path.write_text(path.read_text().replace('m_IsReadable: 1', 'm_IsReadable: 0'))
        result = self.run_tool()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('GrassQuads is not readable', result.stderr)
        self.assertFalse(self.out.exists())

    def test_unprepared_standard_material_is_rejected_before_build(self):
        self.write_editor('pass\n')
        path = self.project / 'Assets/Material/Black.mat'
        path.write_bytes(path.read_bytes().replace(b'm_EnableInstancingVariants: 0', b'm_EnableInstancingVariants: 1'))
        result = self.run_tool()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Standard materials require Android instancing preparation', result.stderr)
        self.assertFalse(self.out.exists())

    def test_embedded_build_uses_single_apk_entry_point(self):
        self.write_editor("import os,sys\nfrom pathlib import Path\nassert sys.argv[sys.argv.index('-executeMethod')+1] == 'NativeAndroidBuild.BuildArm64EmbeddedLauncherApk'\nassert os.environ['OPENBONK_RUNTIME_APPLICATION_ID'] == 'com.example.cleancheck'\nassert os.environ['OPENBONK_NO_REWIRED_SCENE'] == '1'\np=Path(os.environ['OPENBONK_BUILD_ROOT'])/'android'\n(p/'Megabonk-arm64-launcher-test.apk').write_bytes(b'apk')\n")
        result = self.run_tool('--embedded', '--application-id', 'com.example.cleancheck')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((self.out / 'android/Megabonk-arm64-launcher-test.apk').is_file())
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())


if __name__ == '__main__':
    unittest.main()
