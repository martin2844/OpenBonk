import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('exact_texture_plan', Path(__file__).resolve().parents[1] / 'experimental-exact-texture-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ExactTexturePlanTests(unittest.TestCase):
    def test_range_across_splits_masks_and_restores(self):
        parts = [SimpleNamespace(filename='split0', file_size=5), SimpleNamespace(filename='split1', file_size=5)]
        rows = module.fragments(parts, 3, 5, 'sharedassets1.assets', 2, b'ABCDE')
        self.assertEqual([(r['target_offset'], r['source_offset'], r['size']) for r in rows], [(3, 2, 2), (0, 4, 3)])
        source = {'sharedassets1.assets': io.BytesIO(b'xxABCDEyy')}
        for original, row in ((b'123AB', rows[0]), (b'CDE89', rows[1])):
            masked = module.apply(original, [row])
            self.assertEqual(module.apply(masked, [row], source), original)

    def test_rejects_overlap_and_invalid_source(self):
        row = {'target_offset': 0, 'source_offset': 0, 'size': 1, 'source': 'sharedassets1.assets',
               'sha256': hashlib.sha256(b'a').hexdigest()}
        with self.assertRaises(ValueError): module.apply(b'ab', [row, dict(row)])
        row['source'] = '../asset'
        with self.assertRaises(ValueError): module.apply(b'ab', [row])

    def test_rejects_changed_restoration_payload(self):
        row = {'target_offset': 0, 'source_offset': 0, 'size': 1, 'source': 'sharedassets1.assets',
               'sha256': hashlib.sha256(b'a').hexdigest()}
        with self.assertRaises(ValueError): module.apply(b'\0', [row], {'sharedassets1.assets': io.BytesIO(b'b')})

    def test_restore_round_trip_and_bad_hash_cleanup(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); source = root / 'steam'; source.mkdir()
            (source / 'sharedassets1.assets').write_bytes(b'xxABCDEyy')
            template = root / 'template.obb'; plan_file = root / 'plan.json'; output = root / 'output.obb'
            entry = module.PREFIX + 'sharedassets1.assets.split0'
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr(entry, b'123' + bytes(5) + b'89')
                z.writestr('other', b'keep')
            plan = {'schema': 1, 'kind': 'experimental-exact-texture-offsets',
                    'template_sha256': module.file_hash(template),
                    'sources': {'sharedassets1.assets': module.file_hash(source / 'sharedassets1.assets')},
                    'records': [{'entry': entry, 'target_offset': 3, 'source': 'sharedassets1.assets',
                                 'source_offset': 2, 'size': 5, 'sha256': module.sha(b'ABCDE')}]}
            def restore():
                plan_file.write_text(json.dumps(plan))
                module.restore(SimpleNamespace(steam_data=source, template=template, plan=plan_file, output=output))
            restore()
            with zipfile.ZipFile(output) as z:
                self.assertEqual(z.read(entry), b'123ABCDE89')
                self.assertEqual(z.read('other'), b'keep')
            output.unlink()
            plan['records'][0]['sha256'] = '0' * 64
            with self.assertRaises(ValueError): restore()
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
