import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace
import sys


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

    def test_probe_with_different_wrapper_requires_shader_object_validation(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); (root/'feature-probe').write_bytes(b'new wrapper')
            original = shaders.PINS
            self.addCleanup(lambda: setattr(shaders, 'PINS', original))
            shaders.PINS = {'feature-probe': hashlib.sha256(b'old wrapper').hexdigest()}
            with patch.object(shaders, 'verify_probe', side_effect=ValueError('shader differs')) as check:
                with self.assertRaises(ValueError): shaders.verify(root)
                check.assert_called_once_with(root/'feature-probe', 'feature-probe')
            with patch.object(shaders, 'verify_probe') as check:
                shaders.verify(root); check.assert_called_once_with(root/'feature-probe', 'feature-probe')

    def test_probe_validates_every_shader_and_rejects_extras(self):
        class Shader:
            type = SimpleNamespace(name='Shader')
            def __init__(self, name, raw): self.name, self.raw = name, raw
            def read_typetree(self): return {'m_ParsedForm': {'m_Name': self.name}}
            def get_raw_data(self): return self.raw
        objects = [Shader('expected', b'known')]
        fake = SimpleNamespace(load=lambda _: SimpleNamespace(objects=objects))
        with patch.dict(sys.modules, {'UnityPy': fake}), patch.dict(shaders.PROBE_SHADER_PINS, {'fixture': {'expected': hashlib.sha256(b'known').hexdigest()}}):
            shaders.verify_probe('unused', 'fixture')
            objects.append(Shader('unexpected', b'bad'))
            with self.assertRaises(ValueError): shaders.verify_probe('unused', 'fixture')


if __name__ == '__main__': unittest.main()
