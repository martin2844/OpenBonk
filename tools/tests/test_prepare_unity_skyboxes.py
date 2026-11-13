import contextlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('prepare_skyboxes', Path(__file__).resolve().parents[1] / 'prepare-unity-skyboxes.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class SkyboxPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.project = root / 'workspace'
        self.materials = self.project / 'Assets/Material'
        self.shaders = self.project / 'Assets/Shader'
        self.materials.mkdir(parents=True)
        self.shaders.mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.source, self.target = 'a' * 32, 'b' * 32
        for name, guid in (('Skybox_Cubemap Extended.shader', self.source), ('Native_Cubemap_Skybox.shader', self.target)):
            (self.shaders / name).write_text('shader')
            (self.shaders / (name + '.meta')).write_text('guid: ' + guid + '\n')
        self.original = '  m_Shader: {fileID: 4800000, guid: ' + self.source + ', type: 3}\n  setting: retained\n'
        for name in module.NAMES:
            path = self.materials / ('Skybox_' + name + '.mat')
            path.write_text(self.original)
            Path(str(path) + '.meta').write_text('metadata')
        self.backup = root / 'backup'
        self.argv = ['program', '--project', str(self.project), '--backup', str(self.backup)]

    def invoke(self, extra=()):
        with patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_restore(self):
        self.invoke(['--check'])
        self.assertFalse(self.backup.exists())
        self.invoke()
        for name in module.NAMES:
            path = self.materials / ('Skybox_' + name + '.mat')
            self.assertEqual(path.read_text(), self.original.replace(self.source, self.target))
            self.assertEqual((self.backup / path.relative_to(self.project)).read_text(), self.original)
        self.assertEqual(len(json.loads((self.backup / 'manifest.json').read_text())['files']), 16)

    def test_unexpected_shader_rejected_without_writes(self):
        (self.materials / 'Skybox_Hell.mat').write_text(self.original.replace(self.source, 'c' * 32))
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse(self.backup.exists())
        self.assertEqual((self.materials / 'Skybox_Forest1.mat').read_text(), self.original)

    def test_write_failure_rolls_back(self):
        original_write = Path.write_bytes
        def fail(path, data):
            if path == self.materials / 'Skybox_Forest1.mat':
                raise OSError('simulated failure')
            return original_write(path, data)
        with patch.object(Path, 'write_bytes', fail), self.assertRaises(OSError):
            self.invoke()
        for name in module.NAMES:
            self.assertEqual((self.materials / ('Skybox_' + name + '.mat')).read_text(), self.original)
        self.assertTrue(self.backup.is_dir())


if __name__ == '__main__':
    unittest.main()
