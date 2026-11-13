import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'check-etc2-quality-android.py'
SPEC = importlib.util.spec_from_file_location('etc2_quality_check', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
QUALITY_SPEC = importlib.util.spec_from_file_location('quality_texture', SCRIPT.parent / 'quality_texture.py')
QUALITY = importlib.util.module_from_spec(QUALITY_SPEC)
QUALITY_SPEC.loader.exec_module(QUALITY)


class QualityFixtureTests(unittest.TestCase):
    def test_desktop_quality_adapter_rejects_invalid_inputs_before_native_call(self):
        encoder = object.__new__(QUALITY.QualityEncoder)
        for data, width, height, fmt in ((b'', 1, 1, 4), (b'x', 0, 1, 4), (b'x', 2049, 1, 4), (b'x', 1, 1, True)):
            with self.assertRaises(ValueError): encoder.encode(data, width, height, fmt)

    def test_checks_sizes_duplicates_and_names(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root / 'valid.rgba').write_bytes(bytes(24)); manifest = root / 'fixtures.json'
            manifest.write_text(json.dumps([['valid', 2, 3]]))
            self.assertEqual(len(MODULE.fixture_list(root)), 1)
            for value in ([['valid', 2, 4]], [['valid', 2, 3], ['valid', 2, 3]], [['../valid', 2, 3]], [['valid', True, 3]]):
                manifest.write_text(json.dumps(value))
                with self.assertRaises(ValueError): MODULE.fixture_list(root)


if __name__ == '__main__': unittest.main()
