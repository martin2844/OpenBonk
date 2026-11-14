import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('animation_fragments', Path(__file__).resolve().parents[1] / 'experimental-animation-fragment-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class AnimationFragmentTests(unittest.TestCase):
    def test_shared0_font_index_is_scoped_to_named_sdf_metadata(self):
        import UnityPy
        def obj(kind, name, offset, payload):
            return SimpleNamespace(type=SimpleNamespace(name=kind), byte_size=len(payload), byte_start=offset,
                                   path_id=offset + 1, peek_name=lambda: name, get_raw_data=lambda: payload)
        data = b'A' * 256 + b'B' * 256 + b'C' * 256
        objects = [obj('MonoBehaviour', 'NotoSansSC-Bold SDF', 0, data[:256]),
                   obj('MonoBehaviour', 'Other SDF', 256, data[256:512]),
                   obj('Material', 'NotoSansSC-Bold SDF', 512, data[512:])]
        with patch.object(UnityPy, 'load', return_value=SimpleNamespace(objects=objects)):
            index = module.shared0_font_index(data)
        self.assertEqual(list(index), [('NotoSansSC-Bold SDF', 256)])
        self.assertEqual(index[('NotoSansSC-Bold SDF', 256)][0][0], 'sharedassets0.assets')

    def test_fragment_index_separates_types_and_excludes_meshes(self):
        import UnityPy
        def obj(kind, offset, payload):
            return SimpleNamespace(type=SimpleNamespace(name=kind), byte_size=len(payload), byte_start=offset,
                                   path_id=offset + 1, get_raw_data=lambda: payload)
        material, component, mesh = (b'A' * 256, b'B' * 256, b'C' * 256)
        objects = [obj('Material', 0, material), obj('MonoBehaviour', 256, component), obj('Mesh', 512, mesh)]
        with patch.object(UnityPy, 'load', return_value=SimpleNamespace(objects=objects)):
            index = module.fragment_index({'sharedassets1.assets': material + component + mesh})
        self.assertEqual(index[('Material', 256)][0][3], material)
        self.assertEqual(index[('MonoBehaviour', 256)][0][3], component)
        self.assertNotIn(('Mesh', 256), index)

    def test_particle_selection_prefers_matching_blocks(self):
        target = b'A' * 64 + b'B' * 64
        wrong = ('sharedassets1.assets', 1, 0, b'C' * 128)
        best = ('sharedassets1.assets', 2, 128, b'A' * 64 + b'D' * 64)
        self.assertIs(module.best_fragment(target, [wrong, best]), best)
        self.assertIsNone(module.best_fragment(target, []))

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

    def test_scene_entry_names_are_bounded(self):
        for name in ('level1.split0', 'level2', 'sharedassets4.assets.split2', 'resources.assets'):
            self.assertIsNotNone(module.ENTRY.fullmatch(module.study.PREFIX + name))
        for name in ('level1.split01', '../level1', 'level1.untrusted', 'sharedassetsX.assets'):
            self.assertIsNone(module.ENTRY.fullmatch(module.study.PREFIX + name))

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
