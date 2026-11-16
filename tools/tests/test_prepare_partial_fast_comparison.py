import hashlib
import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('partial_fast_comparison', Path(__file__).resolve().parents[1] / 'prepare-partial-fast-comparison.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PreparePartialFastComparisonTests(unittest.TestCase):
    def test_fixture_format_and_existing_file_are_preserved(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'fixtures'
            module.write_fixtures(path, [(4, 4, 10, 45, b'input', b'output')])
            expected = b'OBTX0001' + struct.pack('>I5I', 1, 4, 4, 10, 45, 5) + hashlib.sha256(b'output').digest() + b'input'
            self.assertEqual(path.read_bytes(), expected)
            with self.assertRaises(FileExistsError):
                module.write_fixtures(path, [(4, 4, 10, 45, b'x', b'y')])
            self.assertEqual(path.read_bytes(), expected)

    def test_invalid_record_does_not_leave_partial_output(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'fixtures'
            with self.assertRaises(ValueError):
                module.write_fixtures(path, [(4, 4, 10, 45, b'input', b'')])
            self.assertFalse(path.exists())


if __name__ == '__main__': unittest.main()
