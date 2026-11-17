import importlib.util
import io
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

path = Path(__file__).resolve().parents[1] / 'inventory-apk-unity-data.py'
spec = importlib.util.spec_from_file_location('apk_unity_inventory', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


def archive(entries):
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, 'w') as out:
        for name, data in entries: out.writestr('assets/bin/Data/' + name, data)
    stream.seek(0)
    return zipfile.ZipFile(stream)


class InventoryTests(unittest.TestCase):
    def test_splits_classifies_media_and_preserves_other_entries(self):
        obj = SimpleNamespace(type=SimpleNamespace(name='Texture2D'), path_id=9, byte_size=4,
                              read=lambda: SimpleNamespace(m_Name='sample'))
        with archive([('globalgamemanagers.assets.split1', b'cd'), ('globalgamemanagers.assets.split0', b'ab'),
                      ('Resources/unity default resources', b'opaque')]) as data, \
             patch.object(module.UnityPy, 'load', return_value=SimpleNamespace(objects=[obj])) as load:
            report = module.inventory(data)
        load.assert_called_once_with(b'abcd')
        self.assertEqual(report['serialized_groups'][0]['media'][0]['name'], 'sample')
        self.assertEqual(report['other_data_entries'][0]['name'], 'Resources/unity default resources')
        self.assertEqual(report['problems'], [])

    def test_rejects_missing_or_conflicting_splits(self):
        with archive([('level0.split1', b'x')]) as data:
            with self.assertRaisesRegex(ValueError, 'invalid'): module.group_entries(data)
        with archive([('level0', b'x'), ('level0.split0', b'y')]) as data:
            with self.assertRaisesRegex(ValueError, 'invalid'): module.group_entries(data)

    def test_parse_error_is_reported(self):
        with archive([('level0', b'bad')]) as data, \
             patch.object(module.UnityPy, 'load', side_effect=ValueError('bad data')):
            report = module.inventory(data)
        self.assertEqual(report['problems'], [{'group': 'level0', 'reason': 'ValueError'}])


if __name__ == '__main__': unittest.main()
