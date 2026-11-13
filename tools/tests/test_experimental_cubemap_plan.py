import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('cube_plan', Path(__file__).resolve().parents[1] / 'experimental-cubemap-plan.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class CubemapPlanTests(unittest.TestCase):
    def test_reconstructs_and_rejects_bad_conversion(self):
        with tempfile.TemporaryDirectory() as t:
            root = Path(t); source = root / 'steam'; source.mkdir()
            asset, resource = source / 'sharedassets1.assets', source / 'sharedassets1.assets.resS'
            asset.write_bytes(b'asset'); resource.write_bytes(b'xxsourceyy')
            decoder = root / 'decoder'; decoder.write_bytes(b'decoder')
            template, plan_file, output = (root / n for n in ('template.obb', 'plan.json', 'output.obb'))
            entry = 'assets/bin/Data/sharedassets1.assets.split0'; target = b'A' * (6 * 131072)
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr(entry, b'x' + bytes(len(target)) + b'y'); z.writestr('other', b'keep')
            row = {'id': 0, 'source': resource.name, 'source_offset': 2, 'source_size': 6,
                   'source_sha256': module.converted.sha(b'source'), 'source_format': 12,
                   'target_size': len(target), 'target_sha256': module.converted.sha(target),
                   'fragments': [{'entry': entry, 'target_offset': 1, 'converted_offset': 0, 'size': len(target)}]}
            plan = {'schema': 1, 'kind': 'experimental-converted-steam-cubemap-offsets',
                    'template_sha256': module.converted.file_hash(template),
                    'sources': {p.name: module.converted.file_hash(p) for p in (asset, resource)}, 'records': [row]}
            plan_file.write_text(json.dumps(plan)); args = SimpleNamespace(steam_data=source, template=template, plan=plan_file, decoder=decoder, runner=None, output=output)
            with patch.object(module, 'convert_cube', return_value=target): module.restore(args)
            with zipfile.ZipFile(output) as z:
                self.assertEqual(z.read(entry), b'x' + target + b'y'); self.assertEqual(z.read('other'), b'keep')
            output.unlink()
            with patch.object(module, 'convert_cube', return_value=bytes(len(target))), self.assertRaises(ValueError): module.restore(args)
            self.assertFalse(output.exists())
            row['fragments'][0]['converted_offset'] = 1; plan_file.write_text(json.dumps(plan))
            with self.assertRaises(ValueError): module.restore(args)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
