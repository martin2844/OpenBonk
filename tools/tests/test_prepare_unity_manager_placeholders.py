import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('manager_scenes', Path(__file__).resolve().parents[1] / 'prepare-unity-manager-placeholders.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

GO = b'''%YAML 1.1
--- !u!1 &10
GameObject:
  m_Component:
  - component: {fileID: 20}
  - component: {fileID: 30}
  m_Name: AlwaysManagers
--- !u!4 &20
Transform:
  m_GameObject: {fileID: 10}
'''
EMPTY = b'''--- !u!114 &30
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 10}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 0}
  m_Name: 
  m_EditorClassIdentifier: 
'''

class ManagerPreparationTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'workspace'
        self.scenes = self.project / 'Assets/Scenes'
        self.scenes.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.backup = self.root / 'backup'
        for name in module.SCENES:
            (self.scenes / (name + '.unity')).write_bytes(GO + EMPTY)

    def invoke(self, *extra):
        with patch.object(sys, 'argv', ['prepare', '--project', str(self.project), *extra]), contextlib.redirect_stdout(io.StringIO()), contextlib.redirect_stderr(io.StringIO()):
            module.main()

    def test_removes_only_empty_placeholder_with_backups_and_is_idempotent(self):
        with self.assertRaises(SystemExit): self.invoke('--check')
        self.invoke('--backup', str(self.backup))
        for name in module.SCENES:
            path = self.scenes / (name + '.unity')
            self.assertEqual((self.backup / path.name).read_bytes(), GO + EMPTY)
            self.assertEqual(path.read_bytes(), GO.replace(b'  - component: {fileID: 30}\n', b''))
        module.verify(self.project)
        self.invoke('--check')
        self.invoke()

    def test_rejects_extra_fields_references_and_duplicate_ids(self):
        for data in (GO + EMPTY + b'  serializedValue: 1\n',
                     GO + EMPTY + b'--- !u!1 &40\nGameObject:\n  other: {fileID: 30}\n',
                     GO + EMPTY + b'--- !u!1 &30\nGameObject:\n'):
            with self.subTest(data=data), self.assertRaises(ValueError): module.render(data)

    def test_failure_in_one_scene_changes_no_scene(self):
        broken = self.scenes / 'GeneratedMap.unity'
        broken.write_bytes(GO + EMPTY + b'  serializedValue: 1\n')
        with self.assertRaises(ValueError): self.invoke('--backup', str(self.backup))
        self.assertFalse(self.backup.exists())
        self.assertEqual((self.scenes / 'MainMenu.unity').read_bytes(), GO + EMPTY)
        self.assertEqual(broken.read_bytes(), GO + EMPTY + b'  serializedValue: 1\n')

    def test_preserves_real_script_and_rejects_existing_backup(self):
        valid = (GO + EMPTY).replace(b'm_Script: {fileID: 0}', b'm_Script: {fileID: 11500000, guid: abc, type: 3}')
        self.assertEqual(module.render(valid), valid)
        self.backup.mkdir()
        with self.assertRaises(SystemExit): self.invoke('--backup', str(self.backup))
        self.assertEqual((self.scenes / 'MainMenu.unity').read_bytes(), GO + EMPTY)

    def test_accepts_unsaved_source_scene_empty_scalars(self):
        source = (GO + EMPTY).replace(b'  m_Name: \n', b'  m_Name:\n').replace(b'  m_EditorClassIdentifier: \n', b'  m_EditorClassIdentifier:\n')
        self.assertEqual(module.render(source), GO.replace(b'  - component: {fileID: 30}\n', b''))

if __name__ == '__main__': unittest.main()
