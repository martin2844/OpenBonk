import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-texture-imports.py'

class TexturePreparationTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'workspace'
        for directory in ('Assets/Editor', 'Assets/Texture2D', 'Assets/Plugins/Android/a.androidlib/res', 'ProjectSettings', 'Library/PackageCache'):
            (self.project / directory).mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps({'schema': 1, 'kind': 'openbonk-local-unity-workspace'}))
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
        (self.project / 'Assets/Editor/PreserveItemIconPixels.cs').write_text('fixture')
        self.texture = self.project / 'Assets/Texture2D/item.png'; self.texture.write_bytes(b'png')
        self.meta = Path(str(self.texture) + '.meta'); self.meta.write_bytes(b'original meta')
        (self.project / 'Assets/Plugins/Android/a.androidlib/res/icon.png').write_bytes(b'android image without Unity meta')
        self.editor = self.root / 'Unity'; self.log = self.root / 'editor.log'; self.backup = self.root / 'backup'
        self.editor.write_text('#!' + sys.executable + '\nimport sys\nfrom pathlib import Path\n'
            'project=Path(sys.argv[sys.argv.index("-projectPath")+1])\n'
            'assert sys.argv[sys.argv.index("-executeMethod")+1] == "PreserveItemIconPixels.Apply"\n'
            '(project / "Assets/Texture2D/item.png.meta").write_bytes(b"changed meta")\n'
            'Path(sys.argv[sys.argv.index("-logFile")+1]).write_text("PreserveItemIconPixels: checked 261 content icons; updated 1 Android RGBA32 import overrides")\n')
        self.editor.chmod(0o700)

    def invoke(self, *extra):
        return subprocess.run([sys.executable, str(SCRIPT), '--project', str(self.project), '--unity', str(self.editor),
                               '--log', str(self.log), '--backup', str(self.backup), *extra], capture_output=True, text=True)

    def test_backs_up_before_editor_changes_and_cleans_response_file(self):
        check = self.invoke('--check'); self.assertEqual(check.returncode, 0, check.stderr); self.assertFalse(self.backup.exists())
        result = self.invoke(); self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual((self.backup / 'Texture2D/item.png.meta').read_bytes(), b'original meta')
        self.assertEqual(self.meta.read_bytes(), b'changed meta')
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())
        self.assertNotEqual(self.invoke().returncode, 0)

    def test_rejects_missing_metadata_before_changes(self):
        self.meta.unlink(); result = self.invoke()
        self.assertNotEqual(result.returncode, 0); self.assertFalse(self.backup.exists()); self.assertFalse(self.log.exists())

    def test_failed_editor_retains_backup_and_cleans_response_file(self):
        self.editor.write_text('#!' + sys.executable + '\nimport sys\nsys.exit(7)\n'); self.editor.chmod(0o700)
        result = self.invoke(); self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.backup / 'Texture2D/item.png.meta').read_bytes(), b'original meta')
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

    def test_success_without_confirmation_is_rejected(self):
        self.editor.write_text('#!' + sys.executable + '\nimport sys\nfrom pathlib import Path\nPath(sys.argv[sys.argv.index("-logFile")+1]).write_text("no confirmation")\n'); self.editor.chmod(0o700)
        result = self.invoke(); self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.backup / 'Texture2D/item.png.meta').read_bytes(), b'original meta')
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())

if __name__ == '__main__': unittest.main()
