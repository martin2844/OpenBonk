import importlib.util
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch

path = Path(__file__).resolve().parents[1] / 'audit-inline-texture-payloads.py'
spec = importlib.util.spec_from_file_location('texture_audit', path)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class InlineTexturePayloadTest(unittest.TestCase):
    def test_counts_only_retained_payload_bytes(self):
        body = b'header' + b'\x11\x22\x33\x44' + b'end'
        target = b'outer' + body
        masked = b'outerheader\x11\x00\x33\x00end'
        self.assertEqual(audit.retained_payload(target, masked, 5, body, b'\x11\x22\x33\x44'), 2)

    def test_rejects_wrong_template_and_ambiguous_payload(self):
        with self.assertRaisesRegex(ValueError, 'template payload differs'):
            audit.retained_payload(b'HEADabcd', b'HEADabXd', 4, b'abcd', b'abcd')
        with self.assertRaisesRegex(ValueError, 'ambiguous'):
            audit.retained_payload(b'ababab', b'ababab', 0, b'ababab', b'ab')

    def test_rejects_wrong_reference_or_range(self):
        with self.assertRaisesRegex(ValueError, 'object differs'):
            audit.retained_payload(b'HEADabcd', b'HEADabcd', 4, b'xyz', b'xyz')
        with self.assertRaisesRegex(ValueError, 'range'):
            audit.retained_payload(b'abcd', b'abc', 0, b'abcd', b'abcd')

    def test_ambiguous_zero_payload_requires_mask_only_object(self):
        body = b'\0a\0b'
        self.assertEqual(audit.retained_payload(body, b'\0\0\0b', 0, body, b'\0'), 0)
        with self.assertRaisesRegex(ValueError, 'beyond masking'):
            audit.retained_payload(body, b'\0aXb', 0, body, b'\0')

    def test_audit_reports_skipped_and_counts_retained_data(self):
        part = SimpleNamespace(filename='assets/bin/Data/x', file_size=8)
        obj = SimpleNamespace(type=SimpleNamespace(name='Texture2D'), path_id=1, byte_start=4,
                              get_raw_data=lambda: b'abcd',
                              read=lambda: SimpleNamespace(image_data=b'abcd', m_Name='example'))
        streamed = SimpleNamespace(type=SimpleNamespace(name='Texture2D'), path_id=2,
                                   read=lambda: SimpleNamespace(image_data=b'', m_Name='streamed'),
                                   get_raw_data=lambda: b'')
        empty = SimpleNamespace(type=SimpleNamespace(name='Texture2D'), path_id=3,
                                read=lambda: SimpleNamespace(image_data=b'', m_Name='empty', m_Width=0, m_Height=0,
                                                             m_StreamData=SimpleNamespace(path='', size=0)),
                                get_raw_data=lambda: b'')
        reference = SimpleNamespace(read=lambda _: b'HEADabcd')
        template = SimpleNamespace(read=lambda _: b'HEADa\0c\0')
        with patch.object(audit.objects, 'asset_groups', return_value={'x': [part]}), \
             patch.object(audit.UnityPy, 'load', return_value=SimpleNamespace(objects=[obj, streamed, empty])):
            report = audit.audit(reference, template)
        self.assertEqual(report['retained_nonzero_bytes'], 2)
        self.assertEqual(report['inline_texture_count'], 1)
        self.assertEqual(report['skipped'][0]['reason'], 'no inline payload')
        self.assertEqual(report['empty_textures'][0]['path_id'], 3)


if __name__ == '__main__':
    unittest.main()
