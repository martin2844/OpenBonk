import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'experimental-steam-audio-plan.py'
SPEC = importlib.util.spec_from_file_location('experimental_steam_audio_plan', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class AudioPlanTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-audio-plan-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'steam'
        self.source.mkdir()
        (self.source / 'sharedassets1.assets').write_bytes(b'asset')
        (self.source / 'sharedassets1.resource').write_bytes(b'prefixaudio-data-suffix')
        self.template = self.root / 'template.obb'
        with zipfile.ZipFile(self.template, 'w') as archive:
            archive.writestr('audio.resource', bytes(10))
            archive.writestr('unrelated', b'keep')
        self.plan = self.root / 'plan.json'
        self.doc = {'schema': 1, 'kind': 'experimental-steam-audio-offsets',
                    'asset_sha256': MODULE.file_digest(self.source / 'sharedassets1.assets'),
                    'resource_sha256': MODULE.file_digest(self.source / 'sharedassets1.resource'),
                    'template_sha256': MODULE.file_digest(self.template),
                    'records': [{'entry': 'audio.resource', 'source_offset': 6, 'size': 10,
                                 'sha256': hashlib.sha256(b'audio-data').hexdigest()}]}
        self.plan.write_text(json.dumps(self.doc))
        self.output = self.root / 'output.obb'

    def run_restore(self):
        MODULE.restore(SimpleNamespace(steam_data=self.source, template=self.template,
                                       plan=self.plan, output=self.output))

    def test_restores_expected_payload_and_leaves_other_entries(self):
        self.run_restore()
        with zipfile.ZipFile(self.output) as archive:
            self.assertEqual(archive.read('audio.resource'), b'audio-data')
            self.assertEqual(archive.read('unrelated'), b'keep')
        with self.assertRaises(ValueError):
            self.run_restore()

    def test_rejects_changed_steam_input_without_output(self):
        (self.source / 'sharedassets1.resource').write_bytes(b'wrong')
        with self.assertRaisesRegex(ValueError, 'input differs'):
            self.run_restore()
        self.assertFalse(self.output.exists())

    def test_rejects_bad_range_and_removes_partial_output(self):
        self.doc['records'][0]['source_offset'] = 1000
        self.plan.write_text(json.dumps(self.doc))
        with self.assertRaises(ValueError):
            self.run_restore()
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
