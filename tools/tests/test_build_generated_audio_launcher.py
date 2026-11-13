import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile


SCRIPT = Path(__file__).resolve().parents[1] / 'build-generated-audio-launcher.py'
SPEC = importlib.util.spec_from_file_location('build_generated_audio_launcher', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class GeneratedAudioBuildTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='openbonk-audio-build-test-')
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.template, self.expected, self.runtime, self.plan = (self.root / name for name in ('template.obb', 'expected.obb', 'runtime.apk', 'plan.json'))
        self.gradle = self.root / 'gradle'
        data = self.gradle / 'unityLibrary/src/main/assets/bin/Data'
        data.mkdir(parents=True)
        self.guid = b'00000000-0000-0000-0000-000000000000'
        self.audio = 'assets/bin/Data/' + 'a' * 32 + '.resource'
        entries = {'unity_obb_guid': self.guid, self.audio: bytes(4), 'other': b'keep'}
        with zipfile.ZipFile(self.template, 'w') as z:
            for name, value in entries.items(): z.writestr(name, value)
        entries[self.audio] = b'clip'
        with zipfile.ZipFile(self.expected, 'w') as z:
            for name, value in entries.items(): z.writestr(name, value)
        files = ('ScriptingAssemblies.json', 'RuntimeInitializeOnLoads.json', 'Managed/Metadata/global-metadata.dat')
        with zipfile.ZipFile(self.runtime, 'w') as z:
            z.writestr('assets/unity_obb_guid', self.guid)
            z.writestr('lib/arm64-v8a/libil2cpp.so', b'native')
            for name in files:
                path = data / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(name.encode())
                z.writestr('assets/bin/Data/' + name, name.encode())
        self.plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-steam-audio-offsets',
            'template_sha256': MODULE.digest(self.template), 'records': [{'entry': self.audio,
            'size': 4, 'sha256': hashlib.sha256(b'clip').hexdigest()}]}))

    def test_verified_matching_inputs(self):
        self.assertEqual(MODULE.verify(self.template, self.plan, self.expected, self.runtime, self.gradle), self.guid)

    def test_rejects_changed_expected_audio(self):
        with zipfile.ZipFile(self.expected, 'w') as z:
            z.writestr('unity_obb_guid', self.guid)
            z.writestr(self.audio, b'wrong')
            z.writestr('other', b'keep')
        with self.assertRaises(ValueError):
            MODULE.verify(self.template, self.plan, self.expected, self.runtime, self.gradle)

    def test_rejects_mismatched_runtime_metadata(self):
        data = self.gradle / 'unityLibrary/src/main/assets/bin/Data/ScriptingAssemblies.json'
        data.write_bytes(b'different')
        with self.assertRaisesRegex(ValueError, 'code metadata differ'):
            MODULE.verify(self.template, self.plan, self.expected, self.runtime, self.gradle)

    def test_single_line_replacement_preserves_following_properties(self):
        properties = self.root / 'gradle.properties'
        properties.write_text('unityProjectPath=old\nother=value\n')
        MODULE.replace_once(properties, r'^unityProjectPath=.*$', 'unityProjectPath=new')
        self.assertEqual(properties.read_text(), 'unityProjectPath=new\nother=value\n')


if __name__ == '__main__':
    unittest.main()
