import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-standard-materials.py'
spec = importlib.util.spec_from_file_location('standard_materials', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class StandardMaterialsTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        self.directory = self.project / 'Assets/Material'
        self.directory.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        for name in module.NAMES:
            (self.directory / (name + '.mat')).write_bytes(
                b'Material:\n  m_Name: ' + name.encode() + b'\n' + module.STANDARD
                + b'\n  m_EnableInstancingVariants: 1\n  other: untouched\n')
        self.backup = self.root / 'backup'

    def run_tool(self, *extra):
        return subprocess.run([sys.executable, str(SCRIPT), '--project', str(self.project), *extra],
                              capture_output=True, text=True)

    def test_prepare_backs_up_and_only_changes_the_flag(self):
        before = {p.name: p.read_bytes() for p in self.directory.iterdir()}
        self.assertNotEqual(self.run_tool('--check').returncode, 0)
        result = self.run_tool('--backup', str(self.backup))
        self.assertEqual(result.returncode, 0, result.stderr)
        for name, data in before.items():
            self.assertEqual((self.backup / name).read_bytes(), data)
            self.assertEqual((self.directory / name).read_bytes(),
                             data.replace(b'm_EnableInstancingVariants: 1', b'm_EnableInstancingVariants: 0'))
        self.assertEqual(self.run_tool('--check').returncode, 0)
        self.assertEqual(self.run_tool().returncode, 0)

    def test_unexpected_material_rejects_all_before_writing(self):
        broken = self.directory / (module.NAMES[-1] + '.mat')
        broken.write_bytes(broken.read_bytes().replace(module.STANDARD, b'  m_Shader: unexpected'))
        before = {p.name: p.read_bytes() for p in self.directory.iterdir()}
        result = self.run_tool('--backup', str(self.backup))
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse(self.backup.exists())
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.directory.iterdir()})

    def test_rejects_unmarked_locked_or_unsafe_workspace(self):
        marker = self.project / '.openbonk-workspace.json'
        marker.unlink()
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        marker.write_text(json.dumps(module.MARKER))
        lock = self.project / 'Temp/UnityLockfile'
        lock.parent.mkdir()
        lock.touch()
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        lock.unlink()
        self.assertNotEqual(self.run_tool('--backup', str(self.project / 'backup')).returncode, 0)
        path = self.directory / (module.NAMES[0] + '.mat')
        path.unlink()
        path.symlink_to(self.directory / (module.NAMES[1] + '.mat'))
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        self.assertFalse(self.backup.exists())

    def test_duplicate_flags_are_rejected(self):
        path = self.directory / 'Black.mat'
        path.write_bytes(path.read_bytes() + b'  m_EnableInstancingVariants: 1\n')
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        self.assertFalse(self.backup.exists())


if __name__ == '__main__':
    unittest.main()
