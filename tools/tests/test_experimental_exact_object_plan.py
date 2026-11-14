import importlib.util
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('exact_objects', Path(__file__).resolve().parents[1] / 'experimental-exact-object-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ExactObjectTests(unittest.TestCase):
    def test_font_signature_requires_exact_named_nonempty_payload(self):
        font = SimpleNamespace(m_Name='Example Font')
        payload = b'font-data' * 50
        signature = module.font_signature(font, payload)
        self.assertIsNotNone(signature)
        self.assertNotEqual(signature, module.font_signature(font, payload[:-1] + b'!'))
        self.assertIsNone(module.font_signature(font, bytes(450)))
        self.assertIsNone(module.font_signature(font, b'short'))
        font.m_Name = ''
        self.assertIsNone(module.font_signature(font, payload))

    def test_texture_signature_requires_matching_metadata_and_exact_payload(self):
        tex = SimpleNamespace(m_Name='AreaTex', m_Width=16, m_Height=16, m_MipCount=1,
                              m_ImageCount=1, m_ColorSpace=0, m_TextureFormat=3)
        payload = b'\x01\x02\x03\x04' * 100
        signature = module.texture_signature(tex, payload)
        self.assertIsNotNone(signature)
        self.assertNotEqual(signature, module.texture_signature(tex, payload[:-1] + b'\x05'))
        tex.m_ColorSpace = 1
        self.assertNotEqual(signature, module.texture_signature(tex, payload))
        tex.m_ImageCount = 2
        self.assertIsNone(module.texture_signature(tex, payload))
        self.assertIsNone(module.texture_signature(tex, bytes(400)))

    def test_texture_stream_paths_are_narrowly_accepted(self):
        for name in ('sharedassets1.assets.resS', 'resources.assets.resS'):
            self.assertIsNotNone(module.SOURCE.fullmatch(name))
        for name in ('../sharedassets1.assets.resS', 'sharedassets1.resource.resS', 'dir/sharedassets1.assets.resS'):
            self.assertIsNone(module.SOURCE.fullmatch(name))
        payload = b'texture-payload'
        row = {'target_offset': 2, 'size': len(payload), 'source': 'sharedassets1.assets.resS',
               'source_offset': 3, 'sha256': module.sha(payload)}
        original = b'xx' + payload + b'yy'
        masked = module.apply(original, [row])
        self.assertEqual(module.apply(masked, [row], {'sharedassets1.assets.resS': io.BytesIO(b'---' + payload)}), original)
        with self.assertRaises(ValueError): module.apply(masked, [row], {'sharedassets1.assets.resS': io.BytesIO(b'---' + bytes(len(payload)))})

    def test_localization_bundle_round_trip_and_path_validation(self):
        source_name = 'StreamingAssets/aa/StandaloneLinux64/localization-string-tables-english(en)_assets_all.bundle'
        entry = 'assets/aa/StandaloneLinux64/' + Path(source_name).name
        payload = b'localization bytes'
        row = {'entry': entry, 'source': source_name, 'source_offset': 0, 'target_offset': 0,
               'size': len(payload), 'sha256': module.sha(payload)}
        self.assertIsNotNone(module.SOURCE.fullmatch(source_name))
        self.assertIsNotNone(module.BUNDLE_ENTRY.fullmatch(entry))
        self.assertIsNone(module.SOURCE.fullmatch('StreamingAssets/aa/StandaloneLinux64/../../other.bundle'))
        masked = module.apply(payload, [row])
        self.assertEqual(module.apply(masked, [row], {source_name: io.BytesIO(payload)}), payload)

    def test_windows_bundle_fragments_preserve_platform_differences(self):
        name = 'StreamingAssets/aa/StandaloneWindows64/localization-string-tables-english(en)_assets_all.bundle'
        target = bytes(range(1, 193)); source = bytearray(target); source[80] ^= 255
        self.assertIsNotNone(module.SOURCE.fullmatch(name))
        matches = list(module.common_fragments(target, [(name, 0, bytes(source))]))
        rows = [{'target_offset': offset, 'size': len(payload), 'source': source_name,
                 'source_offset': source_offset, 'sha256': module.sha(payload)}
                for offset, payload, source_name, source_offset in matches]
        self.assertTrue(rows)
        masked = module.apply(target, rows)
        self.assertEqual(masked[80], target[80])
        self.assertEqual(module.apply(masked, rows, {name: io.BytesIO(source)}), target)

    def test_shader_fragments_copy_exact_bytes_from_different_offsets(self):
        shared = bytes(range(1, 97))
        target = b'TARG' * 5 + shared + b'last'
        source = b'SRC!' * 3 + shared + b'other'
        matches = list(module.common_fragments(target, [('resources.assets', 100, source)]))
        self.assertEqual(matches, [(20, shared, 'resources.assets', 112)])
        self.assertEqual(list(module.common_fragments(bytes(128), [('resources.assets', 0, bytes(128))])), [])

    def test_source_files_are_available_in_both_minimal_launcher_zips(self):
        with tempfile.TemporaryDirectory() as t:
            manifest = Path(t) / 'inputs.json'
            manifest.write_text(json.dumps({'platforms': {
                'linux': {'level0': {}, 'level2': {}, 'other': {}},
                'windows': {'level2': {}, 'level3': {}, 'other': {}},
            }}))
            with patch.object(module, 'STEAM_INPUTS', manifest):
                self.assertEqual(module.launcher_source_names(), {'level2'})

    def test_fragments_preserve_split_boundaries(self):
        parts = [SimpleNamespace(filename='one', file_size=5), SimpleNamespace(filename='two', file_size=5)]
        rows = module.fragments(parts, 3, b'ABCDE', 'resources.assets', 2)
        self.assertEqual([(r['target_offset'], r['source_offset'], r['size']) for r in rows], [(3, 2, 2), (0, 4, 3)])
        streams = {'resources.assets': io.BytesIO(b'xxABCDEyy')}
        for old, row in ((b'123AB', rows[0]), (b'CDE89', rows[1])):
            self.assertEqual(module.apply(module.apply(old, [row]), [row], streams), old)
        with self.assertRaises(ValueError): module.apply(b'123AB', [rows[0], dict(rows[0])])

    def test_restoration_and_corruption_cleanup(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t); source = root / 'steam'; source.mkdir(); (source / 'resources.assets').write_bytes(b'xxABCDEyy')
            template, plan_file, output = (root / n for n in ('template.obb', 'plan.json', 'restored.obb'))
            entry = module.PREFIX + 'level1.split0'
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr(entry, b'123' + bytes(5) + b'89'); z.writestr('other', b'keep')
            row = {'entry': entry, 'target_offset': 3, 'source': 'resources.assets', 'source_offset': 2, 'size': 5, 'sha256': module.sha(b'ABCDE')}
            plan = {'schema': 1, 'kind': 'experimental-exact-steam-object-offsets', 'template_sha256': module.file_hash(template),
                    'sources': {'resources.assets': module.file_hash(source / 'resources.assets')}, 'records': [row]}
            args = SimpleNamespace(steam_data=source, template=template, plan=plan_file, output=output)
            plan_file.write_text(json.dumps(plan)); module.restore(args)
            with zipfile.ZipFile(output) as z:
                self.assertEqual(z.read(entry), b'123ABCDE89'); self.assertEqual(z.read('other'), b'keep')
            output.unlink(); row['sha256'] = '0' * 64; plan_file.write_text(json.dumps(plan))
            with self.assertRaises(ValueError): module.restore(args)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
