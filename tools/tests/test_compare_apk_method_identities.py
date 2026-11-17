import importlib.util
from pathlib import Path
import struct
import tempfile
import unittest
import warnings
import zipfile

spec = importlib.util.spec_from_file_location('compare_methods', Path(__file__).resolve().parents[1] / 'compare-apk-method-identities.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


def metadata(order=('Move', 'Jump')):
    data = bytearray(600); strings = b'\0Game.dll\0Game\0Fox\0Move\0Jump\0value\0'
    struct.pack_into('<II', data, 0, 0xFAB11BAF, 29)
    for header, offset, size in ((24, 256, len(strings)), (48, 448, 64), (88, 512, 12), (160, 320, 88), (168, 408, 40)):
        struct.pack_into('<II', data, header, offset, size)
    data[256:256 + len(strings)] = strings
    struct.pack_into('<II', data, 320, strings.index(b'Fox'), strings.index(b'Game\0'))
    struct.pack_into('<IIII', data, 408, strings.index(b'Game.dll'), 0, 0, 1)
    for i, name in enumerate(order):
        token = {'Move': 1, 'Jump': 2}[name]
        struct.pack_into('<IiiiiIHHHH', data, 448 + 32 * i, strings.index(name.encode()), 0, 2, 0, -1, token, 1, 2, 3, int(name == 'Move'))
    struct.pack_into('<IIi', data, 512, strings.index(b'value'), 3, 7)
    return bytes(data)


class CompareMethodIdentityTests(unittest.TestCase):
    def test_method_order_can_change_without_changing_identity(self):
        first = module.method_identities(metadata())
        reordered = module.method_identities(metadata(('Jump', 'Move')))
        self.assertNotEqual(first, reordered)
        self.assertCountEqual(first, reordered)
        changed = bytearray(metadata()); struct.pack_into('<I', changed, 448 + 20, 99)
        self.assertNotEqual(first, module.method_identities(changed))
        first_signature = module.method_signatures(metadata())
        self.assertCountEqual(first_signature, module.method_signatures(metadata(('Jump', 'Move'))))
        changed = bytearray(metadata()); struct.pack_into('<i', changed, 448 + 8, 99)
        self.assertNotEqual(first_signature, module.method_signatures(changed))
        changed = bytearray(metadata()); struct.pack_into('<i', changed, 512 + 8, 99)
        self.assertNotEqual(first_signature, module.method_signatures(changed))

    def test_invalid_metadata_ranges_are_rejected(self):
        with self.assertRaises(ValueError): module.method_identities(b'bad')
        bad = bytearray(metadata()); struct.pack_into('<I', bad, 48, len(bad))
        with self.assertRaises(ValueError): module.method_identities(bad)
        bad = bytearray(metadata()); struct.pack_into('<I', bad, 408 + 12, 2)
        with self.assertRaises(ValueError): module.method_identities(bad)
        bad = bytearray(metadata()); struct.pack_into('<i', bad, 448 + 4, -1)
        with self.assertRaises(ValueError): module.method_identities(bad)
        bad = bytearray(metadata()); struct.pack_into('<i', bad, 448 + 12, 2)
        with self.assertRaises(ValueError): module.method_signatures(bad)

    def test_method_shapes_ignore_indices_but_preserve_arity_and_multiplicity(self):
        original = module.method_signatures(metadata())
        changed = bytearray(metadata()); struct.pack_into('<i', changed, 448 + 8, 99)
        self.assertEqual(module.method_shapes(original), module.method_shapes(module.method_signatures(bytes(changed))))
        changed = bytearray(metadata()); struct.pack_into('<H', changed, 448 + 30, 0)
        self.assertNotEqual(module.method_shapes(original), module.method_shapes(module.method_signatures(bytes(changed))))
        self.assertNotEqual(module.method_shapes(original), module.method_shapes(original + [original[0]]))
        owners = module.unmatched_owners(module.method_shapes(original + [original[0]]), module.method_shapes(original))
        self.assertEqual(owners, [{'image': 'Game.dll', 'namespace': 'Game', 'type': 'Fox', 'count': 1}])

    def test_duplicate_metadata_entries_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'test.apk'
            with warnings.catch_warnings():
                warnings.simplefilter('ignore', UserWarning)
                with zipfile.ZipFile(path, 'w') as apk:
                    apk.writestr(module.ENTRY, metadata()); apk.writestr(module.ENTRY, metadata())
            with self.assertRaises(ValueError): module.apk_methods(path)


if __name__ == '__main__': unittest.main()
