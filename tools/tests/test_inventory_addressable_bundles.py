import importlib.util
import io
from pathlib import Path
from types import SimpleNamespace
import unittest
import zipfile

path = Path(__file__).resolve().parents[1] / 'inventory-addressable-bundles.py'
spec = importlib.util.spec_from_file_location('bundle_inventory', path)
inventory = importlib.util.module_from_spec(spec); spec.loader.exec_module(inventory)


def archive(entries):
    data = io.BytesIO()
    with zipfile.ZipFile(data, 'w') as z:
        for name, value in entries: z.writestr(name, value)
    data.seek(0)
    return zipfile.ZipFile(data)


class BundleInventoryTests(unittest.TestCase):
    def test_classifies_using_reference_even_if_template_is_masked(self):
        name = 'assets/aa/Android/example.bundle'
        script = SimpleNamespace(type=SimpleNamespace(name='MonoScript'),
            read=lambda: SimpleNamespace(m_AssemblyName='Unity.Localization', m_Namespace='Tables', m_ClassName='StringTable'))
        texture = SimpleNamespace(type=SimpleNamespace(name='Texture2D'))
        with archive([(name, b'ABCD')]) as reference, archive([(name, b'A\0C\0')]) as template:
            result = inventory.inventory(reference, template, load=lambda raw: SimpleNamespace(objects=[script, texture]))
        self.assertEqual(result['bundle_count'], 1)
        self.assertEqual(result['retained_nonzero_bytes'], 2)
        self.assertEqual(result['bundles'][0]['object_types'], {'MonoScript': 1, 'Texture2D': 1})
        self.assertEqual(result['bundles'][0]['scripts'][0][2], 'StringTable')
        self.assertFalse(result['problems'])

    def test_reports_changed_missing_and_unreadable_bundles(self):
        first, second, third = ('assets/aa/Android/' + x + '.bundle' for x in ('a', 'b', 'c'))
        with archive([(first, b'AB'), (second, b'AB')]) as reference, \
                archive([(first, b'AX'), (second, b'AB'), (third, b'AB')]) as template:
            result = inventory.inventory(reference, template, load=lambda raw: (_ for _ in ()).throw(ValueError('bad bundle')))
        self.assertEqual(result['bundle_count'], 0)
        self.assertEqual(len(result['problems']), 3)

    def test_rejects_template_without_bundles(self):
        with archive([]) as reference, archive([]) as template, self.assertRaises(ValueError):
            inventory.inventory(reference, template)


if __name__ == '__main__': unittest.main()
