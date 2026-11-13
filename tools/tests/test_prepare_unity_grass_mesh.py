import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('prepare_grass', Path(__file__).resolve().parents[1] / 'prepare-unity-grass-mesh.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class GrassPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'workspace'
        (self.project / 'Assets/Mesh').mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.mesh = self.project / module.MESH; self.backup = self.root / 'mesh-backup.asset'
        self.original = b'Mesh:\n  m_Name: GrassQuads\n  m_IsReadable: 0\n  m_IndexBuffer: 001122\n  m_VertexData:\n    m_VertexCount: 8\n'
        self.mesh.write_bytes(self.original)

    def invoke(self, *extra):
        with patch.object(sys, 'argv', ['prepare', '--project', str(self.project), *extra]), \
                contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()): module.main()

    def test_prepares_and_backs_up_only_the_readability_flag(self):
        with self.assertRaises(SystemExit): self.invoke('--check')
        self.invoke('--backup', str(self.backup))
        self.assertEqual(self.backup.read_bytes(), self.original)
        self.assertEqual(self.mesh.read_bytes(), self.original.replace(b'm_IsReadable: 0', b'm_IsReadable: 1'))
        module.verify(self.project); self.invoke('--check'); self.invoke()

    def test_rejects_unknown_layout_and_existing_backup(self):
        self.mesh.write_bytes(self.original.replace(b'm_VertexCount: 8', b'm_VertexCount: 9'))
        with self.assertRaises(ValueError): self.invoke('--backup', str(self.backup))
        self.assertFalse(self.backup.exists())
        self.mesh.write_bytes(self.original); self.backup.write_bytes(b'keep')
        with self.assertRaises(SystemExit): self.invoke('--backup', str(self.backup))
        self.assertEqual(self.backup.read_bytes(), b'keep'); self.assertEqual(self.mesh.read_bytes(), self.original)


if __name__ == '__main__': unittest.main()
