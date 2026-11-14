import importlib.util
from pathlib import Path
import struct
import unittest

spec = importlib.util.spec_from_file_location('scene_compare', Path(__file__).resolve().parents[1] / 'compare-unity-scene-behaviours.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class SceneBehaviourTests(unittest.TestCase):
    def setUp(self):
        self.references = {(2, 17): ['AudioClip', 'sound'], (4, 23): ['GameObject', 'effect'], (0, 0): ['null']}
        self.resolve = lambda file_id, path_id: self.references[(file_id, path_id)]

    def test_sound_compares_resolved_clips_and_remaining_fields(self):
        tail = struct.pack('<Iiqiq', 2, 2, 17, 0, 0) + b'other fields'
        actual = module.normalized_tail('RandomSfx', tail, self.resolve)
        self.assertEqual(actual, [[['AudioClip', 'sound'], ['null']], b'other fields'.hex()])
        with self.assertRaises(ValueError): module.normalized_tail('RandomSfx', struct.pack('<I', 65), self.resolve)
        with self.assertRaises(ValueError): module.normalized_tail('RandomSfx', struct.pack('<I', 1), self.resolve)

    def test_lava_and_pot_preserve_nonreference_data(self):
        self.assertEqual(module.normalized_tail('Lava', struct.pack('<iq', 4, 23), self.resolve), [['GameObject', 'effect']])
        prefix, suffix = bytes(range(20)), b'pot fields'
        tail = prefix + struct.pack('<iq', 4, 23) + struct.pack('<iq', 0, 0) * 4 + suffix
        fields = module.normalized_tail('InteractablePot', tail, self.resolve)
        self.assertEqual(fields[0], prefix.hex()); self.assertEqual(fields[1][0], ['GameObject', 'effect'])
        self.assertEqual(fields[1][1:], [['null']] * 4); self.assertEqual(fields[2], suffix.hex())
        with self.assertRaises(ValueError): module.normalized_tail('InteractablePot', tail[:79], self.resolve)
        with self.assertRaises(ValueError): module.normalized_tail('Lava', bytes(13), self.resolve)
        with self.assertRaises(ValueError): module.normalized_tail('unknown', b'', self.resolve)

    def test_ui_images_resolve_material_and_image_references(self):
        for cls, size in [('Image', 104), ('RawImage', 84)]:
            data = bytearray(size)
            struct.pack_into('<iq', data, 0, 0, 0)
            struct.pack_into('<iq', data, 56, 4, 23)
            data[20] = 42; data[70] = 17
            actual = module.normalized_tail(cls, bytes(data), self.resolve)
            self.assertEqual(actual[0], ['null']); self.assertEqual(actual[2], ['GameObject', 'effect'])
            self.assertEqual(actual[1], bytes(data[12:56]).hex()); self.assertEqual(actual[3], bytes(data[68:]).hex())
            with self.assertRaises(ValueError): module.normalized_tail(cls, bytes(data[:-1]), self.resolve)


if __name__ == '__main__': unittest.main()
