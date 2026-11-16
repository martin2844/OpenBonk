import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest
import warnings
import zipfile

spec = importlib.util.spec_from_file_location('compare_apk', Path(__file__).resolve().parents[1] / 'compare-apk-executable-sections.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


def elf(text=b'ABCD', address=0x1000):
    names = b'\0.shstrtab\0.text\0'
    data = bytearray(512)
    data[:7] = b'\x7fELF\x02\x01\x01'
    struct.pack_into('<HHI', data, 16, 3, 183, 1)
    struct.pack_into('<Q', data, 0x28, 64)
    struct.pack_into('<HHH', data, 0x3a, 64, 3, 1)
    struct.pack_into('<IIQQQQIIQQ', data, 64 + 64, 1, 3, 0, 0, 256, len(names), 0, 0, 1, 0)
    struct.pack_into('<IIQQQQIIQQ', data, 64 + 128, 11, 1, 4, address, 300, len(text), 0, 0, 4, 0)
    data[256:256 + len(names)] = names
    data[300:300 + len(text)] = text
    return bytes(data)


class CompareApkExecutableSectionsTests(unittest.TestCase):
    def test_code_and_virtual_address_are_compared(self):
        first = module.executable_sections(elf())
        self.assertEqual(first, module.executable_sections(elf()))
        self.assertNotEqual(first, module.executable_sections(elf(b'ABCE')))
        self.assertNotEqual(first, module.executable_sections(elf(address=0x2000)))
        self.assertEqual(first['.text']['size'], 4)

    def test_invalid_header_and_bounds_are_rejected(self):
        with self.assertRaises(ValueError): module.executable_sections(b'not an elf')
        bad = bytearray(elf()); struct.pack_into('<H', bad, 18, 62)
        with self.assertRaises(ValueError): module.executable_sections(bad)
        bad = bytearray(elf()); struct.pack_into('<Q', bad, 0x28, len(bad))
        with self.assertRaises(ValueError): module.executable_sections(bad)
        bad = bytearray(elf()); struct.pack_into('<Q', bad, 64 + 128 + 24, len(bad))
        with self.assertRaises(ValueError): module.executable_sections(bad)

    def test_duplicate_library_entries_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'test.apk'
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                with zipfile.ZipFile(path, 'w') as apk:
                    apk.writestr(module.LIBRARY, elf()); apk.writestr(module.LIBRARY, elf())
            with self.assertRaises(ValueError): module.apk_sections(path)


if __name__ == '__main__': unittest.main()
