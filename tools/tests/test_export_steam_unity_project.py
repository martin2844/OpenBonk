import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('export_steam_unity_project', Path(__file__).resolve().parents[1] / 'export-steam-unity-project.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ExportPreflightTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.game = root / 'game'
        (self.game / 'Megabonk_Data').mkdir(parents=True)
        (self.game / 'Megabonk_Data/globalgamemanagers.assets').touch()
        self.binary = root / 'AssetRipper'
        self.binary.write_text('#!/bin/sh\nprintf "AssetRipper.GUI.Web 2.0.0+test\\n"\n')
        self.binary.chmod(0o755)
        self.output = root / 'export'
        self.log = root / 'export.assetripper.log'
        self.argv = ['program', '--game', str(self.game), '--assetripper', str(self.binary), '--output', str(self.output), '--check']

    def invoke(self):
        with patch.object(sys, 'argv', self.argv):
            module.main()

    def test_check_does_not_create_output(self):
        self.invoke()
        self.assertFalse(self.output.exists())
        self.assertFalse(self.log.exists())

    def test_existing_sibling_log_is_protected(self):
        self.log.write_text('preserve')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertEqual(self.log.read_text(), 'preserve')
        self.assertFalse(self.output.exists())

    def test_rejects_wrong_exporter_version(self):
        self.binary.write_text('#!/bin/sh\nprintf "AssetRipper.GUI.Web 1.3.14\\n"\n')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
