import hashlib
import importlib.util
from pathlib import Path
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / 'legacy-converted-assets.py'
spec = importlib.util.spec_from_file_location('legacy_converted_assets', SCRIPT)
assets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(assets)


class ConvertedAssetsTests(unittest.TestCase):
    def test_archive_creation_and_tamper_rejection(self):
        original = assets.PINS
        try:
            data = {'sharedassets0.assets': b'zero', 'sharedassets1.assets': b'one', 'sharedassets2.assets': b'two'}
            assets.PINS = {name: hashlib.sha256(value).hexdigest() for name, value in data.items()}
            with tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                source = root/'source'; source.mkdir()
                for name, value in data.items(): (source/name).write_bytes(value)
                archive = root/'assets.zip'
                assets.make_archive(source, archive)
                assets.validate_archive(archive)
                bad = root/'bad.zip'
                with zipfile.ZipFile(bad, 'x') as z:
                    for name, value in data.items(): z.writestr(assets.PREFIX+name, value+b'!')
                with self.assertRaisesRegex(ValueError, 'differs'):
                    assets.validate_archive(bad)
                (source/'sharedassets1.assets').write_bytes(b'changed')
                with self.assertRaisesRegex(ValueError, 'differs'):
                    assets.make_archive(source, root/'failed.zip')
                self.assertFalse((root/'failed.zip').exists())
        finally:
            assets.PINS = original


if __name__ == '__main__':
    unittest.main()
