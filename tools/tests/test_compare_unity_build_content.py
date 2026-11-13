import importlib.util
from collections import Counter
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('compare_build_content', Path(__file__).resolve().parents[1] / 'compare-unity-build-content.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class BuildContentComparisonTests(unittest.TestCase):
    def test_groups_splits_and_rejects_missing_parts(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'data.zip'
            with zipfile.ZipFile(path, 'w') as z:
                z.writestr(module.PREFIX + 'globalgamemanagers.assets.split1', b'b')
                z.writestr(module.PREFIX + 'globalgamemanagers.assets.split0', b'a')
                z.writestr(module.PREFIX + 'level1', b'c')
                z.writestr('assets/aa/ignored.bundle', b'd')
            with zipfile.ZipFile(path) as z:
                groups = module.asset_groups(z)
                self.assertEqual([z.read(i) for i in groups['globalgamemanagers.assets']], [b'a', b'b'])
                self.assertEqual(set(groups), {'globalgamemanagers.assets', 'level1'})
            with zipfile.ZipFile(path, 'w') as z: z.writestr(module.PREFIX + 'level1.split1', b'x')
            with zipfile.ZipFile(path) as z:
                with self.assertRaises(ValueError): module.asset_groups(z)

    def test_differences_report_duplicate_counts(self):
        diff = module.differences(Counter({'same': 2, 'left': 1}), Counter({'same': 1, 'right': 3}))
        self.assertEqual(diff, {'reference_only': {'left': 1, 'same': 1}, 'candidate_only': {'right': 3}})

    def test_inventory_records_script_identity_and_read_errors(self):
        class Object:
            def __init__(self, kind, value): self.type = SimpleNamespace(name=kind); self.path_id = 7; self.value = value
            def read(self):
                if isinstance(self.value, Exception): raise self.value
                return self.value
        objects = [Object('MonoScript', SimpleNamespace(m_AssemblyName='game', m_Namespace='a', m_ClassName='B')),
                   Object('Texture2D', SimpleNamespace(m_Name='sprite')), Object('Material', ValueError())]
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'data.zip'
            with zipfile.ZipFile(path, 'w') as z: z.writestr(module.PREFIX + 'level1', b'data')
            with patch.object(module.UnityPy, 'load', return_value=SimpleNamespace(objects=objects)):
                counts, names, scripts, problems, groups, hashes = module.inventory([path])
            self.assertEqual(groups, 1)
            self.assertEqual(counts['Material'], 1)
            self.assertEqual(names['Texture2D']['sprite'], 1)
            self.assertEqual(scripts[('game', 'a', 'B')], 1)
            self.assertEqual(problems[0]['error'], 'ValueError')
            self.assertEqual(hashes[0]['entries'], 1)


if __name__ == '__main__': unittest.main()
