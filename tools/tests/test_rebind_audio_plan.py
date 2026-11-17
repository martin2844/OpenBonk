import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('rebind_audio', Path(__file__).resolve().parents[1] / 'rebind-audio-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RebindTests(unittest.TestCase):
    def test_localization_exception_is_scoped_and_never_rebinds_modified_entries(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t); original, canonical, plan, output = (root / n for n in ('a.zip', 'b.zip', 'plan.json', 'out.json'))
            locale = 'assets/aa/Android/localization-string-tables-english(en)_assets_all.bundle'
            with zipfile.ZipFile(original, 'w') as z: z.writestr(locale, b'compressed')
            with zipfile.ZipFile(canonical, 'w') as z: z.writestr(locale, b'normalized')
            obj = {'schema': 1, 'kind': 'experimental-partial-rgba-texture-offsets', 'template_sha256': module.hash_file(original), 'records': [{'fragments': [{'entry': 'assets/bin/Data/sharedassets1.assets.split0'}]}]}
            plan.write_text(json.dumps(obj))
            with self.assertRaises(ValueError): module.rebind(plan, original, canonical, output)
            with patch.object(module, 'localization_equivalent', return_value=False):
                with self.assertRaises(ValueError): module.rebind(plan, original, canonical, output, True)
            with patch.object(module, 'localization_equivalent', return_value=True):
                module.rebind(plan, original, canonical, output, True)
                self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
                output.unlink()
                obj['records'][0]['fragments'][0]['entry'] = locale
                plan.write_text(json.dumps(obj))
                with self.assertRaisesRegex(ValueError, 'modifies Android localization'): module.rebind(plan, original, canonical, output, True)
                self.assertFalse(output.exists())

    def test_only_rebinds_entry_identical_archive(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t)
            original, canonical, plan, output = (root / n for n in ('one.zip', 'two.zip', 'plan.json', 'new.json'))
            with zipfile.ZipFile(original, 'w', zipfile.ZIP_STORED) as z: z.writestr('entry', b'content')
            with zipfile.ZipFile(canonical, 'w', zipfile.ZIP_DEFLATED) as z: z.writestr('entry', b'content')
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-steam-audio-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-packed-steam-audio-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-steam-mesh-buffer-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-cubemap-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-animation-fragment-mask', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-exact-steam-object-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-partial-rgba-texture-offsets', 'template_sha256': module.hash_file(original)}))
            module.rebind(plan, original, canonical, output)
            self.assertEqual(json.loads(output.read_text())['template_sha256'], module.hash_file(canonical))
            output.unlink()
            with zipfile.ZipFile(canonical, 'w') as z: z.writestr('entry', b'changed')
            with self.assertRaises(ValueError): module.rebind(plan, original, canonical, output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
