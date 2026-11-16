import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'build-independent-postprocess-study.py'


class IndependentPostprocessStudyTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.package = self.root / 'package'
        self.package.mkdir()
        (self.package / 'package.json').write_text(json.dumps({'name': 'com.unity.postprocessing', 'version': '3.4.0'}))
        (self.package / 'Tests').mkdir()
        (self.package / 'Tests/example.cs').write_text('fixture')
        self.editor = self.root / 'Unity'
        self.editor.write_text('#!' + sys.executable + '''
import os, sys
from pathlib import Path
p = Path(sys.argv[sys.argv.index('-projectPath') + 1])
assert not (p / 'Packages/com.unity.postprocessing/Tests').exists()
assert (p / 'Assets/Editor/OpenBonkPostprocessStudy.cs').is_file()
Path(os.environ['OPENBONK_POSTPROCESS_OUTPUT'], 'postprocess-study').write_bytes(b'bundle')
marker = '\\nOPENBONK_POSTPROCESS_COMPUTE_OK' if os.environ.get('OPENBONK_POSTPROCESS_COMPUTE') == '1' else ''
if marker:
    for n in 'autoexposure multiscalevorender multiscalevoupsample multiscalevodownsample1 multiscalevodownsample2 lut3dbaker histogram texture3dlerp gaussiandownsample exposurehistogram waveform vectorscope'.split():
        Path(os.environ['OPENBONK_POSTPROCESS_OUTPUT'], n).write_bytes(b'compute')
Path(sys.argv[sys.argv.index('-logFile') + 1]).write_text('Unity Editor version: 2023.2.22f1\\nOPENBONK_MINIMAL_POSTPROCESS_SUCCESS' + marker)
''')
        self.editor.chmod(0o700)
        self.output = self.root / 'output'

    def run_tool(self, *args):
        return subprocess.run([sys.executable, str(SCRIPT), '--unity', str(self.editor), '--package', str(self.package),
                               '--output', str(self.output), *args], capture_output=True, text=True)

    def test_isolated_build_preserves_package_and_refuses_overwrite(self):
        self.assertEqual(self.run_tool('--check').returncode, 0)
        self.assertFalse(self.output.exists())
        result = self.run_tool()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.output / 'bundles/postprocess-study').read_bytes(), b'bundle')
        self.assertTrue((self.package / 'Tests/example.cs').is_file())
        self.assertNotEqual(self.run_tool().returncode, 0)

    def test_rejects_wrong_package_and_symlinks(self):
        (self.package / 'package.json').write_text('{"name":"com.unity.postprocessing","version":"0"}')
        self.assertNotEqual(self.run_tool('--check').returncode, 0)
        (self.package / 'package.json').write_text('{"name":"com.unity.postprocessing","version":"3.4.0"}')
        (self.package / 'linked').symlink_to(self.package / 'package.json')
        self.assertNotEqual(self.run_tool('--check').returncode, 0)
        self.assertFalse(self.output.exists())

    def test_optional_graphics_settings_are_copied_without_modifying_input(self):
        source = self.root / 'GraphicsSettings.asset'; source.write_text('graphics fixture')
        result = self.run_tool('--graphics-settings', str(source))
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.output / 'project/ProjectSettings/GraphicsSettings.asset').read_text(), 'graphics fixture')
        self.assertEqual(source.read_text(), 'graphics fixture')

    def test_vulkan_pretransform_flag_reaches_isolated_editor(self):
        source = self.editor.read_text()
        source = source.replace("p = Path(sys.argv[sys.argv.index('-projectPath') + 1])", "assert os.environ.get('OPENBONK_POSTPROCESS_VULKAN_PRETRANSFORM') == '1'\np = Path(sys.argv[sys.argv.index('-projectPath') + 1])")
        self.editor.write_text(source)
        result = self.run_tool('--vulkan-pretransform')
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_pinned_verification_rejects_wrong_bundle_and_requires_vulkan(self):
        self.assertNotEqual(self.run_tool('--verify-pinned', '--check').returncode, 0)
        result = self.run_tool('--verify-pinned', '--vulkan-pretransform')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('differs from the pinned build', result.stderr)

    def test_compute_build_is_requested_and_must_report_success(self):
        result = self.run_tool('--include-compute')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn('OPENBONK_POSTPROCESS_COMPUTE_OK', (self.output/'unity.log').read_text())


if __name__ == '__main__':
    unittest.main()
