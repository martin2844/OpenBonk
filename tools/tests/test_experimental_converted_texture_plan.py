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
    def test_emission_record_requires_supported_target_and_source_hash(self):
        row = {'emission_source': True, 'group': 'sharedassets1', 'name': 'Emission_0', 'source_name': 'Emission',
               'source_sha256': 'ab' * 32, 'source_format': 10, 'target_format': 4, 'bytes': 87380,
               'normal': False, 'quality': False, 'direct_normal': False}
        self.assertTrue(module.valid_emission_record(row))
        self.assertTrue(module.valid_emission_record(dict(row, group='a' * 32, name='Emission')))
        for change in ({'name': 'other'}, {'source_sha256': 'bad'}, {'quality': True}, {'target_format': 47}, {'shadow': True}):
            self.assertFalse(module.valid_emission_record(dict(row, **change)))

    def test_lightmap_alias_requires_known_identity_and_format(self):
        row = {'lightmap_alias': True, 'group': 'sharedassets1', 'name': 'LDR_LLL1_4_0',
               'source_name': 'LDR_LLL1_4', 'source_format': 1, 'target_format': 47,
               'bytes': 65536, 'normal': False, 'quality': True, 'direct_normal': False}
        self.assertTrue(module.valid_lightmap_record(row))
        for change in ({'name': 'LDR_LLL1_2_0'}, {'source_name': 'LDR_LLL1_5'}, {'quality': False},
                       {'group': 'sharedassets2'}, {'bytes': 16}, {'shadow': True}):
            self.assertFalse(module.valid_lightmap_record(dict(row, **change)))

    def test_shared0_sdf_record_is_scoped_to_known_atlases(self):
        row = {'shared0_sdf': True, 'group': 'sharedassets0', 'name': 'NotoSansSC-Bold SDF Atlas 19',
               'source_format': 1, 'target_format': 47, 'bytes': 1048576,
               'normal': False, 'quality': False, 'direct_normal': False}
        self.assertTrue(module.valid_shared0_sdf_record(row))
        self.assertTrue(module.valid_shared0_sdf_record(dict(row, quality=True)))
        for change in ({'group': 'sharedassets1'}, {'name': 'Other SDF Atlas'}, {'source_format': 12},
                       {'bytes': 100}, {'quality': 'yes'}, {'source_name': 'other'}, {'shared0_sdf': False}):
            with self.subTest(change=change): self.assertFalse(module.valid_shared0_sdf_record(dict(row, **change)))

    def test_forest_record_requires_known_name_and_source_hash(self):
        row = {'forest_source': True, 'group': 'sharedassets1', 'name': 'Forest1_TopGrass_0',
               'source_name': 'Forest1_TopGrass', 'source_sha256': 'ab' * 32,
               'source_format': 10, 'target_format': 45, 'normal': False, 'quality': False, 'direct_normal': False}
        self.assertTrue(module.valid_forest_record(row))
        for change in ({'group': 'sharedassets2'}, {'source_name': 'other'}, {'source_sha256': 'invalid'},
                       {'target_format': 47}, {'quality': True}, {'shadow': True}, {'name': 'other'}):
            self.assertFalse(module.valid_forest_record(dict(row, **change)))
        for name in ('FillMiddle_0', 'FillMiddle_1', 'FillMiddle_2'):
            variant = dict(row, name=name, source_name='FillMiddle')
            self.assertTrue(module.valid_forest_record(variant))
            self.assertFalse(module.valid_forest_record(dict(variant, source_name='Forest1_TopGrass')))

    def test_collision_mapping_is_limited_to_known_texture_and_format(self):
        row = {'collision_alias': True, 'group': 'sharedassets1', 'name': 'bush__openbonk_8cd6c6a0',
               'source_name': 'bush', 'source_format': 3, 'target_format': 45,
               'normal': False, 'quality': False, 'direct_normal': False}
        self.assertTrue(module.valid_collision_record(row))
        for change in ({'group': 'sharedassets2'}, {'source_name': 'Bush'}, {'name': 'skull__openbonk_8cd6c6a0'},
                       {'source_format': 4}, {'target_format': 47}, {'quality': True}, {'alias_group': 'sharedassets1'}):
            self.assertFalse(module.valid_collision_record(dict(row, **change)))
        retaliation = dict(row, name='retaliation__openbonk_56e12a7d', source_name='retaliation', source_format=12, target_format=47)
        self.assertTrue(module.valid_collision_record(retaliation))
        self.assertFalse(module.valid_collision_record(dict(retaliation, source_format=3)))
    def test_shadow_mapping_requires_specific_quality_texture(self):
        row = {'shadow': True, 'group': 'sharedassets1', 'name': 'DropShadow', 'source_format': 12,
               'target_format': 47, 'quality': True, 'normal': False, 'direct_normal': False}
        self.assertTrue(module.valid_shadow_record(row))
        self.assertTrue(module.valid_shadow_record(dict(row, name='DropShadow_0')))
        for change in ({'group': 'sharedassets2'}, {'quality': False}, {'name': 'other'},
                       {'source_format': 4}, {'alias_group': 'sharedassets1'}, {'shadow': False}):
            with self.subTest(change=change): self.assertFalse(module.valid_shadow_record(dict(row, **change)))

    def test_resource_mapping_only_accepts_known_quality_textures(self):
        row = {'name': 'GridBox_Collider', 'source_name': 'GridBox_Collider', 'source_format': 12,
               'target_format': 47, 'quality': True, 'normal': False, 'direct_normal': False}
        self.assertTrue(module.valid_resource_record(row))
        for change in ({'name': 'other'}, {'source_name': 'other'}, {'source_format': 10},
                       {'target_format': 45}, {'quality': False}, {'normal': True}, {'font': True},
                       {'alias_group': 'sharedassets1'}):
            self.assertFalse(module.valid_resource_record(dict(row, **change)))
        default = dict(row, name='GridBox_Default', source_name='GridBox_Default', source_format=10, target_format=45)
        self.assertTrue(module.valid_resource_record(default))
        nodraw = dict(default, name='GridBox_NoDraw', source_name='GridBox_NoDraw')
        self.assertTrue(module.valid_resource_record(nodraw))
        self.assertFalse(module.valid_resource_record(dict(default, source_format=12)))
        self.assertFalse(module.valid_resource_record(dict(default, target_format=47)))
        emoji = dict(row, name='psd_EmojiOne', source_name='psd_EmojiOne')
        self.assertTrue(module.valid_resource_record(emoji))
        self.assertFalse(module.valid_resource_record(dict(emoji, source_name='psd_EmojiOne2')))

    def test_quality_etc2_rgb_keeps_color_block_and_checks_size(self):
        class Encoder:
            def encode(self, data, width, height, source_format):
                return b'ALPHA123COLOR456'
        row = {'source_format': 4, 'target_format': 45, 'width': 4, 'height': 4, 'mips': 1,
               'quality': True, 'target_size': 8, 'target_sha256': module.sha(b'COLOR456')}
        self.assertEqual(module.convert_texture(bytes(64), row, Encoder()), b'COLOR456')
        class ShortEncoder:
            def encode(self, *args): return b'bad'
        with self.assertRaisesRegex(ValueError, 'quality block size'):
            module.convert_texture(bytes(64), row, ShortEncoder())

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

    def test_restores_shared0_source_and_split(self):
        source = self.source / 'sharedassets0.assets'
        source.write_bytes(b'__source')
        entry = 'assets/bin/Data/sharedassets0.assets.split0'
        with zipfile.ZipFile(self.template, 'w') as z: z.writestr(entry, b'abc' + bytes(5))
        self.row.update(source='sharedassets0.assets', fragments=[{'entry': entry, 'target_offset': 3, 'size': 5, 'converted_offset': 0}])
        self.plan.update(template_sha256=module.file_hash(self.template), sources={'sharedassets0.assets': module.file_hash(source)})
        self.restore()
        with zipfile.ZipFile(self.output) as z: self.assertEqual(z.read(entry), b'abcABCDE')

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

    def test_direct_normal_rejects_unsupported_encoding(self):
        self.row.update(direct_normal=True, source_format=12, target_format=45)
        with self.assertRaises(ValueError): self.restore()
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
