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
