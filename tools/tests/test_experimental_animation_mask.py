import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'experimental-animation-mask.py'
SPEC = importlib.util.spec_from_file_location('experimental_animation_mask', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class MaskTest(unittest.TestCase):
    def test_equal_bytes_round_trip_without_copying_differences(self):
        source = bytes([0, 7, 8, 9, 1, 2, 3, 4, 5])
        target = bytes([0, 7, 3, 9, 1, 0, 3, 4, 5])
        masked, bits, count = MODULE.mask_equal(target, source)
        self.assertEqual(count, 6)
        self.assertEqual(masked[2], 3)
        self.assertEqual(MODULE.restore_masked(masked, source, bits), target)

    def test_rejects_invalid_mask_and_modified_bytes(self):
        for target, source, bits in ((b'\0', b'\1', b'\2'), (b'\1', b'\1', b'\1'),
                                     (b'\0', b'\0', b'\1'), (b'\0', b'\1', b'')):
            with self.assertRaises(ValueError):
                MODULE.restore_masked(target, source, bits)

    def test_restore_rejects_tampered_archive_before_creating_output(self):
        with tempfile.TemporaryDirectory(prefix='openbonk-mask-test-') as tmp:
            root = Path(tmp)
            source = root / 'steam'
            source.mkdir()
            for name in MODULE.SOURCE_NAMES:
                (source / name).write_bytes(b'fixture')
            template = root / 'template.zip'
            with zipfile.ZipFile(template, 'w') as z:
                z.writestr('fixture', b'value')
            plan = root / 'plan.json'
            doc = {'schema': 1, 'kind': 'experimental-animation-equal-byte-mask',
                   'sources': {name: MODULE.digest(b'fixture') for name in MODULE.SOURCE_NAMES},
                   'template_sha256': '0' * 64, 'records': [{'entry': 'fixture'}]}
            plan.write_text(json.dumps(doc))
            out = root / 'restored.zip'
            with self.assertRaisesRegex(ValueError, 'template archive differs'):
                MODULE.restore(SimpleNamespace(steam_data=source, template=template, plan=plan, output=out))
            self.assertFalse(out.exists())


if __name__ == '__main__':
    unittest.main()
