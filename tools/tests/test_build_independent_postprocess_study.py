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
Path(sys.argv[sys.argv.index('-logFile') + 1]).write_text('Unity Editor version: 2023.2.22f1\\nOPENBONK_MINIMAL_POSTPROCESS_SUCCESS')
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


if __name__ == '__main__':
    unittest.main()
