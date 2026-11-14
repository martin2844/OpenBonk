import importlib.util
import io
from pathlib import Path
import stat
import unittest
import warnings
import zipfile


spec = importlib.util.spec_from_file_location('dual_check', Path(__file__).resolve().parents[1] / 'check-dual-research-launcher.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class DiagnosticAssetTests(unittest.TestCase):
    def archive(self, extra=()):
        data = io.BytesIO()
        with zipfile.ZipFile(data, 'w') as z, warnings.catch_warnings():
            warnings.simplefilter('ignore', UserWarning)
            for name in ('dual-platform', 'generated-audio-mode', 'windows/generated-audio-mode'):
                z.writestr(module.PREFIX + name, b'x')
            for name, body in extra: z.writestr(name, body)
        data.seek(0)
        return zipfile.ZipFile(data)

    def test_valid_dual_assets(self):
        with self.archive() as z: self.assertEqual(len(module.diagnostic_entries(z)), 3)

    def test_traversal_and_duplicate_assets_rejected(self):
        for name in ('../escape', 'windows/../../escape', 'windows\\escape', 'dual-platform'):
            with self.subTest(name=name), self.archive([(module.PREFIX + name, b'x')]) as z:
                with self.assertRaises(ValueError): module.diagnostic_entries(z)

    def test_symbolic_link_rejected(self):
        info = zipfile.ZipInfo(module.PREFIX + 'link')
        info.create_system = 3
        info.external_attr = (stat.S_IFLNK | 0o777) << 16
        with self.archive([(info, b'target')]) as z:
            with self.assertRaises(ValueError): module.diagnostic_entries(z)

    def test_non_dual_archive_rejected(self):
        data = io.BytesIO()
        with zipfile.ZipFile(data, 'w') as z: z.writestr(module.PREFIX + 'marker', b'x')
        data.seek(0)
        with zipfile.ZipFile(data) as z:
            with self.assertRaises(ValueError): module.diagnostic_entries(z)


if __name__ == '__main__': unittest.main()
