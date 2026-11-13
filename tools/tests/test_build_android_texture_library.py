import contextlib
import hashlib
import importlib.util
import io
from pathlib import Path
import sys
import tarfile
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('build_texture_library', Path(__file__).resolve().parents[1] / 'build-android-texture-library.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class TextureBuildTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = self.root / 'checkout'
        self.source.mkdir()
        self.ndk = self.root / 'ndk'
        self.compiler = self.ndk / 'toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        self.compiler.parent.mkdir(parents=True)
        self.compiler.write_text('#!' + sys.executable + '\nimport sys\nfrom pathlib import Path\nassert "-U__ARM_NEON" in sys.argv and "-static-libstdc++" in sys.argv\np=Path(sys.argv[sys.argv.index("-o")+1]);p.write_bytes(b"\\x7fELF\\x02\\x01"+b"\\0"*12+b"\\xb7\\0")\n')
        self.compiler.chmod(0o755)
        self.pins = {}
        for relative in module.PINS:
            path = self.source / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            if relative.endswith('.tar.gz'):
                staging = self.root / 'etcpak-0.9.15/src/etcpak'
                staging.mkdir(parents=True)
                (staging / 'ProcessRGB.cpp').write_text('fixture')
                (staging / 'Tables.cpp').write_text('fixture')
                with tarfile.open(path, 'w:gz') as tar:
                    tar.add(self.root / 'etcpak-0.9.15', arcname='etcpak-0.9.15')
            else:
                path.write_text('fixture ' + relative)
            self.pins[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
        self.output = self.root / 'texture.so'
        self.argv = ['program', '--ndk', str(self.ndk), '--output', str(self.output)]

    def invoke(self, extra=()):
        with patch.object(module, 'ROOT', self.source), patch.object(module, 'PINS', self.pins), patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_and_build(self):
        self.invoke(['--check'])
        self.assertFalse(self.output.exists())
        self.invoke()
        self.assertEqual(self.output.read_bytes()[:6], b'\x7fELF\x02\x01')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()

    def test_source_mismatch_is_rejected_before_output(self):
        (self.source / next(iter(self.pins))).write_text('modified')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            self.invoke()
        self.assertFalse(self.output.exists())


if __name__ == '__main__':
    unittest.main()
