import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest


TOOLS = Path(__file__).resolve().parents[1]


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, TOOLS / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


generator = load('thai_generator', 'generate-thai-localization-patch.py')
applier = load('thai_applier', 'apply-bsdiff-pinned.py')


class ThaiPatchTest(unittest.TestCase):
    def test_generated_patch_restores_changed_data(self):
        old = bytes((i * 71 + 23) % 256 for i in range(34973))
        new = old[:11903] + b'xyz' + old[11907:]
        self.assertEqual(len(new), 34972)
        patch = generator.make_patch(old, new)
        self.assertEqual(applier.apply(old, patch, len(new), hashlib.sha256(new).hexdigest()), new)
        with self.assertRaises(ValueError):
            generator.make_patch(old[:-1], new)

    def test_rejects_changed_or_unsafe_input(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            relative = 'StreamingAssets/aa/StandaloneLinux64/' + generator.NAME
            path = root / relative
            path.parent.mkdir(parents=True)
            path.write_bytes(b'example')
            inventory = {'linux': {relative: {'size': 7, 'sha256': hashlib.sha256(b'example').hexdigest()}}}
            self.assertEqual(generator.load_input(root, 'linux', inventory), b'example')
            path.write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError, 'unsupported Thai bundle'):
                generator.load_input(root, 'linux', inventory)
            path.unlink()
            path.symlink_to('/etc/passwd')
            with self.assertRaisesRegex(ValueError, 'unsafe Thai bundle'):
                generator.load_input(root, 'linux', inventory)


if __name__ == '__main__':
    unittest.main()
