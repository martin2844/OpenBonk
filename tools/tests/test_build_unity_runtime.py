import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'build-unity-runtime.py'
PLUGINS = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')


class RuntimeBuildTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-runtime-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        (self.project / 'Assets').mkdir(parents=True)
        (self.project / 'ProjectSettings').mkdir()
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
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
        self.editor.write_text('#!' + sys.executable + '\n' + body)
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

    def test_existing_response_file_is_preserved(self):
        self.write_editor('pass\n')
        rsp = self.project / 'Assets/csc.rsp'
        rsp.write_text('keep')
        self.assertNotEqual(self.run_tool().returncode, 0)
        self.assertEqual(rsp.read_text(), 'keep')
        self.assertFalse(self.out.exists())

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


if __name__ == '__main__':
    unittest.main()
