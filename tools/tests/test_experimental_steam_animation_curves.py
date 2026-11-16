import importlib.util
from pathlib import Path
import struct
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile


path = Path(__file__).resolve().parents[1] / 'experimental-steam-animation-curves.py'
spec = importlib.util.spec_from_file_location('steam_animation_curves', path)
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


class AnimationCurveTest(unittest.TestCase):
    def test_accepts_small_curve_difference_and_rejects_larger_one(self):
        left = [(0.0, [(3, (0.0, 0.0, 0.0, 1.0))]), (1.0, [])]
        close = [(0.0, [(3, (0.0, 0.0, 0.0, 1.0000001))]), (1.0, [])]
        far = [(0.0, [(3, (0.0, 0.0, 0.0, 1.01))]), (1.0, [])]
        self.assertIsNotNone(study.compatible_frames(left, close))
        self.assertIsNone(study.compatible_frames(left, far))

    def test_relaxed_bound_is_explicit_and_still_rejects_larger_differences(self):
        left = [(0.0, [(3, (0.0, 0.0, 0.0, 1.0))]), (1.0, [])]
        close = [(0.0, [(3, (0.0, 0.0, 0.0, 1.00005))]), (1.0, [])]
        far = [(0.0, [(3, (0.0, 0.0, 0.0, 1.0002))]), (1.0, [])]
        self.assertIsNone(study.compatible_frames(left, close))
        self.assertIsNotNone(study.compatible_frames(left, close, limit=study.RELAXED_LIMIT))
        self.assertIsNone(study.compatible_frames(left, far, limit=study.RELAXED_LIMIT))
        self.assertIsNotNone(study.compatible_frames(left, far, limit=study.EXTENDED_LIMIT))
        too_far = [(0.0, [(3, (0.0, 0.0, 0.0, 1.002))]), (1.0, [])]
        self.assertIsNone(study.compatible_frames(left, too_far, limit=study.EXTENDED_LIMIT))

    def test_rejects_curve_mapping_and_sentinel_differences(self):
        coeff = (0.0, 0.0, 0.0, 1.0)
        left = [(-1.0, [(3, coeff)]), (0.0, [(3, coeff)]), (1.0, [])]
        different_sentinel = [(-1.0, [(3, (0.0, 0.0, 0.0, 1.0000001))]), (0.0, [(3, coeff)]), (1.0, [])]
        different_index = [(-1.0, [(3, coeff)]), (0.0, [(4, coeff)]), (1.0, [])]
        self.assertIsNone(study.compatible_frames(left, different_sentinel))
        self.assertIsNone(study.compatible_frames(left, different_index))

    def test_detects_difference_between_equal_segment_endpoints(self):
        zero = [(0.0, [(1, (0.0, 0.0, 0.0, 0.0))]), (1.0, [])]
        hump = [(0.0, [(1, (0.0, -4.0, 4.0, 0.0))]), (1.0, [])]
        self.assertIsNone(study.compatible_frames(zero, hump))

    def test_rejects_unchecked_or_nonfinite_intervals(self):
        coeff = (0.0, 0.0, 0.0, 1.0)
        self.assertIsNone(study.compatible_frames([(0.0, [(1, coeff)])], [(0.0, [(1, coeff)])]))
        self.assertIsNone(study.compatible_frames([(float('nan'), [])], [(float('nan'), [])]))

    def test_candidate_requires_matching_metadata_and_unique_stream(self):
        def word(value):
            return struct.unpack('<I', struct.pack('<f', value))[0]
        def obj(value, label='same', duplicate=False):
            words = [word(0), 1, 3, word(0), word(0), word(0), word(value), word(1), 0]
            payload = struct.pack('<%dI' % len(words), *words)
            tree = {'name': label, 'm_MuscleClip': {'m_Clip': {'data': {'m_StreamedClip': {'data': words}}}}}
            body = b'HEAD' + payload + (payload if duplicate else b'END')
            return SimpleNamespace(read_typetree=lambda: tree, get_raw_data=lambda: body)
        target = obj(1.0)
        result = study.candidate(target, obj(1.0000001))
        self.assertIsNotNone(result)
        self.assertEqual(result[0], 4)
        self.assertIsNone(study.candidate(target, obj(1.0000001, label='different')))
        self.assertIsNone(study.candidate(obj(1.0, duplicate=True), obj(1.0000001)))

    def test_generation_changes_only_the_selected_stream(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root / 'steam'; source.mkdir()
            for name in study.animation.SOURCE_NAMES: (source / name).write_bytes(b'fixture')
            obb = root / 'input.obb'; output = root / 'output'
            with zipfile.ZipFile(obb, 'w') as z:
                z.writestr('asset', b'HEADxxABCDyyTAIL')
                z.writestr('unrelated', b'preserve')
            obj = SimpleNamespace(type=SimpleNamespace(name='AnimationClip'), byte_start=4, byte_size=8, path_id=1,
                                  read=lambda: SimpleNamespace(m_Name='fixture'))
            env = SimpleNamespace(objects=[obj])
            def groups(archive): return {'asset': [archive.getinfo('asset')]}
            with patch.object(study.UnityPy, 'load', return_value=env), \
                 patch.object(study.objects, 'asset_groups', side_effect=groups), \
                 patch.object(study, 'candidate', return_value=(2, b'ABCD', b'WXYZ', 0.0)):
                study.generate(SimpleNamespace(steam_data=source, obb=obb, output=output))
            with zipfile.ZipFile(output / 'converted.obb') as z:
                self.assertEqual(z.read('asset'), b'HEADxxWXYZyyTAIL')
                self.assertEqual(z.read('unrelated'), b'preserve')


if __name__ == '__main__':
    unittest.main()
