import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile


TOOLS = Path(__file__).resolve().parents[1]


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, TOOLS / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


CHECK = load('conversion_metadata_check', 'check-conversion-metadata.py')
PACKAGE = load('conversion_metadata_package', 'package-conversion-plans.py')


class ConversionMetadataTest(unittest.TestCase):
    def test_pinned_plans_and_modified_bytes(self):
        for name, (_, count, _, _) in CHECK.PLANS.items():
            data = (CHECK.DIRECTORY / name).read_bytes()
            self.assertEqual(CHECK.validate(name, data), count)
            modified = bytearray(data)
            modified[-1] ^= 1
            with self.assertRaises(ValueError):
                CHECK.validate(name, modified)

    def test_missing_metadata_uses_checked_in_copies(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-plans-test-') as tmp:
            root = Path(tmp)
            source = root / 'source'
            source.mkdir()
            for index in range(48):
                (source / f'fixture-{index:02}.bin').write_bytes(bytes([index]))
            output = root / 'plans.zip'
            PACKAGE.package(source, output)
            with zipfile.ZipFile(output) as archive:
                self.assertEqual(len(archive.namelist()), 50)
                for name in PACKAGE.METADATA_PINS:
                    self.assertEqual(archive.read(name), (PACKAGE.METADATA_DIR / name).read_bytes())
            with self.assertRaises(ValueError):
                PACKAGE.package(source, output)

    def test_rejects_bad_override_before_creating_archive(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-plans-test-') as tmp:
            root = Path(tmp)
            source = root / 'source'
            source.mkdir()
            (source / 'exact.bin').write_bytes(b'wrong')
            output = root / 'plans.zip'
            with self.assertRaises(ValueError):
                PACKAGE.package(source, output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
