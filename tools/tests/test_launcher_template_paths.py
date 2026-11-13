import os
from pathlib import Path
import subprocess
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'build-private-launcher-prototype.sh'


class LauncherTemplatePathsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        self.template = self.root / 'template'
        self.plans = self.root / 'plans'
        for path in (self.project, self.template, self.plans):
            path.mkdir()
        self.env = os.environ.copy()
        self.env['OPENBONK_UNITY_PROJECT'] = str(self.project)
        self.env['OPENBONK_GRADLE_TEMPLATE'] = str(self.template)

    def run_script(self, output):
        return subprocess.run(['bash', str(SCRIPT), str(self.plans), str(output)], env=self.env,
                              capture_output=True, text=True)

    def test_rejects_output_inside_template(self):
        result = self.run_script(self.template / 'output')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('outside the Unity project and Gradle template', result.stderr)
        self.assertFalse((self.template / 'output').exists())

    def test_rejects_output_inside_project(self):
        result = self.run_script(self.project / 'output')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('outside the Unity project and Gradle template', result.stderr)
        self.assertFalse((self.project / 'output').exists())

    def test_missing_template_is_rejected(self):
        self.env['OPENBONK_GRADLE_TEMPLATE'] = str(self.root / 'missing')
        result = self.run_script(self.root / 'output')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Missing Unity project or Gradle template', result.stderr)
        self.assertFalse((self.root / 'output').exists())


if __name__ == '__main__':
    unittest.main()
