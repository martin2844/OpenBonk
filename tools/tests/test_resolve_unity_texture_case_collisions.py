import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('resolve_texture_case', Path(__file__).resolve().parents[1] / 'resolve-unity-texture-case-collisions.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CollisionTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        self.textures = self.project / 'Assets/Texture2D'
        self.textures.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        for name, guid in (('Bush.png', 'a' * 32), ('bush.png', 'b' * 32)):
            (self.textures / name).write_bytes(name.encode())
            (self.textures / (name + '.meta')).write_text('guid: ' + guid + '\n')
        self.manifest = root / 'renames.json'
        self.argv = ['program', '--project', str(self.project), '--manifest', str(self.manifest)]

    def invoke(self, extra=()):
        with patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_rename_preserve_metadata(self):
        self.invoke(['--check'])
        self.assertFalse(self.manifest.exists())
        self.invoke()
        target = self.textures / 'bush__openbonk_bbbbbbbb.png'
        self.assertEqual(target.read_bytes(), b'bush.png')
        self.assertEqual(Path(str(target) + '.meta').read_text(), 'guid: ' + 'b' * 32 + '\n')
        self.assertTrue((self.textures / 'Bush.png').is_file())
        self.assertFalse((self.textures / 'bush.png').exists())
        self.assertEqual(len(json.loads(self.manifest.read_text())['renames']), 1)

    def test_missing_metadata_does_not_move_files(self):
        (self.textures / 'bush.png.meta').unlink()
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertTrue((self.textures / 'bush.png').is_file())
        self.assertFalse(self.manifest.exists())

    def test_failed_metadata_rename_rolls_back_texture(self):
        original = Path.rename
        def fail(path, target):
            if path == self.textures / 'bush.png.meta':
                raise OSError('simulated failure')
            return original(path, target)
        with patch.object(Path, 'rename', fail), self.assertRaises(OSError):
            self.invoke()
        self.assertTrue((self.textures / 'bush.png').is_file())
        self.assertTrue((self.textures / 'bush.png.meta').is_file())


if __name__ == '__main__':
    unittest.main()
