import copy
import importlib.util
from pathlib import Path
import struct
import unittest


path = Path(__file__).resolve().parents[1] / 'animation_binding_reorder.py'
spec = importlib.util.spec_from_file_location('animation_binding_reorder', path)
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


def binding(path):
    return {'path': path, 'attribute': 1, 'typeID': 4, 'customType': 0,
            'isPPtrCurve': 0, 'isIntCurve': 0, 'isSerializeReferenceCurve': 0}


def tree(bindings, deltas):
    return {'m_ClipBindingConstant': {'genericBindings': bindings},
            'm_MuscleClip': {'m_ValueArrayDelta': deltas,
                             'm_Clip': {'data': {'m_StreamedClip': {'data': []}}}}}


def frame(time, keys):
    word = struct.unpack('<I', struct.pack('<f', time))[0]
    return [word, len(keys)] + [item for key in keys for item in (key, 0, 0, 0, 0)]


class BindingReorderTest(unittest.TestCase):
    def test_binding_reordering_checks_deltas_and_other_metadata(self):
        a = tree([binding(1), binding(2)], list(range(6)))
        b = tree([binding(2), binding(1)], [3, 4, 5, 0, 1, 2])
        self.assertEqual(study.binding_map(a, b), [3, 4, 5, 0, 1, 2])
        bad = copy.deepcopy(b); bad['m_MuscleClip']['m_ValueArrayDelta'][0] = -1
        with self.assertRaises(ValueError): study.binding_map(a, bad)
        bad = copy.deepcopy(b); bad['m_MuscleClip']['extra'] = 1
        with self.assertRaises(ValueError): study.binding_map(a, bad)

    def test_ambiguous_or_unknown_bindings_are_rejected(self):
        duplicate = tree([binding(1), binding(1)], [0] * 6)
        with self.assertRaises(ValueError): study.binding_map(duplicate, duplicate)
        unknown = tree([binding(1)], [0] * 3)
        unknown['m_ClipBindingConstant']['genericBindings'][0]['typeID'] = 5
        with self.assertRaises(ValueError): study.binding_map(unknown, unknown)

    def test_pairs_use_source_offsets_after_reordering(self):
        target = frame(0, [0, 3]) + frame(1, [0, 3])
        source = frame(0, [0, 3]) + frame(1, [0, 3])
        pairs = study.coefficient_pairs(target, source, [3, 4, 5, 0, 1, 2])
        self.assertEqual(pairs, [(0, 0.0, 3, 8), (3, 0.0, 8, 3),
                                 (0, 1.0, 15, 20), (3, 1.0, 20, 15)])
        with self.assertRaises(ValueError):
            study.coefficient_pairs(target, frame(0, [0, 3]) + frame(2, [0, 3]), [3, 4, 5, 0, 1, 2])
        with self.assertRaises(ValueError):
            study.coefficient_pairs(target, frame(0, [0, 0]), [3, 4, 5, 0, 1, 2])
        with self.assertRaises(ValueError):
            study.coefficient_pairs(target, source, [3, 3, 5, 0, 1, 2])
        sentinel = frame(float('-inf'), [0])
        self.assertEqual(len(study.coefficient_pairs(sentinel, sentinel, [0])), 1)
        invalid = frame(float('nan'), [0])
        with self.assertRaises(ValueError): study.coefficient_pairs(invalid, invalid, [0])


if __name__ == '__main__':
    unittest.main()
