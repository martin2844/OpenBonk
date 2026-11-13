import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('repair_localization', Path(__file__).resolve().parents[1] / 'repair-localization-guids.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class LocalizationGuidRepairTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name); self.project = root / 'project'
        for name in ('Assets/Localization/String Tables', 'Packages', 'ProjectSettings'):
            (self.project / name).mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.old, self.new = 'a' * 32, 'b' * 32
        self.meta = self.project / 'Assets/Localization/String Tables/Menu Shared Data.asset.meta'
        self.meta.write_text('fileFormatVersion: 2\nguid: ' + self.old + '\n')
        self.scene = self.project / 'Assets/scene.unity'
        self.scene.write_text('m_SharedData: {guid: ' + self.old + ', type: 2}\n')
        self.export = root / 'export.json'
        self.export.write_text(json.dumps({'tables': [{'collection': 'Menu', 'collectionGuid': self.new}]}))
        self.backup = root / 'backup'

    def test_repairs_meta_and_references_with_backup(self):
        self.assertEqual(module.repair(self.project, self.export, self.backup), 1)
        self.assertIn(self.new, self.meta.read_text()); self.assertIn(self.new, self.scene.read_text())
        self.assertIn(self.old, (self.backup / self.meta.relative_to(self.project)).read_text())
        self.assertIn(self.old, (self.backup / self.scene.relative_to(self.project)).read_text())
        self.assertEqual(module.repair(self.project, self.export, self.backup.parent / 'unused'), 0)

    def test_rejects_collision_without_writing(self):
        (self.project / 'Assets/other.meta').write_text('guid: ' + self.new + '\n')
        with self.assertRaises(ValueError): module.repair(self.project, self.export, self.backup)
        self.assertIn(self.old, self.meta.read_text()); self.assertFalse(self.backup.exists())

    def test_rejects_binary_guid_reference_without_writing(self):
        (self.project / 'Assets/binary.asset').write_bytes(b'\0' + self.old.encode())
        with self.assertRaises(ValueError): module.repair(self.project, self.export, self.backup)
        self.assertIn(self.old, self.meta.read_text()); self.assertFalse(self.backup.exists())


if __name__ == '__main__': unittest.main()
