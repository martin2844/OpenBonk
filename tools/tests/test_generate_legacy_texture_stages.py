from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]/'analysis/data-conversion'


class TextureStageTests(unittest.TestCase):
    def test_shader_output_directory_is_not_overwritten(self):
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory)/'output'; output.mkdir()
            sentinel = output/'keep'; sentinel.write_text('existing data')
            result = subprocess.run([sys.executable, str(ROOT/'generate-android-shared1-shader-pack.py'),
                                     '--output', str(output)], capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('output exists', result.stderr)
            self.assertEqual(sentinel.read_text(), 'existing data')

    def test_invalid_base_is_rejected_without_output(self):
        stages = (('generate-android-shared1-exact-textures-plan.py', ()),
                  ('generate-android-shared1-exact-textures-plan.py', ('rgba',)),
                  ('generate-android-shared1-rgba-converted-plan.py', ()),
                  ('generate-android-shared1-normal-plan.py', ()),
                  ('generate-android-shared1-normal-plan.py', ('water',)),
                  ('generate-android-shared1-animation-terrain-plan.py', ()),
                  ('generate-android-shared1-cubemap-plan.py', ()),
                  *(( 'generate-android-shared1-etc-plan.py', (mode,) if mode else ())
                    for mode in ('', 'rgba', 'alias', 'matched', 'alpha', 'large', 'renamed', 'last')))
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); base = root/'base'; base.write_bytes(b'invalid')
            for index, (name, mode) in enumerate(stages):
                with self.subTest(name=name, mode=mode):
                    output = root/(str(index)+'.bin')
                    result = subprocess.run([sys.executable, str(ROOT/name), *mode, '--base', str(base),
                                             '--target-asset', str(root/'target'), '--output', str(output)],
                                            capture_output=True, text=True)
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn('invalid base', result.stderr)
                    self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
