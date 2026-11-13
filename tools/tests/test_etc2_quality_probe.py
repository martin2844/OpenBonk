import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1] / 'check-etc2-quality-android.py'
SPEC = importlib.util.spec_from_file_location('etc2_quality_check', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class QualityFixtureTests(unittest.TestCase):
    def test_checks_sizes_duplicates_and_names(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); (root / 'valid.rgba').write_bytes(bytes(24)); manifest = root / 'fixtures.json'
            manifest.write_text(json.dumps([['valid', 2, 3]]))
            self.assertEqual(len(MODULE.fixture_list(root)), 1)
            for value in ([['valid', 2, 4]], [['valid', 2, 3], ['valid', 2, 3]], [['../valid', 2, 3]], [['valid', True, 3]]):
                manifest.write_text(json.dumps(value))
                with self.assertRaises(ValueError): MODULE.fixture_list(root)


if __name__ == '__main__': unittest.main()
