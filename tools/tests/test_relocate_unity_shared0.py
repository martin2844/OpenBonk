import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'relocate-unity-shared0.py'
spec = importlib.util.spec_from_file_location('shared0_relocate', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RelocateShared0Tests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.apk = self.root / 'runtime.apk'; self.obb = self.root / 'runtime.obb'
        self.gradle = self.root / 'gradle'; data = self.gradle / module.DATA; data.mkdir(parents=True)
        self.name = module.PREFIX + '0'; self.fragment = data / 'sharedassets0.assets.split0'; self.fragment.write_bytes(b'private bytes')
        with zipfile.ZipFile(self.apk, 'w') as z:
            z.writestr('assets/unity_obb_guid', b'1' * 36); z.writestr(self.name, b'private bytes')
            z.writestr('assets/keep', b'keep'); z.writestr('META-INF/MANIFEST.MF', b'signature')
        with zipfile.ZipFile(self.obb, 'w') as z:
            z.writestr('unity_obb_guid', b'1' * 36); z.writestr('existing', b'game data')
        self.output = self.root / 'output'

    def invoke(self, *flags):
        return subprocess.run([sys.executable, str(SCRIPT), '--runtime-apk', str(self.apk), '--runtime-obb', str(self.obb),
                               '--gradle-project', str(self.gradle), '--output', str(self.output), *flags], capture_output=True, text=True)

    def test_relocates_exact_bytes_and_preserves_inputs(self):
        result = self.invoke(); self.assertEqual(result.returncode, 0, result.stderr)
        with zipfile.ZipFile(self.output / 'runtime.apk') as apk, zipfile.ZipFile(self.output / 'runtime.obb') as obb:
            self.assertNotIn(self.name, apk.namelist()); self.assertNotIn('META-INF/MANIFEST.MF', apk.namelist())
            self.assertEqual(apk.read('assets/keep'), b'keep'); self.assertEqual(obb.read(self.name), b'private bytes')
            self.assertEqual(obb.read('existing'), b'game data')
        self.assertFalse((self.output / 'gradle' / module.DATA / self.fragment.name).exists())
        self.assertEqual(self.fragment.read_bytes(), b'private bytes')

    def test_check_only_does_not_write(self):
        self.assertEqual(self.invoke('--check').returncode, 0)
        self.assertFalse(self.output.exists())

    def test_mismatched_gradle_and_conflicting_obb_are_rejected(self):
        self.fragment.write_bytes(b'changed bytes')
        self.assertNotEqual(self.invoke().returncode, 0); self.assertFalse(self.output.exists())
        self.fragment.write_bytes(b'private bytes')
        with zipfile.ZipFile(self.obb, 'a') as z: z.writestr(self.name, b'conflict')
        self.assertNotEqual(self.invoke().returncode, 0); self.assertFalse(self.output.exists())

    def test_missing_fragment_and_wrong_guid_rejected(self):
        with zipfile.ZipFile(self.apk, 'w') as z:
            z.writestr('assets/unity_obb_guid', b'2' * 36); z.writestr(module.PREFIX + '1', b'private bytes')
        self.assertNotEqual(self.invoke().returncode, 0); self.assertFalse(self.output.exists())


if __name__ == '__main__': unittest.main()
