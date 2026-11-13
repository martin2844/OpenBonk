"""Exercise device selection and bootstrap handoff without touching Android."""
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import types
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'tools' / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


installer = load('installer_test', 'install-personal-adb.py')
bootstrap = load('bootstrap_test', 'bootstrap-personal.py')


class DeviceSelection(unittest.TestCase):
    def test_previous_package_detection(self):
        with patch.object(installer, 'command', return_value='package:com.megabonk.nativeport.splittest'):
            self.assertTrue(installer.has_previous_install('adb', 'a'))
        with patch.object(installer, 'command', return_value='package:com.megabonk.nativeport.splittest.other'):
            self.assertFalse(installer.has_previous_install('adb', 'a'))
        with patch.object(installer, 'command', return_value='unexpected error'):
            with self.assertRaises(ValueError): installer.has_previous_install('adb', 'a')

    def test_selection_requires_authorized_booted_device(self):
        with patch.object(installer.subprocess, 'run') as run:
            run.return_value.stdout = 'List of devices attached\na\tdevice\nb\tunauthorized\n'
            with patch.object(installer, 'command', return_value='1'):
                self.assertEqual(installer.select_device('adb'), 'a')
                with self.assertRaises(ValueError): installer.select_device('adb', 'b')
            with patch.object(installer, 'command', return_value='0'):
                with self.assertRaises(ValueError): installer.select_device('adb', 'a')
            run.return_value.stdout = 'List of devices attached\na\tdevice\nc\tdevice\n'
            with self.assertRaises(ValueError): installer.select_device('adb')


class BootstrapHandoff(unittest.TestCase):
    def test_installs_only_after_success_and_keeps_receipt_on_failure(self):
        for fail_install in (False, True):
            with self.subTest(fail_install=fail_install), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp); source = root/'steam'; source.mkdir()
                work = root/'work'; commands = []
                prep = types.SimpleNamespace(check_steam_directory=lambda p: None)
                device = types.SimpleNamespace(select_device=lambda adb, serial: 'test-device', has_previous_install=lambda adb, serial: False)
                def fake_load(name, path):
                    return device if name == 'bootstrap_installer' else prep
                def fake_run(args, **kwargs):
                    commands.append(args)
                    if str(args[1]).endswith('install-personal-adb.py'):
                        self.assertTrue((work/'BOOTSTRAP_READY.json').is_file())
                        if fail_install: raise subprocess.CalledProcessError(1, args)
                    return types.SimpleNamespace(returncode=0)
                argv = ['bootstrap-personal.py', '--steam-data', str(source), '--work-dir', str(work), '--install']
                with patch.object(sys, 'argv', argv), patch.object(bootstrap, 'load', side_effect=fake_load), \
                     patch.object(bootstrap, 'verify_local_inputs', return_value=root), \
                     patch.object(bootstrap, 'java_home', return_value=root), \
                     patch.object(bootstrap.shutil, 'which', return_value='/fake/adb'), \
                     patch.object(bootstrap.subprocess, 'run', side_effect=fake_run):
                    if fail_install:
                        with self.assertRaises(SystemExit): bootstrap.main()
                    else: bootstrap.main()
                calls = [c for c in commands if str(c[1]).endswith('install-personal-adb.py')]
                self.assertEqual(len(calls), 1)
                self.assertEqual(calls[0][-2:], ['--serial', 'test-device'])
                self.assertTrue((work/'BOOTSTRAP_READY.json').is_file())

    def test_existing_install_without_key_stops_before_creating_work(self):
        with tempfile.TemporaryDirectory() as tmp:
            work = Path(tmp)/'work'
            prep = types.SimpleNamespace()
            device = types.SimpleNamespace(select_device=lambda adb, serial: 'test-device', has_previous_install=lambda adb, serial: True)
            def fake_load(name, path): return device if name == 'bootstrap_installer' else prep
            argv = ['bootstrap-personal.py', '--work-dir', str(work), '--install']
            with patch.object(sys, 'argv', argv), patch.object(bootstrap, 'load', side_effect=fake_load), \
                 patch.object(bootstrap.shutil, 'which', return_value='/fake/adb'):
                with self.assertRaises(SystemExit): bootstrap.main()
            self.assertFalse(work.exists())


if __name__ == '__main__': unittest.main()
