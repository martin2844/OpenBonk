import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('animation_fragments', Path(__file__).resolve().parents[1] / 'experimental-animation-fragment-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class AnimationFragmentTests(unittest.TestCase):
    def test_split_mask_round_trip_and_overlap_rejection(self):
        parts = [SimpleNamespace(filename='one', file_size=5), SimpleNamespace(filename='two', file_size=5)]
        target, source = b'ABCDE', b'ABxDE'
        rows = module.make_fragments(parts, 3, target, 'sharedassets1.assets', 2, source)
        self.assertEqual(len(rows), 2)
        sources = {'sharedassets1.assets': b'xx' + source}
        for raw, row in ((b'123AB', rows[0]), (b'CDE89', rows[1])):
            masked = module.apply(raw, [row], sources)
            self.assertEqual(module.apply(masked, [row], sources, restore=True), raw)
        with self.assertRaises(ValueError): module.apply(b'123AB', [rows[0], dict(rows[0])], sources)

    def test_restore_validates_masks_and_cleans_partial_output(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root / 'steam'; source.mkdir()
            (source / 'sharedassets1.assets').write_bytes(b'xxABCDE')
            (source / 'sharedassets2.assets').write_bytes(b'other')
            entry = module.study.PREFIX + 'sharedassets1.assets.split0'
            part = SimpleNamespace(filename=entry, file_size=10)
            rows = module.make_fragments([part], 3, b'ABCDE', 'sharedassets1.assets', 2, b'ABCDE')
            sources = module.animation.read_sources(source)
            template, plan_file, output = (root / n for n in ('template.obb', 'plan.json', 'restored.obb'))
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr(entry, module.apply(b'123ABCDE89', rows, sources)); z.writestr('other', b'keep')
            plan = {'schema': 1, 'kind': 'experimental-animation-fragment-mask',
                    'template_sha256': module.animation.file_digest(template),
                    'sources': {name: module.animation.digest(data) for name, data in sources.items()}, 'records': rows}
            args = SimpleNamespace(steam_data=source, template=template, plan=plan_file, output=output)
            plan_file.write_text(json.dumps(plan)); module.restore(args)
            with zipfile.ZipFile(output) as z:
                self.assertEqual(z.read(entry), b'123ABCDE89'); self.assertEqual(z.read('other'), b'keep')
            output.unlink(); rows[0]['masked_bytes'] += 1; plan_file.write_text(json.dumps(plan))
            with self.assertRaises(ValueError): module.restore(args)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
