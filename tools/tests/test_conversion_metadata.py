import importlib.util
import hashlib
from pathlib import Path
import tempfile
import unittest
import zipfile
from unittest.mock import patch


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
            for name in PACKAGE.EXPECTED_NAMES - set(PACKAGE.METADATA_PINS) - set(PACKAGE.SHADER_PINS):
                (source / name).write_bytes(b'fixture')
            shader_pins = {}
            for name in PACKAGE.SHADER_PINS:
                data = name.encode()
                (source / name).write_bytes(data)
                shader_pins[name] = hashlib.sha256(data).hexdigest()
            output = root / 'plans.zip'
            with patch.object(PACKAGE, 'SHADER_PINS', shader_pins):
                PACKAGE.package(source, output)
            with zipfile.ZipFile(output) as archive:
                self.assertEqual(len(archive.namelist()), 50)
                for name in PACKAGE.METADATA_PINS:
                    self.assertEqual(archive.read(name), (PACKAGE.METADATA_DIR / name).read_bytes())
            with self.assertRaises(ValueError):
                PACKAGE.package(source, output)

    def test_equal_count_with_wrong_name_is_rejected(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-plans-test-') as tmp:
            source = Path(tmp)
            for name in PACKAGE.EXPECTED_NAMES - set(PACKAGE.METADATA_PINS) - set(PACKAGE.SHADER_PINS):
                (source / name).write_bytes(b'fixture')
            shaders = {}
            for name in PACKAGE.SHADER_PINS:
                (source / name).write_bytes(name.encode())
                shaders[name] = hashlib.sha256(name.encode()).hexdigest()
            (source / 'map.bin').rename(source / 'wrong.bin')
            with patch.object(PACKAGE, 'SHADER_PINS', shaders):
                with self.assertRaisesRegex(ValueError, 'names differ'):
                    PACKAGE.inputs(source)

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
