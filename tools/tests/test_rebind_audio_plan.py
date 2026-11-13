import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('rebind_audio', Path(__file__).resolve().parents[1] / 'rebind-audio-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class RebindTests(unittest.TestCase):
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
            with zipfile.ZipFile(canonical, 'w') as z: z.writestr('entry', b'changed')
            with self.assertRaises(ValueError): module.rebind(plan, original, canonical, output)
            self.assertFalse(output.exists())


if __name__ == '__main__':
    unittest.main()
