import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-desert-edges.py'
spec = importlib.util.spec_from_file_location('desert_edges', SCRIPT)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class DesertEdgeTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'project'
        self.materials = self.project / 'Assets/Material'; self.materials.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        for name in module.NAMES:
            (self.materials / (name + '.mat')).write_bytes(b'Material:\n  m_Name: ' + name.encode() + b'\n' + module.SHADER + b'\n    m_Floats:\n      _OutlineWidth: 0.001\n    m_Colors:\n      _Color: red\n')
        (self.materials / 'normal.mat').write_bytes(b'unchanged')
        self.backup = self.root / 'backup'

    def run_tool(self, *args):
        return subprocess.run([sys.executable, str(SCRIPT), '--project', str(self.project), *args], capture_output=True, text=True)

    def test_prepares_both_edges_preserves_other_materials_and_is_idempotent(self):
        before = {name: (self.materials / (name + '.mat')).read_bytes() for name in module.NAMES}
        self.assertNotEqual(self.run_tool('--check').returncode, 0)
        result = self.run_tool('--backup', str(self.backup)); self.assertEqual(result.returncode, 0, result.stderr)
        for name, data in before.items():
            self.assertEqual((self.backup / (name + '.mat')).read_bytes(), data)
            self.assertEqual((self.materials / (name + '.mat')).read_bytes(), data.replace(b'    m_Colors:', b'      _SuppressOutline: 1\n    m_Colors:'))
        self.assertEqual((self.materials / 'normal.mat').read_bytes(), b'unchanged')
        self.assertEqual(self.run_tool('--check').returncode, 0)
        self.assertEqual(self.run_tool().returncode, 0)

    def test_invalid_second_material_rejected_without_changes(self):
        path = self.materials / (module.NAMES[1] + '.mat')
        path.write_bytes(path.read_bytes().replace(module.SHADER, b'  m_Shader: other'))
        first = self.materials / (module.NAMES[0] + '.mat'); original = first.read_bytes()
        self.assertNotEqual(self.run_tool('--backup', str(self.backup)).returncode, 0)
        self.assertEqual(first.read_bytes(), original); self.assertFalse(self.backup.exists())

    def test_existing_zero_is_updated_and_invalid_or_duplicate_values_fail(self):
        data = (self.materials / (module.NAMES[0] + '.mat')).read_bytes()
        zero = data.replace(b'    m_Colors:', b'      _SuppressOutline: 0\n    m_Colors:')
        self.assertIn(b'_SuppressOutline: 1', module.render(zero, module.NAMES[0]))
        for value in (b'2', b'0\n      _SuppressOutline: 1'):
            bad = data.replace(b'    m_Colors:', b'      _SuppressOutline: ' + value + b'\n    m_Colors:')
            with self.assertRaises(ValueError): module.render(bad, module.NAMES[0])


if __name__ == '__main__': unittest.main()
