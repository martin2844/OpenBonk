import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

path = Path(__file__).resolve().parents[1] / 'experimental-localization-obb-plan.py'
spec = importlib.util.spec_from_file_location('localization_obb_plan', path)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class LocalizationObbPlanTests(unittest.TestCase):
    def test_two_archives_preserve_distinct_crc_and_stream_unchanged_entries(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); source = root/'source.zip'; masked = root/'masked.zip'; converted = root/'converted.zip'
            with zipfile.ZipFile(source, 'w') as z:
                z.writestr('changed', b'original'); z.writestr('unchanged', b'keep' * 300000)
            with zipfile.ZipFile(source) as inp, zipfile.ZipFile(masked, 'w') as a, zipfile.ZipFile(converted, 'w') as b:
                module.write_pair(inp, a, b, inp.getinfo('changed'), b'zeros', b'different-content')
                module.write_pair(inp, a, b, inp.getinfo('unchanged'))
            for path, value in ((masked, b'zeros'), (converted, b'different-content')):
                with zipfile.ZipFile(path) as z:
                    self.assertIsNone(z.testzip())
                    self.assertEqual(z.read('changed'), value)
                    self.assertEqual(z.read('unchanged'), b'keep' * 300000)


if __name__ == '__main__': unittest.main()
