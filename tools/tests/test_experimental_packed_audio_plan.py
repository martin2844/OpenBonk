import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / 'experimental-packed-audio-plan.py'
spec = importlib.util.spec_from_file_location('packed_plan', SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PackedPlanTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name)
        self.steam = root / 'steam'
        self.steam.mkdir()
        for group in module.audio.GROUPS:
            (self.steam / (group + '.assets')).write_bytes(b'asset')
            (self.steam / (group + '.resource')).write_bytes(b'prefix-audio-suffix')
        self.entry = module.audio.PREFIX + 'sharedassets1.resource'
        self.template = root / 'template.obb'
        with zipfile.ZipFile(self.template, 'w') as z:
            z.writestr(self.entry, b'head' + bytes(5) + b'tail')
            z.writestr('other', b'unmodified')
        self.plan = root / 'plan.json'
        self.output = root / 'restored.obb'
        self.doc = {'schema': 1, 'kind': 'experimental-packed-steam-audio-offsets',
                    'sources': {n: module.file_digest(p) for n, p in module.sources(self.steam).items()},
                    'template_sha256': module.file_digest(self.template),
                    'records': [{'entry': self.entry, 'target_offset': 4, 'source': 'sharedassets1.resource',
                                 'source_offset': 7, 'size': 5, 'sha256': hashlib.sha256(b'audio').hexdigest()}]}

    def restore(self):
        self.plan.write_text(json.dumps(self.doc))
        module.restore(SimpleNamespace(steam_data=self.steam, template=self.template,
                                       plan=self.plan, output=self.output))

    def test_restores_payload_and_preserves_other_content(self):
        self.restore()
        with zipfile.ZipFile(self.output) as z:
            self.assertEqual(z.read(self.entry), b'headaudiotail')
            self.assertEqual(z.read('other'), b'unmodified')

    def test_rejects_tampering_and_cleans_partial_output(self):
        self.doc['records'][0]['sha256'] = '0' * 64
        with self.assertRaises(ValueError):
            self.restore()
        self.assertFalse(self.output.exists())

    def test_rejects_overlap_and_changed_source(self):
        self.doc['records'].append(dict(self.doc['records'][0]))
        with self.assertRaises(ValueError):
            self.restore()
        self.assertFalse(self.output.exists())
        self.doc['records'].pop()
        (self.steam / 'sharedassets1.resource').write_bytes(b'changed')
        with self.assertRaises(ValueError):
            self.restore()
        self.assertFalse(self.output.exists())

    def test_restores_sharedassets4(self):
        self.doc['records'][0]['source'] = 'sharedassets4.resource'
        self.doc['records'][0]['entry'] = module.audio.PREFIX + 'sharedassets4.resource'
        with zipfile.ZipFile(self.template, 'w') as z:
            z.writestr(self.doc['records'][0]['entry'], b'head' + bytes(5) + b'tail')
        self.doc['template_sha256'] = module.file_digest(self.template)
        self.restore()
        with zipfile.ZipFile(self.output) as z:
            self.assertEqual(z.read(self.doc['records'][0]['entry']), b'headaudiotail')

    def test_previous_two_group_plan_remains_supported(self):
        for name in ('sharedassets4.assets', 'sharedassets4.resource'):
            self.doc['sources'].pop(name)
            (self.steam / name).unlink()
        self.restore()
        with zipfile.ZipFile(self.output) as z:
            self.assertEqual(z.read(self.entry), b'headaudiotail')

    def test_old_plan_cannot_reference_undeclared_fourth_group(self):
        for name in ('sharedassets4.assets', 'sharedassets4.resource'):
            self.doc['sources'].pop(name)
        self.doc['records'][0]['source'] = 'sharedassets4.resource'
        with self.assertRaises(ValueError): self.restore()
        self.assertFalse(self.output.exists())

    def test_loading_video_requires_exact_payload_and_valid_bounds(self):
        source = {'m_Name': 'Buffering3', 'm_ExternalResources':
                  {'m_Source': 'sharedassets1.resource', 'm_Offset': 7, 'm_Size': 5}}
        target = {'m_Name': 'Buffering3', 'm_ExternalResources':
                  {'m_Source': 'sharedassets1.resource', 'm_Offset': 4, 'm_Size': 5}}
        row, = module.video_records(target, source, 'sharedassets1', b'headaudiotail', module.sources(self.steam))
        self.assertEqual(row['source_offset'], 7)
        self.assertEqual(row['target_offset'], 4)
        self.assertEqual(row['sha256'], hashlib.sha256(b'audio').hexdigest())
        with self.assertRaisesRegex(ValueError, 'does not match'):
            module.video_records(target, source, 'sharedassets1', b'headxxxxxTail', module.sources(self.steam))
        bad = dict(source, m_ExternalResources={'m_Source': '../secret', 'm_Offset': 7, 'm_Size': 5})
        with self.assertRaisesRegex(ValueError, 'reference'):
            module.video_records(target, bad, 'sharedassets1', b'headaudiotail', module.sources(self.steam))

    def test_loading_video_allows_only_track_uid_difference(self):
        header = b'\x1a\x45\xdf\xa3payload\x73\xc5\x87'
        source_bytes = header + b'1234567' + b'video data'
        target_bytes = header + b'ABCDEFG' + b'video data'
        (self.steam / 'sharedassets1.resource').write_bytes(source_bytes)
        source = {'m_Name': 'Buffering3', 'm_ExternalResources':
                  {'m_Source': 'sharedassets1.resource', 'm_Offset': 0, 'm_Size': len(source_bytes)}}
        target = {'m_Name': 'Buffering3', 'm_ExternalResources':
                  {'m_Source': 'sharedassets1.resource', 'm_Offset': 0, 'm_Size': len(target_bytes)}}
        rows = module.video_records(target, source, 'sharedassets1', target_bytes, module.sources(self.steam))
        masked = module.apply_ranges(target_bytes, rows)
        self.assertEqual(masked[len(header):len(header) + 7], b'ABCDEFG')
        with (self.steam / 'sharedassets1.resource').open('rb') as stream:
            self.assertEqual(module.apply_ranges(masked, rows, {'sharedassets1.resource': stream}), target_bytes)
        with self.assertRaisesRegex(ValueError, 'does not match'):
            module.video_records(target, source, 'sharedassets1', target_bytes[:-1] + b'X', module.sources(self.steam))
        marker = b'\x73\xc5\x87'
        ambiguous_source = source_bytes + marker
        ambiguous_target = target_bytes + marker
        (self.steam / 'sharedassets1.resource').write_bytes(ambiguous_source)
        source['m_ExternalResources']['m_Size'] = len(ambiguous_source)
        target['m_ExternalResources']['m_Size'] = len(ambiguous_target)
        with self.assertRaisesRegex(ValueError, 'does not match'):
            module.video_records(target, source, 'sharedassets1', ambiguous_target, module.sources(self.steam))


if __name__ == '__main__':
    unittest.main()
