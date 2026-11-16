import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'generate-windows-localization-inventory.py'
spec = importlib.util.spec_from_file_location('generate_windows_localization_inventory', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class LocalizationInventoryTest(unittest.TestCase):
    def test_generates_offsets_and_rejects_changed_inputs(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            linux, windows = root / 'linux', root / 'windows'
            inventory = {'linux': {}, 'windows': {}}
            names = [f'localization-test-{i:02}.bundle' for i in range(17)] + [module.THAI]
            for platform, folder, directory in (('linux', 'StandaloneLinux64', linux),
                                                ('windows', 'StandaloneWindows64', windows)):
                prefix = f'StreamingAssets/aa/{folder}/'
                (directory / prefix).mkdir(parents=True)
                for name in names:
                    payload = (b'linux' if platform == 'linux' else b'win') if name == module.THAI else (
                        b'abXde' if platform == 'linux' else b'abYde')
                    (directory / prefix / name).write_bytes(payload)
                    inventory[platform][prefix + name] = {
                        'size': len(payload), 'sha256': hashlib.sha256(payload).hexdigest()}
            result = module.build_inventory(inventory, linux, windows, 'test-patch-hash')
            self.assertEqual(len(result['bundles']), 18)
            rows = {item['name']: item for item in result['bundles']}
            self.assertEqual(rows[names[0]]['offset'], 2)
            self.assertEqual(rows[module.THAI]['patch_sha256'], 'test-patch-hash')
            (linux / 'StreamingAssets/aa/StandaloneLinux64' / names[0]).write_bytes(b'abZde')
            with self.assertRaisesRegex(ValueError, 'unsupported localization input'):
                module.build_inventory(inventory, linux, windows, 'test-patch-hash')


if __name__ == '__main__':
    unittest.main()
