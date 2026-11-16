import importlib.util
from pathlib import Path
import struct
from types import SimpleNamespace
import unittest
from unittest.mock import patch


path = Path(__file__).resolve().parents[1] / 'audit-inline-animation-payloads.py'
spec = importlib.util.spec_from_file_location('animation_payload_audit', path)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class AnimationPayloadAuditTest(unittest.TestCase):
    def tree(self, words, dense=None, constant=None):
        return {'m_Name': 'fixture', 'm_MuscleClip': {'m_Clip': {'data': {
            'm_StreamedClip': {'data': words}, 'm_DenseClip': {'m_SampleArray': dense or []},
            'm_ConstantClip': {'data': constant or []}}}}}

    def test_encodes_each_nonempty_array(self):
        result = dict(audit.sample_payloads(self.tree([1, 2], [1.5], [-2.0])))
        self.assertEqual(result['streamed'], struct.pack('<II', 1, 2))
        self.assertEqual(result['dense'], struct.pack('<f', 1.5))
        self.assertEqual(result['constant'], struct.pack('<f', -2.0))

    def test_counts_at_traced_offset_when_payload_repeats(self):
        payload = struct.pack('<f', 1.0)
        body = payload + b'HEAD' + payload
        masked = payload + b'HEAD' + b'\0\0\x80\0'
        self.assertEqual(audit.count_at_offset(body, masked, 0, body, payload, 8), 1)
        with self.assertRaises(ValueError):
            audit.count_at_offset(body, payload + b'HEAD' + b'\0\0\x81\0', 0, body, payload, 8)
        with self.assertRaises(ValueError):
            audit.count_at_offset(body, masked, 0, body, payload, 7)

    def test_counts_retained_bytes_and_reports_empty_clip(self):
        payload = struct.pack('<II', 0x11223344, 0x55667788)
        body = b'HEAD' + payload + b'END!'
        part = SimpleNamespace(filename='a', file_size=len(body))
        obj = SimpleNamespace(type=SimpleNamespace(name='AnimationClip'), path_id=1, byte_start=0,
                              read_typetree=lambda: self.tree([0x11223344, 0x55667788]),
                              get_raw_data=lambda: body)
        empty = SimpleNamespace(type=SimpleNamespace(name='AnimationClip'), path_id=2,
                                read_typetree=lambda: self.tree([]), get_raw_data=lambda: body)
        source = SimpleNamespace(read=lambda _: body)
        masked = SimpleNamespace(read=lambda _: b'HEAD' + bytes([0, 0x33, 0, 0x11, 0, 0, 0, 0]) + b'END!')
        with patch.object(audit.texture_audit.objects, 'asset_groups', return_value={'a': [part]}), \
             patch.object(audit.UnityPy, 'load', return_value=SimpleNamespace(objects=[obj, empty])):
            report = audit.audit(source, masked)
        self.assertEqual(report['array_count'], 1)
        self.assertEqual(report['retained_nonzero_bytes'], 2)
        self.assertEqual(len(report['empty_clips']), 1)
        self.assertEqual(report['skipped'], [])


if __name__ == '__main__':
    unittest.main()
