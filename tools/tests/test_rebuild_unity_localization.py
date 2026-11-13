import contextlib
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'rebuild-unity-localization.py'
spec = importlib.util.spec_from_file_location('rebuild_unity_localization', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class LocalizationPreparationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.project = self.root / 'workspace'
        (self.project / 'ProjectSettings').mkdir(parents=True)
        (self.project / 'Assets/StreamingAssets/aa/StandaloneLinux64').mkdir(parents=True)
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        (self.project / 'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
        self.editor = self.root / 'Unity'
        self.editor.write_text('#!/bin/sh\nexit 0\n')
        self.editor.chmod(0o755)
        self.output = self.root / 'generated'
        self.bundle_dir = self.project / 'Assets/StreamingAssets/aa/StandaloneLinux64'
        self.manifest_path = self.root / 'steam-linux-locale-inputs.json'
        self.fake_script = self.root / 'rebuild-unity-localization.py'
        self.fake_script.touch()
        b1, b2 = b'local test bundle', b'local test locales'
        (self.bundle_dir / 'strings.bundle').write_bytes(b1)
        (self.bundle_dir / 'locales.bundle').write_bytes(b2)
        self.manifest_path.write_text(json.dumps({'strings.bundle': {'size': len(b1), 'sha256': hashlib.sha256(b1).hexdigest()}}))
        self.locales = ('locales.bundle', len(b2), hashlib.sha256(b2).hexdigest())
        self.argv = ['program', '--project', str(self.project), '--unity', str(self.editor), '--build-root', str(self.output)]

    def invoke(self, extra=()):
        with patch.object(module, '__file__', str(self.fake_script)), patch.object(module, 'LOCALES', self.locales), patch.object(sys, 'argv', self.argv + list(extra)):
            return module.main()

    def test_check_and_reject_wrong_input_without_writes(self):
        self.invoke(['--check'])
        self.assertFalse(self.output.exists())
        (self.bundle_dir / 'strings.bundle').write_bytes(b'wrong')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse(self.output.exists())

    def test_repairs_guids_and_checks_localized_lookup(self):
        methods = []
        def run(cmd, env):
            method = cmd[cmd.index('-executeMethod') + 1]
            methods.append(method)
            self.assertEqual((self.project / 'Assets/csc.rsp').read_bytes(), module.DEFINES)
            (self.project / 'Assets/csc.rsp.meta').write_text('generated')
            if method.startswith('LocalizationBundleExport'):
                path = self.output / 'android/localization-content.json'
                path.parent.mkdir()
                path.write_text('{"locales":[1],"tables":[1]}')
            elif method == 'LocalizationGuidPortCheck.Run':
                Path(cmd[cmd.index('-logFile') + 1]).write_text('LocalizationGuidPortCheck passed: test')
            else:
                Path(cmd[cmd.index('-logFile') + 1]).write_text('Android Addressables built.')
            return type('Result', (), {'returncode': 0})()
        with patch.object(module.subprocess, 'run', side_effect=run), patch.object(module.repair_guids, 'repair') as repair:
            self.invoke()
        self.assertEqual(methods, ['LocalizationBundleExport.ExportLinuxLocalizationBundles', 'RebuildAndroidLocalization.Build',
                                   'RestoreLocalizationCollections.Run', 'RebuildAndroidLocalization.Build', 'LocalizationGuidPortCheck.Run'])
        repair.assert_called_once_with(self.project, self.output / 'android/localization-content.json', self.output / 'guid-backup')
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())
        self.assertFalse((self.project / 'Assets/csc.rsp.meta').exists())

    def test_failure_keeps_output_and_cleans_response_file(self):
        with patch.object(module.subprocess, 'run', return_value=type('Result', (), {'returncode': 1})()):
            with self.assertRaises(SystemExit):
                self.invoke()
        self.assertTrue(self.output.is_dir())
        self.assertFalse((self.project / 'Assets/csc.rsp').exists())


if __name__ == '__main__':
    unittest.main()
