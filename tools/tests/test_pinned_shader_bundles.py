import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1]/'pinned-shader-bundles.py'
spec = importlib.util.spec_from_file_location('pinned_shader_bundles_test', SCRIPT)
shaders = importlib.util.module_from_spec(spec)
spec.loader.exec_module(shaders)


class PinnedShaderBundlesTest(unittest.TestCase):
    def test_generated_package_names_match_builder_and_pins(self):
        builder_spec = importlib.util.spec_from_file_location('postprocess_builder_test', SCRIPT.parent/'build-independent-postprocess-study.py')
        builder = importlib.util.module_from_spec(builder_spec); builder_spec.loader.exec_module(builder)
        self.assertEqual(shaders.PACKAGE_NAMES, frozenset(builder.COMPUTE_NAMES) | {'postprocess-study'})
        self.assertTrue(shaders.PACKAGE_NAMES < shaders.PINS.keys())

    def test_stage_rebuilt_package_and_cache_without_overwriting(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); cache = root/'cache'; package = root/'package'; output = root/'output'
            cache.mkdir(); package.mkdir()
            originals = shaders.PINS, shaders.PACKAGE_NAMES
            self.addCleanup(lambda: (setattr(shaders, 'PINS', originals[0]), setattr(shaders, 'PACKAGE_NAMES', originals[1])))
            shaders.PINS = {name: hashlib.sha256(value).hexdigest() for name, value in (('compiled', b'one'), ('other', b'two'))}
            shaders.PACKAGE_NAMES = frozenset(('compiled',))
            (package/'compiled').write_bytes(b'one'); (cache/'other').write_bytes(b'two')
            shaders.stage(cache, output, package)
            self.assertEqual((output/'compiled').read_bytes(), b'one')
            self.assertEqual((output/'other').read_bytes(), b'two')
            with self.assertRaises(ValueError): shaders.stage(cache, output, package)
            self.assertEqual((output/'compiled').read_bytes(), b'one')

    def test_invalid_rebuilt_bundle_is_rejected_before_staging(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); cache = root/'cache'; package = root/'package'; output = root/'output'
            cache.mkdir(); package.mkdir()
            originals = shaders.PINS, shaders.PACKAGE_NAMES
            self.addCleanup(lambda: (setattr(shaders, 'PINS', originals[0]), setattr(shaders, 'PACKAGE_NAMES', originals[1])))
            shaders.PINS = {'compiled': hashlib.sha256(b'good').hexdigest()}
            shaders.PACKAGE_NAMES = frozenset(('compiled',))
            (package/'compiled').write_bytes(b'bad')
            with self.assertRaises(ValueError): shaders.stage(cache, output, package)
            self.assertFalse(output.exists())


if __name__ == '__main__': unittest.main()
