import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('converted_plan', Path(__file__).resolve().parents[1] / 'experimental-converted-texture-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ConvertedTexturePlanTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        root = Path(self.temp.name); self.source = root / 'steam'; self.source.mkdir()
        source_file = self.source / 'sharedassets1.assets'; source_file.write_bytes(b'xxsourceyy')
        self.template, self.plan_path, self.output = (root / n for n in ('template.obb', 'plan.json', 'output.obb'))
        self.entries = [module.study.PREFIX + 'sharedassets1.assets.split' + str(i) for i in range(2)]
        with zipfile.ZipFile(self.template, 'w') as z:
            z.writestr(self.entries[0], b'abc' + bytes(2))
            z.writestr(self.entries[1], bytes(3) + b'xy')
            z.writestr('other', b'keep')
        self.row = {'id': 0, 'source': 'sharedassets1.assets', 'source_offset': 2, 'source_size': 6,
                    'source_sha256': module.sha(b'source'), 'target_size': 5, 'target_sha256': module.sha(b'ABCDE'),
                    'width': 1, 'height': 1, 'mips': 1, 'source_format': 4, 'target_format': 4,
                    'fragments': [{'entry': self.entries[0], 'target_offset': 3, 'size': 2, 'converted_offset': 0},
                                  {'entry': self.entries[1], 'target_offset': 0, 'size': 3, 'converted_offset': 2}]}
        self.plan = {'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                     'template_sha256': module.file_hash(self.template),
                     'sources': {'sharedassets1.assets': module.file_hash(source_file)}, 'records': [self.row]}

    def restore(self, value=b'ABCDE'):
        self.plan_path.write_text(json.dumps(self.plan))
        module.restore(SimpleNamespace(steam_data=self.source, template=self.template, plan=self.plan_path, output=self.output),
                       convert=lambda payload, row: value)

    def test_reconstructs_split_fragments_and_preserves_other_entry(self):
        self.restore()
        with zipfile.ZipFile(self.output) as z:
            self.assertEqual(z.read(self.entries[0]), b'abcAB')
            self.assertEqual(z.read(self.entries[1]), b'CDExy')
            self.assertEqual(z.read('other'), b'keep')

    def test_rejects_conversion_mismatch_and_incomplete_ranges(self):
        with self.assertRaises(ValueError): self.restore(b'wrong')
        self.assertFalse(self.output.exists())
        self.row['fragments'][1]['converted_offset'] = 1
        with self.assertRaises(ValueError): self.restore()
        self.assertFalse(self.output.exists())

    def test_rejects_overlapping_fragments_and_removes_output(self):
        self.row['fragments'][1].update(entry=self.entries[0], target_offset=3)
        with self.assertRaises(ValueError): self.restore()
        self.assertFalse(self.output.exists())

    def test_normal_format_requires_explicit_flag_and_dxt5_input(self):
        self.row.update(source_format=12, target_format=34, normal=True)
        self.restore()
        self.output.unlink()
        self.row['normal'] = False
        with self.assertRaises(ValueError): self.restore()
        self.row.update(normal=True, source_format=4)
        with self.assertRaises(ValueError): self.restore()
        self.assertFalse(self.output.exists())

    def test_quality_requires_rgba_target_and_encoder(self):
        self.row.update(quality=True, target_format=4)
        with self.assertRaises(ValueError): self.restore()
        self.row['target_format'] = 47
        with self.assertRaisesRegex(ValueError, 'quality encoder is required'): self.restore()
        self.assertFalse(self.output.exists())

    def test_reconstructs_standalone_hashed_entry(self):
        entry = module.study.PREFIX + 'a' * 32
        with zipfile.ZipFile(self.template, 'w') as z:
            z.writestr(entry, b'header' + bytes(5) + b'tail')
        self.plan['template_sha256'] = module.file_hash(self.template)
        self.row['fragments'] = [{'entry': entry, 'target_offset': 6, 'size': 5, 'converted_offset': 0}]
        self.restore()
        with zipfile.ZipFile(self.output) as z: self.assertEqual(z.read(entry), b'headerABCDEtail')


if __name__ == '__main__': unittest.main()
