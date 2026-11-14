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


if __name__ == '__main__':
    unittest.main()
