import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-rootfx.py'
spec = importlib.util.spec_from_file_location('rootfx', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

PREFAB = (b'%YAML 1.1\n--- !u!1 &10\nGameObject:\n  m_Component:\n  - component: {fileID: 20}\n'
          b'  m_Name: RootFx\n--- !u!4 &20\nTransform:\n  m_GameObject: {fileID: 10}\n'
          b'  m_LocalScale: {x: 6, y: 6, z: 6}\n  m_Father: {fileID: 0}\n'
          b'--- !u!4 &30\nTransform:\n  m_GameObject: {fileID: 99}\n  m_LocalScale: {x: 6, y: 6, z: 6}\n')


class RootFxTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'project'
        self.path = self.project / module.PREFAB
        self.path.parent.mkdir(parents=True)
        self.path.write_bytes(PREFAB)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.backup = self.root / 'rootfx-backup.prefab'

    def run_tool(self, *args):
        return subprocess.run([sys.executable, str(SCRIPT), '--project', str(self.project), *args],
                              capture_output=True, text=True)

    def test_restores_only_root_scale_and_is_idempotent(self):
        self.assertNotEqual(self.run_tool('--check').returncode, 0)
        result = self.run_tool('--backup', str(self.backup))
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(self.backup.read_bytes(), PREFAB)
        self.assertEqual(self.path.read_bytes(), PREFAB.replace(b'm_LocalScale: {x: 6, y: 6, z: 6}',
                                                                b'm_LocalScale: {x: 4, y: 4, z: 4}', 1))
        self.assertEqual(self.run_tool('--check').returncode, 0)
        self.assertEqual(self.run_tool().returncode, 0)

    def test_rejects_unexpected_scale_or_nonroot_without_writes(self):
        for invalid in (PREFAB.replace(b'{x: 6, y: 6, z: 6}', b'{x: 7, y: 7, z: 7}', 1),
                        PREFAB.replace(b'  m_Father: {fileID: 0}', b'  m_Father: {fileID: 99}', 1)):
            self.path.write_bytes(invalid)
            self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
            self.assertEqual(self.path.read_bytes(), invalid)
            self.assertFalse(self.backup.exists())

    def test_rejects_ambiguous_prefab_and_locked_workspace(self):
        self.path.write_bytes(PREFAB + b'--- !u!1 &99\nGameObject:\n  m_Name: RootFx\n')
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        self.assertFalse(self.backup.exists())
        self.path.write_bytes(PREFAB)
        lock = self.project / 'Temp/UnityLockfile'
        lock.parent.mkdir()
        lock.write_text('locked')
        result = self.run_tool('--backup', str(self.backup))
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('workspace is locked', result.stderr)
        self.assertEqual(self.path.read_bytes(), PREFAB)


if __name__ == '__main__':
    unittest.main()
