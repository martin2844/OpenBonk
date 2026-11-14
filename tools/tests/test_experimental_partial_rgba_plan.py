import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('partial_rgba', Path(__file__).resolve().parents[1] / 'experimental-partial-rgba-plan.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class PartialRgbaPlanTests(unittest.TestCase):
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
        for name in ('assets/bin/Data/' + 'a' * 32, 'assets/bin/Data/sharedassets1.assets.split0',
                     'assets/bin/Data/sharedassets2.assets.split17'):
            self.assertIsNotNone(module.ENTRY.fullmatch(name))
        for name in ('assets/bin/Data/sharedassets1.assets', 'assets/bin/Data/level1.split0',
                     'assets/bin/Data/sharedassets1.assets.split01', 'assets/bin/Data/../bad'):
            self.assertIsNone(module.ENTRY.fullmatch(name))

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
