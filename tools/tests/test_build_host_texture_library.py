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

spec = importlib.util.spec_from_file_location('build_host_texture', Path(__file__).resolve().parents[1] / 'build-host-texture-library.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class HostTextureBuildTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.source = self.root / 'checkout'; self.source.mkdir()
        self.jdk = self.root / 'jdk'; (self.jdk / 'include/linux').mkdir(parents=True)
        (self.jdk / 'include/jni.h').write_text('fixture'); (self.jdk / 'include/linux/jni_md.h').write_text('fixture')
        self.compiler = self.root / 'compiler'
        self.compiler.write_text('#!' + sys.executable + '\nimport sys\nfrom pathlib import Path\nassert "-DOPENBONK_JNI" in sys.argv\nPath(sys.argv[sys.argv.index("-o")+1]).write_bytes(b"\\x7fELF\\x02\\x01"+b"\\0"*12+b"\\x3e\\0")\n')
        self.compiler.chmod(0o755); self.pins = {}
        for relative in module.android.PINS:
            path = self.source / relative; path.parent.mkdir(parents=True, exist_ok=True)
            if relative.endswith('.tar.gz'):
                staging = self.root / 'etcpak-0.9.15/src/etcpak'; staging.mkdir(parents=True)
                (staging / 'ProcessRGB.cpp').write_text('fixture'); (staging / 'Tables.cpp').write_text('fixture')
                with tarfile.open(path, 'w:gz') as tar: tar.add(self.root / 'etcpak-0.9.15', arcname='etcpak-0.9.15')
            else: path.write_text('fixture ' + relative)
            self.pins[relative] = hashlib.sha256(path.read_bytes()).hexdigest()
        self.output = self.root / 'host.so'
        self.argv = ['program', '--java-home', str(self.jdk), '--compiler', str(self.compiler), '--output', str(self.output)]

    def invoke(self, extra=()):
        with patch.object(module, 'ROOT', self.source), patch.object(module.android, 'PINS', self.pins), \
                patch.object(module.platform, 'system', return_value='Linux'), patch.object(module.platform, 'machine', return_value='x86_64'), \
                patch.object(sys, 'argv', self.argv + list(extra)):
            module.main()

    def test_check_build_and_refuse_overwrite(self):
        self.invoke(['--check']); self.assertFalse(self.output.exists())
        self.invoke(); self.assertEqual(self.output.read_bytes()[:6], b'\x7fELF\x02\x01')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit): self.invoke()

    def test_modified_input_is_rejected(self):
        (self.source / next(iter(self.pins))).write_text('modified')
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit): self.invoke()
        self.assertFalse(self.output.exists())


if __name__ == '__main__': unittest.main()
