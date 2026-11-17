import importlib.util
import sys
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('partial_rgba', Path(__file__).resolve().parents[1] / 'experimental-partial-rgba-plan.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class PartialRgbaPlanTests(unittest.TestCase):
    def test_rgba_normal_mode_is_validated_and_applied(self):
        item = {'target_format': 4, 'source_format': 12, 'normal_rgba': True,
                'width': 1, 'height': 1, 'mips': 1}
        with patch.object(module, 'converted', return_value=bytes((0, 128, 0, 128))):
            self.assertEqual(module.converted_plan_texture(None, item, None), bytes((128, 128, 254, 255)))
        for update in ({'source_format': 4}, {'target_format': 45}, {'normal_rgba': 'yes'}, {'blend': 'AA=='}):
            with self.assertRaises(ValueError): module.normal_rgba_mode(dict(item, **update))

    def test_tiny_etc2_cli_requires_small_and_byte_modes(self):
        base = ['tool', 'generate', '--steam-data', '/unused', '--obb', '/unused', '--output', '/unused',
                '--etc2-quality-library', '/unused', '--include-tiny-etc2']
        with patch.object(sys, 'argv', base), self.assertRaises(SystemExit): module.main()
        with patch.object(sys, 'argv', base + ['--include-small-etc2']), self.assertRaises(SystemExit): module.main()
        with patch.object(sys, 'argv', base + ['--include-small-etc2', '--fast-etc2', '--blend-etc2', '--byte-etc2']), \
             patch.object(module, 'generate') as generate:
            module.main()
        self.assertTrue(generate.call_args.args[0].include_tiny_etc2)

    def test_small_alias_requires_clear_half_match_and_rejects_competitors(self):
        target = SimpleNamespace(m_TextureFormat=45, m_ImageCount=1, m_MipCount=1,
                                 m_ColorSpace=0, m_Width=8, m_Height=8)
        def source(ident):
            return SimpleNamespace(ident=ident, m_TextureFormat=10, m_ImageCount=1,
                                   m_MipCount=1, m_ColorSpace=0)
        one, two = ('sharedassets1', 1, source(1)), ('sharedassets1', 2, source(2))
        payload = b'A' * 64
        outputs = {1: b'A' * 40 + b'B' * 24, 2: b'A' * 19 + b'C' * 45}
        with patch.object(module, 'converted', side_effect=lambda src, *args: outputs[src.ident]):
            self.assertIsNone(module.partial_etc2_source(target, payload, [one, two], object()))
            self.assertEqual(module.partial_etc2_source(target, payload, [one, two], object(), True)[0], one)
            outputs[2] = b'A' * 20 + b'C' * 44
            self.assertIsNone(module.partial_etc2_source(target, payload, [one, two], object(), True))
            outputs[1] = b'A' * 31 + b'B' * 33
            self.assertIsNone(module.partial_etc2_source(target, payload, [one], object(), True))
            large = b'A' * 4000
            outputs[1] = b'A' * 1024 + b'B' * 2976
            outputs[2] = b'A' * 511 + b'C' * 3489
            self.assertEqual(module.partial_etc2_source(target, large, [one, two], object(), True)[0], one)
            outputs[2] = b'A' * 512 + b'C' * 3488
            self.assertIsNone(module.partial_etc2_source(target, large, [one, two], object(), True))

    def test_alias_cli_requires_tiny_mode(self):
        args = ['tool', 'generate', '--steam-data', '/unused', '--obb', '/unused', '--output', '/unused',
                '--etc2-quality-library', '/unused', '--include-etc2-aliases']
        with patch.object(sys, 'argv', args), self.assertRaises(SystemExit): module.main()
        args += ['--include-tiny-etc2', '--include-small-etc2', '--fast-etc2', '--blend-etc2', '--byte-etc2']
        with patch.object(sys, 'argv', args), patch.object(module, 'generate') as generate: module.main()
        self.assertTrue(generate.call_args.args[0].include_etc2_aliases)

    def test_block_blend_chooses_best_encoder_and_validates_mask(self):
        original = b'A' * 16 + b'B' * 16 + b'C' * 3
        quality = b'A' * 16 + b'X' * 16 + b'C' * 3
        fast = b'X' * 16 + b'B' * 16 + b'X' * 3
        output, choices = module.select_blocks(original, quality, fast)
        self.assertEqual(output, original)
        self.assertEqual(choices, b'\x02')
        self.assertEqual(module.blend_blocks(quality, fast, choices), original)
        with self.assertRaises(ValueError): module.blend_blocks(quality, fast, b'\x80')
        with self.assertRaises(ValueError): module.blend_blocks(quality, fast, b'')
        with self.assertRaises(ValueError): module.select_blocks(original, quality, b'bad')
        output, choices = module.select_blocks(b'ABC', b'AXC', b'XBX', 1)
        self.assertEqual((output, choices), (b'ABC', b'\x02'))
        self.assertEqual(module.blend_blocks(b'AXC', b'XBX', choices, 1), b'ABC')
        with self.assertRaises(ValueError): module.blend_blocks(b'ABC', b'ABC', b'\x80', 1)
        with self.assertRaises(ValueError): module.select_blocks(b'A', b'A', b'A', True)
        with self.assertRaises(ValueError): module.converted_plan_texture(None, {'target_format': 47, 'quality': True, 'blend': None}, None)
        with self.assertRaises(ValueError): module.converted_plan_texture(None, {'target_format': 47, 'quality': True, 'blend_unit': 1}, None)

    def test_fast_mode_is_selected_only_for_more_equal_nonzero_bytes(self):
        self.assertEqual(module.select_encoding(b'AB\0', b'Ax\0', b'ABx'), (b'ABx', False))
        self.assertEqual(module.select_encoding(b'AB\0', b'Ax\0', b'xB\0'), (b'Ax\0', True))
        with self.assertRaises(ValueError): module.select_encoding(b'AB', b'AB', b'A')
        self.assertTrue(module.quality_mode({'target_format': 45}))
        self.assertFalse(module.quality_mode({'target_format': 45, 'quality': False}))
        with self.assertRaises(ValueError): module.quality_mode({'target_format': 4, 'quality': True})
        with self.assertRaises(ValueError): module.quality_mode({'target_format': 47, 'quality': 'false'})

    def test_small_etc2_is_opt_in_and_still_requires_substantial_data(self):
        self.assertFalse(module.eligible_etc2_payload(b'A' * 4096))
        self.assertTrue(module.eligible_etc2_payload(b'A' * 4096, True))
        self.assertFalse(module.eligible_etc2_payload(b'A' * 1023 + bytes(3073), True))
        self.assertFalse(module.eligible_etc2_payload(b'A' * 4095, True))
        self.assertTrue(module.eligible_etc2_payload(b'A' * 65536))
        self.assertFalse(module.eligible_etc2_payload(b'A' * 63, True, True))
        self.assertFalse(module.eligible_etc2_payload(b'A' * 15 + bytes(49), True, True))
        self.assertTrue(module.eligible_etc2_payload(b'A' * 16 + bytes(48), True, True))

    def test_renamed_etc2_alpha_requires_unique_source_and_matching_bytes(self):
        target = SimpleNamespace(m_Name='light_0', m_TextureFormat=47, m_ImageCount=1,
                                 m_MipCount=1, m_ColorSpace=0, m_Width=256, m_Height=256)
        source = SimpleNamespace(m_TextureFormat=1, m_ImageCount=1, m_MipCount=1, m_ColorSpace=0)
        item = ('sharedassets1', 42, source); payload = b'A' * 65536; encoder = object()
        with patch.object(module.study, 'select_source', return_value=item) as select, \
                patch.object(module, 'converted', return_value=payload[:32768] + b'B' * 32768) as convert:
            self.assertEqual(module.etc2_alpha_alias(target, payload, [item], encoder)[0], item)
            convert.return_value = b'B' * len(payload)
            self.assertIsNone(module.etc2_alpha_alias(target, payload, [item], encoder))
            select.return_value = None
            self.assertIsNone(module.etc2_alpha_alias(target, payload, [item], encoder))
            self.assertIsNone(module.etc2_alpha_alias(target, payload, [item], None))

    def test_renamed_rgba_requires_near_exact_unambiguous_candidate(self):
        target = SimpleNamespace(m_Name='Scorpion_0', m_TextureFormat=4, m_ImageCount=1,
                                 m_MipCount=1, m_ColorSpace=0, m_Width=128, m_Height=128)
        source = SimpleNamespace(m_TextureFormat=4, m_ImageCount=1, m_MipCount=1, m_ColorSpace=0)
        good, bad, other = ('sharedassets1', 1, source), ('sharedassets1', 2, source), ('sharedassets1', 3, source)
        payload = b'A' * 65536
        encoded = {1: payload, 2: b'B' * len(payload), 3: payload[:-1] + b'B'}
        def convert(src, *args): return encoded[src.ident]
        good[2].ident = 1
        bad = (bad[0], bad[1], SimpleNamespace(**{**vars(source), 'ident': 2}))
        other = (other[0], other[1], SimpleNamespace(**{**vars(source), 'ident': 3}))
        with patch.object(module, 'converted', side_effect=convert):
            self.assertEqual(module.rgba_alias(target, payload, [bad, good])[0], good)
            self.assertIsNone(module.rgba_alias(target, payload, [good, other]))
            self.assertIsNone(module.rgba_alias(target, payload, [bad]))
            target.m_Name = 'Scorpion'
            self.assertIsNone(module.rgba_alias(target, payload, [good]))

    def test_supported_entries_match_android_restorer(self):
        for name in ('assets/bin/Data/' + 'a' * 32, 'assets/bin/Data/sharedassets0.assets.split0', 'assets/bin/Data/sharedassets1.assets.split0',
                     'assets/bin/Data/sharedassets2.assets.split17', 'assets/bin/Data/sharedassets4.assets.split0'):
            self.assertIsNotNone(module.ENTRY.fullmatch(name))
        for name in ('assets/bin/Data/sharedassets1.assets', 'assets/bin/Data/level1.split0',
                     'assets/bin/Data/sharedassets1.assets.split01', 'assets/bin/Data/../bad'):
            self.assertIsNone(module.ENTRY.fullmatch(name))

    def test_shared4_candidates_do_not_change_other_groups(self):
        target = SimpleNamespace(m_Width=128, m_Height=128)
        other = ('sharedassets1', 1, object()); fourth = ('sharedassets4', 2, object()); zero = ('sharedassets0', 3, object())
        index = {('same', 128, 128): [other, fourth, zero]}
        self.assertEqual(module.candidate_sources(index, target, 'sharedassets1.assets', 'same'), [other])
        self.assertEqual(module.candidate_sources(index, target, 'sharedassets4.assets', 'same'), [fourth])
        self.assertEqual(module.candidate_sources(index, target, 'sharedassets0.assets', 'same'), [zero])
        self.assertEqual(module.candidate_sources(index, target, 'a' * 32, 'same'), [other])

    def test_small_renamed_rgba_requires_exact_bytes(self):
        target = SimpleNamespace(m_Name='icon_0', m_TextureFormat=4, m_ImageCount=1,
                                 m_MipCount=1, m_ColorSpace=0, m_Width=2, m_Height=2)
        source = SimpleNamespace(m_TextureFormat=4, m_ImageCount=1, m_MipCount=1, m_ColorSpace=0)
        item = ('sharedassets1', 1, source); payload = b'A' * 15 + b'\0'
        with patch.object(module, 'converted', return_value=payload) as convert:
            self.assertEqual(module.rgba_alias(target, payload, [item])[0], item)
            convert.return_value = b'A' * 16
            self.assertIsNone(module.rgba_alias(target, payload, [item]))
            self.assertIsNone(module.rgba_alias(target, bytes(16), [item]))

    def test_partial_etc2_selects_clear_match_and_rejects_ambiguity(self):
        target = SimpleNamespace(m_TextureFormat=45, m_ImageCount=1, m_MipCount=1,
                                 m_ColorSpace=0, m_Width=64, m_Height=64)
        def source(ident): return SimpleNamespace(ident=ident, m_TextureFormat=4,
                                                   m_ImageCount=1, m_MipCount=1, m_ColorSpace=0)
        first, second = ('sharedassets1', 1, source(1)), ('sharedassets1', 2, source(2))
        payload = b'A' * 4096
        outputs = {1: b'A' * 2048 + b'B' * 2048, 2: b'A' * 256 + b'C' * 3840}
        with patch.object(module, 'converted', side_effect=lambda s, *args: outputs[s.ident]):
            self.assertEqual(module.partial_etc2_source(target, payload, [first, second], object())[0], first)
            outputs[2] = b'A' * 1100 + b'C' * 2996
            self.assertIsNone(module.partial_etc2_source(target, payload, [first, second], object()))
            outputs[1] = outputs[2] = b'A' * 1000 + b'B' * 3096
            self.assertIsNone(module.partial_etc2_source(target, payload, [first, second], object()))
            self.assertIsNone(module.partial_etc2_source(target, payload, [first], None))

    def test_etc2_rgb_uses_color_blocks_and_rejects_bad_lengths(self):
        source = SimpleNamespace(m_TextureFormat=10, get_image_data=lambda: b'compressed')
        encoder = SimpleNamespace(encode=lambda data, w, h, fmt: b'A' * 8 + b'B' * 8 + b'C' * 8 + b'D' * 8)
        with patch.object(module.study, 'mip_slices', return_value=[(8, 4, b'input')]):
            self.assertEqual(module.converted(source, 8, 4, 1, 45, encoder), b'B' * 8 + b'D' * 8)
            self.assertEqual(module.converted(source, 8, 4, 1, 47, encoder), b'A' * 8 + b'B' * 8 + b'C' * 8 + b'D' * 8)
            with self.assertRaises(ValueError): module.converted(source, 8, 4, 1, 45)
            encoder.encode = lambda *args: b'short'
            with self.assertRaises(ValueError): module.converted(source, 8, 4, 1, 45, encoder)

    def test_split_fragments_preserve_different_and_zero_bytes(self):
        parts = [SimpleNamespace(filename='one', file_size=5), SimpleNamespace(filename='two', file_size=5)]
        original = b'A\x00CDE'; converted = b'AxCDz'
        rows = module.fragment_rows(parts, 3, original, converted, 0)
        self.assertEqual(sum(row['masked_bytes'] for row in rows), 3)
        converted_by_id = {0: converted}
        first, second = b'123' + original[:2], original[2:] + b'89'
        masked_first = module.apply(first, rows[:1], converted_by_id)
        masked_second = module.apply(second, rows[1:], converted_by_id)
        self.assertEqual(masked_first, b'123\x00\x00')
        self.assertEqual(masked_second, b'\x00\x00E89')
        self.assertEqual(module.apply(masked_first, rows[:1], converted_by_id, True), first)
        self.assertEqual(module.apply(masked_second, rows[1:], converted_by_id, True), second)

    def test_rejects_changed_converted_bytes_or_template(self):
        parts = [SimpleNamespace(filename='one', file_size=5)]
        rows = module.fragment_rows(parts, 0, b'ABCDE', b'ABCxy', 0)
        masked = module.apply(b'ABCDE', rows, {0: b'ABCxy'})
        with self.assertRaises(ValueError): module.apply(masked, rows, {0: b'XBCxy'}, True)
        with self.assertRaises(ValueError): module.apply(b'X' + masked[1:], rows, {0: b'ABCxy'}, True)
        with self.assertRaises(ValueError): module.apply(masked, rows + rows, {0: b'ABCxy'}, True)

    def test_rejects_nonzero_padding_and_out_of_bounds(self):
        parts = [SimpleNamespace(filename='one', file_size=5)]
        rows = module.fragment_rows(parts, 0, b'ABCDE', b'ABCxy', 0)
        bad = dict(rows[0], mask='/w==')
        with self.assertRaises(ValueError): module.apply(b'ABCDE', [bad], {0: b'ABCxy'})
        with self.assertRaises(ValueError): module.fragment_rows(parts, 3, b'ABCD', b'ABCD', 0)

    def test_android_plan_binds_verified_stream_offsets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); source = root / 'source'; source.mkdir()
            (source / 'sharedassets1.assets.resS').write_bytes(b'xxpayloadyy')
            template = root / 'template.obb'; template.write_bytes(b'template')
            stream = SimpleNamespace(path='sharedassets1.assets.resS', offset=2, size=7)
            tex = SimpleNamespace(m_Width=1, m_Height=1, m_MipCount=1, m_TextureFormat=4,
                                  m_StreamData=stream, get_image_data=lambda: b'payload')
            item = {'id': 0, 'source_group': 'sharedassets1', 'source_id': 42, 'width': 1, 'height': 1,
                    'mips': 1, 'source_format': 4, 'source_sha256': module.sha(b'payload'),
                    'converted_sha256': module.sha(b'rgba')}
            row = {'texture_id': 0, 'entry': 'assets/bin/Data/sharedassets1.assets.split0', 'target_offset': 0,
                   'size': 4, 'converted_offset': 0, 'mask': 'Dw==', 'masked_bytes': 4,
                   'original_sha256': module.sha(b'rgba'), 'masked_sha256': module.sha(bytes(4))}
            plan = root / 'plan.json'; plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-partial-rgba',
                'template_sha256': module.masker.file_digest(template), 'textures': [item], 'records': [row]}))
            output = root / 'android.json'; args = SimpleNamespace(steam_data=source, template=template, plan=plan, output=output)
            with patch.object(module, 'sources', return_value=({}, {('sharedassets1', 42): tex})), patch.object(module, 'converted', return_value=b'rgba'):
                module.export_android(args)
                result = json.loads(output.read_text()); record = result['records'][0]
                self.assertEqual((record['source'], record['source_offset'], record['source_size']), ('sharedassets1.assets.resS', 2, 7))
                self.assertEqual(result['sources'][record['source']], module.masker.file_digest(source / record['source']))
                self.assertNotIn('texture_id', record['fragments'][0])
                output.unlink(); stream.offset = 3
                with self.assertRaises(ValueError): module.export_android(args)
                self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
