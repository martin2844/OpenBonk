import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch
from PIL import Image

spec = importlib.util.spec_from_file_location('chest_rgba', Path(__file__).resolve().parents[1] / 'experimental-steam-chest-rgba.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class ChestRgbaTests(unittest.TestCase):
    def test_split_preserves_names_and_rejects_count_changes(self):
        parts = [SimpleNamespace(filename='split0', file_size=module.CHUNK),
                 SimpleNamespace(filename='split1', file_size=7)]
        payload = b'A' * module.CHUNK + b'B' * 21
        result = module.split_serialized(payload, parts)
        self.assertEqual(result, {'split0': b'A' * module.CHUNK, 'split1': b'B' * 21})
        for bad in (b'A' * module.CHUNK, b'A' * (module.CHUNK * 2 + 1)):
            with self.assertRaises(ValueError): module.split_serialized(bad, parts)
        with self.assertRaises(ValueError):
            module.split_serialized(payload, [SimpleNamespace(filename='a', file_size=7), parts[1]])
        with self.assertRaises(ValueError):
            module.split_serialized(payload, [parts[0], SimpleNamespace(filename='split0', file_size=7)])
        with self.assertRaises(ValueError):
            module.split_serialized(payload, [parts[0], SimpleNamespace(filename='split1', file_size=0)])

    def test_target_metadata_and_stream_are_checked(self):
        data = b'x' * 43704
        target = SimpleNamespace(m_Name='t_chest_normal', m_Width=256, m_Height=256, m_MipCount=9,
                                 m_ImageCount=1, m_TextureDimension=2, m_TextureFormat=45, image_data=data,
                                 m_StreamData=SimpleNamespace(path='', size=0))
        tree = {'m_CompleteImageSize': 43704, 'm_MipsStripped': 0, 'image data': data}
        module.validate_target(target, tree)
        for changed in ({'m_MipsStripped': 1}, {'m_CompleteImageSize': 1}, {'image data': b'different'}):
            with self.assertRaises(ValueError): module.validate_target(target, dict(tree, **changed))
        target.m_StreamData.path = 'unexpected.resS'
        with self.assertRaises(ValueError): module.validate_target(target, tree)

    def test_steam_source_requires_matching_unique_content(self):
        target = SimpleNamespace(m_Name='t_chest_normal', m_Width=256, m_Height=256,
                                 m_MipCount=9, m_ImageCount=1, m_ColorSpace=1)
        def texture(data, fmt=10):
            return SimpleNamespace(m_MipCount=9, m_ImageCount=1, m_ColorSpace=1, m_TextureFormat=fmt,
                                   get_image_data=lambda: data)
        item = ('sharedassets1', 12, texture(b'one'))
        index = {('t_chest_normal', 256, 256): [item]}
        self.assertEqual(module.select_source(target, index), item)
        index[('t_chest_normal', 256, 256)].append(('sharedassets2', 13, texture(b'different')))
        with self.assertRaises(ValueError): module.select_source(target, index)
        index[('t_chest_normal', 256, 256)] = [('sharedassets1', 12, texture(b'one', 4))]
        with self.assertRaises(ValueError): module.select_source(target, index)

    def test_manifest_is_copied_and_existing_target_is_rejected(self):
        report = {'schema': 1, 'textures': [{'group': 'sharedassets1', 'target_id': 123, 'name': 'unrelated'}]}
        updated = module.augmented_manifest(report, 571)
        self.assertEqual(len(report['textures']), 1)
        self.assertEqual(updated['textures'][-1], {'group': 'sharedassets1', 'target_id': 571, 'name': 't_chest_normal',
                          'source_format': 10, 'target_format': 4, 'normal': False, 'quality': False,
                          'direct_normal': False, 'bytes': 349524})
        with self.assertRaises(ValueError): module.augmented_manifest(updated, 571)
        with self.assertRaises(ValueError): module.augmented_manifest({'schema': 2, 'textures': []}, 571)
        with self.assertRaises(ValueError): module.augmented_manifest(report, True)

    def test_optional_water_manifest_rejects_duplicate_and_preserves_default(self):
        report = {'schema': 1, 'textures': []}
        self.assertEqual(len(module.augmented_manifest(report, 571)['textures']), 1)
        rows = module.augmented_manifest(report, 571, 478)['textures']
        self.assertEqual(len(rows), 2)
        self.assertEqual((rows[1]['name'], rows[1]['normal_rgba'], rows[1]['bytes']), ('IL3DN_WaterNormal', True, 5460))
        for invalid in (True, 571):
            with self.assertRaises(ValueError): module.augmented_manifest(report, 571, invalid)
        with self.assertRaises(ValueError): module.augmented_manifest({'schema': 1, 'textures': [rows[1]]}, 571, 478)

    def test_water_requires_all_mip_alpha_and_normal_fidelity(self):
        data = b'x' * 5460
        target = SimpleNamespace(m_Width=32, m_Height=32, m_MipCount=6, m_ImageCount=1,
                                 m_TextureDimension=2, m_TextureFormat=4, image_data=data,
                                 m_StreamData=SimpleNamespace(path='', size=0),
                                 object_reader=SimpleNamespace(version=None, platform=None))
        tree = {'m_CompleteImageSize': 5460, 'm_MipsStripped': 0, 'image data': data}
        source = ('sharedassets1', 963, SimpleNamespace(m_TextureFormat=12))
        mips = [(1, 1, b'pixel')] * 6
        opaque = Image.new('RGBA', (1, 1), (128, 128, 255, 255))
        transparent = Image.new('RGBA', (1, 1), (128, 128, 255, 0))
        with patch.object(module.partial, 'candidate_sources', return_value=[source]), \
             patch.object(module.partial.study, 'select_source', return_value=source), \
             patch.object(module.partial, 'converted', return_value=data), \
             patch.object(module.partial.normal_rgba, 'reconstruct', return_value=data), \
             patch.object(module.partial.study, 'mip_slices', return_value=mips), \
             patch.object(module.partial.study, 'acceptable_normal_image', return_value=True), \
             patch.object(module.partial, 'decode_block_image', return_value=opaque) as decode:
            self.assertEqual(module.water_payload(target, tree, {})[1], data)
            decode.side_effect = [opaque, opaque] * 5 + [opaque, transparent]
            with self.assertRaisesRegex(ValueError, 'alpha or angular fidelity'):
                module.water_payload(target, tree, {})
            decode.side_effect = None
            with patch.object(module.partial.study, 'acceptable_normal_image', return_value=False):
                with self.assertRaisesRegex(ValueError, 'alpha or angular fidelity'):
                    module.water_payload(target, tree, {})

    def test_object_fingerprints_detect_duplicate_path_ids(self):
        def obj(path, raw):
            return SimpleNamespace(path_id=path, type=SimpleNamespace(name='Texture2D'), get_raw_data=lambda: raw)
        env = SimpleNamespace(objects=[obj(1, b'one'), obj(2, b'two')])
        self.assertEqual(set(module.object_fingerprints(env)), {1, 2})
        with self.assertRaises(ValueError): module.object_fingerprints(SimpleNamespace(objects=[obj(1, b'a'), obj(1, b'b')]))


if __name__ == '__main__': unittest.main()
