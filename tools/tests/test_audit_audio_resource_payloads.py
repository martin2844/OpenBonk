import importlib.util
import io
from pathlib import Path
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

path = Path(__file__).resolve().parents[1] / 'audit-audio-resource-payloads.py'
spec = importlib.util.spec_from_file_location('audio_audit', path)
audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)


def archive(entries):
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, 'w') as output:
        for name, data in entries: output.writestr(name, data)
    buffer.seek(0)
    return zipfile.ZipFile(buffer)


class AudioResourceAuditTests(unittest.TestCase):
    def test_only_the_referenced_range_counts(self):
        name = 'assets/bin/Data/sharedassets1.resource'
        with archive([(name, b'headerSOUNDvideo')]) as reference, archive([(name, b'header\0\0U\0\0video')]) as template:
            self.assertEqual(audit.retained_range(reference, template, 'sharedassets1.resource', 6, 5, {}), 1)

    def test_rejects_invalid_path_range_and_changed_payload(self):
        name = 'assets/bin/Data/sharedassets1.resource'
        with archive([(name, b'abc')]) as reference, archive([(name, b'aXc')]) as template:
            for source, offset, size in (('../sharedassets1.resource', 0, 1), ('sharedassets1.resource', -1, 1),
                                         ('sharedassets1.resource', 2, 2), ('sharedassets1.resource', 0, 0)):
                with self.subTest(source=source, offset=offset, size=size), self.assertRaises(ValueError):
                    audit.retained_range(reference, template, source, offset, size, {})
            with self.assertRaises(ValueError): audit.retained_range(reference, template, 'sharedassets1.resource', 0, 3, {})

    def test_clip_scan_reports_uncovered_inline_audio(self):
        name = 'assets/bin/Data/sharedassets1.resource'
        good = SimpleNamespace(type=SimpleNamespace(name='AudioClip'), path_id=1,
            read_typetree=lambda: {'m_Name': 'test', 'm_Resource': {'m_Source': 'sharedassets1.resource', 'm_Offset': 0, 'm_Size': 3}})
        inline = SimpleNamespace(type=SimpleNamespace(name='AudioClip'), path_id=2,
            read_typetree=lambda: {'m_AudioData': b'abc'})
        with archive([(name, b'abc'), ('group', b'fixture')]) as reference, archive([(name, b'\0\0\0')]) as template:
            with patch.object(audit.objects, 'asset_groups', return_value={'group': [reference.getinfo('group')]}), \
                    patch.object(audit.UnityPy, 'load', return_value=SimpleNamespace(objects=[good, inline])):
                result = audit.audit(reference, template)
        self.assertEqual((result['clip_count'], result['resource_count'], result['retained_nonzero_bytes']), (1, 1, 0))
        self.assertEqual(len(result['skipped']), 1)
        self.assertIn('inline audio', result['skipped'][0]['reason'])


if __name__ == '__main__': unittest.main()
