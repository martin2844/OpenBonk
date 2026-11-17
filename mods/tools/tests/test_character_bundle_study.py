import importlib.util
from pathlib import Path
import tempfile
import unittest

SOURCE = Path(__file__).resolve().parents[1] / 'character_bundle_study.py'
spec = importlib.util.spec_from_file_location('character_bundle_study', SOURCE)
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


class CharacterStudyInputs(unittest.TestCase):
    def test_paths_must_remain_inside_exported_assets(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            (root / 'Assets').mkdir()
            (root / 'Assets/idle.anim').write_text('fixture')
            (root / 'other.anim').write_text('outside')
            (root / 'Assets/link.anim').symlink_to(root / 'other.anim')
            self.assertEqual(study.asset_path(root, 'Assets/idle.anim'), 'Assets/idle.anim')
            for bad in ('../other.anim', 'Assets/../other.anim', 'Assets/link.anim',
                        'Assets/idle.anim;Assets/other.anim', '/etc/passwd'):
                with self.subTest(path=bad), self.assertRaises((ValueError, OSError)):
                    study.asset_path(root, bad)

    def test_third_party_code_and_plugins_are_not_imported(self):
        names = ['Model.prefab', 'Run.anim', 'Scripts', 'plugins', 'Editor',
                 'Injected.CS', 'Native.DLL', 'assembly.asmdef', 'compiler.rsp',
                 'libforeign.so', 'external.aar', 'native.bundle', 'code.jslib']
        ignored = set(study.omit_foreign_code('/unused', names))
        self.assertEqual(ignored, set(names) - {'Model.prefab', 'Run.anim'})


if __name__ == '__main__':
    unittest.main()
