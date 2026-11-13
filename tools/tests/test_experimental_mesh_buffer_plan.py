import importlib.util
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('mesh_plan', Path(__file__).resolve().parents[1] / 'experimental-mesh-buffer-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class MeshPlanTests(unittest.TestCase):
    def test_fragment_crosses_split_and_reconstructs(self):
        parts = [SimpleNamespace(filename='one', file_size=5), SimpleNamespace(filename='two', file_size=5)]
        rows = module.fragments(parts, 3, b'ABCDE', 'sharedassets1.assets', 2)
        self.assertEqual([(r['target_offset'], r['source_offset'], r['size']) for r in rows], [(3, 2, 2), (0, 4, 3)])
        streams = {'sharedassets1.assets': io.BytesIO(b'xxABCDEyy')}
        for old, row in ((b'123AB', rows[0]), (b'CDE89', rows[1])):
            self.assertEqual(module.apply(module.apply(old, [row]), [row], streams), old)

    def test_rejects_overlapping_ranges_and_unsafe_source(self):
        row = {'target_offset': 0, 'source_offset': 0, 'size': 1, 'source': 'sharedassets1.assets', 'sha256': module.sha(b'a')}
        with self.assertRaises(ValueError): module.apply(b'ab', [row, dict(row)])
        row['source'] = '../secret'
        with self.assertRaises(ValueError): module.apply(b'ab', [row])

    def test_round_trip_and_tampered_hash_cleanup(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root / 'steam'; source.mkdir(); (source / 'level1.resS').write_bytes(b'xxABCDEyy')
            template, plan_file, output = (root / n for n in ('template.obb', 'plan.json', 'output.obb'))
            entry = module.study.PREFIX + 'sharedassets1.assets.split0'
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr(entry, b'123' + bytes(5) + b'89'); z.writestr('other', b'keep')
            row = {'entry': entry, 'target_offset': 3, 'source': 'level1.resS', 'source_offset': 2, 'size': 5, 'sha256': module.sha(b'ABCDE')}
            plan = {'schema': 1, 'kind': 'experimental-steam-mesh-buffer-offsets', 'template_sha256': module.file_hash(template),
                    'sources': {'level1.resS': module.file_hash(source / 'level1.resS')}, 'records': [row]}
            def restore():
                plan_file.write_text(json.dumps(plan))
                module.restore(SimpleNamespace(steam_data=source, template=template, plan=plan_file, output=output))
            restore()
            with zipfile.ZipFile(output) as z:
                self.assertEqual(z.read(entry), b'123ABCDE89'); self.assertEqual(z.read('other'), b'keep')
            output.unlink(); row['sha256'] = '0' * 64
            with self.assertRaises(ValueError): restore()
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
