import base64
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

    def test_packed_template_ranges_and_unmodified_bytes(self):
        packed, intermediate, plan = (self.root / n for n in ('packed.obb', 'intermediate.obb', 'packed.json'))
        entry = 'assets/bin/Data/sharedassets1.resource'
        with zipfile.ZipFile(packed, 'w') as z: z.writestr(entry, b'head' + bytes(4) + b'tail')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headcliptail')
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-packed-steam-audio-offsets',
            'template_sha256': MODULE.digest(packed), 'records': [{'entry': entry, 'target_offset': 4,
            'size': 4, 'sha256': hashlib.sha256(b'clip').hexdigest()}]}))
        MODULE.verify_packed(packed, plan, intermediate)
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headclipBAD!')
        with self.assertRaises(ValueError): MODULE.verify_packed(packed, plan, intermediate)

    def test_texture_plan_verifies_split_payload_and_surroundings(self):
        template, intermediate, plan = (self.root / n for n in ('tex-template.obb', 'tex-intermediate.obb', 'tex-plan.json'))
        entries = ['assets/bin/Data/sharedassets1.assets.split0', 'assets/bin/Data/sharedassets1.assets.split1']
        with zipfile.ZipFile(template, 'w') as z:
            z.writestr(entries[0], b'xx' + bytes(2)); z.writestr(entries[1], bytes(2) + b'yy')
        with zipfile.ZipFile(intermediate, 'w') as z:
            z.writestr(entries[0], b'xxAB'); z.writestr(entries[1], b'CDyy')
        row = {'id': 0, 'target_size': 4, 'target_sha256': hashlib.sha256(b'ABCD').hexdigest(), 'fragments': [
            {'entry': entries[0], 'target_offset': 2, 'size': 2, 'converted_offset': 0},
            {'entry': entries[1], 'target_offset': 0, 'size': 2, 'converted_offset': 2}]}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_textures(template, plan, intermediate)
        with zipfile.ZipFile(intermediate, 'w') as z:
            z.writestr(entries[0], b'xxAB'); z.writestr(entries[1], b'CDzz')
        with self.assertRaises(ValueError): MODULE.verify_textures(template, plan, intermediate)

    def test_texture_plan_accepts_hashed_standalone_and_rejects_cubemap_use(self):
        template, intermediate, plan = (self.root / n for n in ('standalone-template.obb', 'standalone-intermediate.obb', 'standalone-plan.json'))
        entry = 'assets/bin/Data/' + 'a' * 32
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'head' + bytes(4) + b'tail')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headdatatail')
        row = {'id': 0, 'target_size': 4, 'target_sha256': hashlib.sha256(b'data').hexdigest(),
               'fragments': [{'entry': entry, 'target_offset': 4, 'size': 4, 'converted_offset': 0}]}
        payload = {'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                   'template_sha256': MODULE.digest(template), 'records': [row]}
        plan.write_text(json.dumps(payload))
        MODULE.verify_textures(template, plan, intermediate)
        payload['kind'] = 'experimental-converted-steam-cubemap-offsets'
        row['target_size'] = 6 * 131072
        plan.write_text(json.dumps(payload))
        with self.assertRaises(ValueError): MODULE.verify_textures(template, plan, intermediate, cubemaps=True)

    def test_mesh_plan_checks_payload_and_unchanged_bytes(self):
        template, intermediate, plan = (self.root / n for n in ('mesh-template.obb', 'mesh-intermediate.obb', 'mesh.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'head' + bytes(4) + b'tail')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headmesh' + b'tail')
        row = {'entry': entry, 'target_offset': 4, 'size': 4, 'sha256': hashlib.sha256(b'mesh').hexdigest()}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-steam-mesh-buffer-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_meshes(template, plan, intermediate)
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headmesh' + b'nope')
        with self.assertRaises(ValueError): MODULE.verify_meshes(template, plan, intermediate)

    def test_cubemap_plan_checks_full_target_and_blank_template(self):
        template, intermediate, plan = (self.root / n for n in ('cube-template.obb', 'cube-intermediate.obb', 'cube.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'; payload = b'a' * (6 * 131072)
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x' + bytes(len(payload)))
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'x' + payload)
        row = {'id': 0, 'target_size': len(payload), 'target_sha256': hashlib.sha256(payload).hexdigest(),
               'fragments': [{'entry': entry, 'target_offset': 1, 'size': len(payload), 'converted_offset': 0}]}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-cubemap-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_textures(template, plan, intermediate, cubemaps=True)
        row['target_sha256'] = '0' * 64
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-cubemap-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        with self.assertRaises(ValueError): MODULE.verify_textures(template, plan, intermediate, cubemaps=True)

    def test_animation_plan_checks_mask_and_surrounding_bytes(self):
        template, intermediate, plan = (self.root / n for n in ('anim-template.obb', 'anim-intermediate.obb', 'anim.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x\0b\0y')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'xAbCy')
        row = {'entry': entry, 'target_offset': 1, 'size': 3, 'mask': base64.b64encode(b'\x05').decode(),
               'masked_bytes': 2, 'masked_sha256': hashlib.sha256(b'\0b\0').hexdigest(),
               'original_sha256': hashlib.sha256(b'AbC').hexdigest()}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-animation-fragment-mask',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_animations(template, plan, intermediate)
        scene = 'assets/bin/Data/level1.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(scene, b'x\0b\0y')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(scene, b'xAbCy')
        row['entry'] = scene
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-animation-fragment-mask',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_animations(template, plan, intermediate)
        row['mask'] = base64.b64encode(b'\x85').decode()
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-animation-fragment-mask',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, intermediate)

    def test_exact_object_plan_checks_standalone_entry(self):
        template, intermediate, plan = (self.root / n for n in ('obj-template.obb', 'obj-intermediate.obb', 'obj.json'))
        entry = 'assets/bin/Data/' + 'b' * 32
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x' + bytes(4) + b'y')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'xmesh' + b'y')
        row = {'entry': entry, 'target_offset': 1, 'size': 4, 'sha256': hashlib.sha256(b'mesh').hexdigest()}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-exact-steam-object-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_meshes(template, plan, intermediate, objects=True)
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'xmesh' + b'z')
        with self.assertRaises(ValueError): MODULE.verify_meshes(template, plan, intermediate, objects=True)

    def test_exact_object_plan_checks_localization_bundle(self):
        template, intermediate, plan = (self.root / n for n in ('bundle-template.obb', 'bundle-intermediate.obb', 'bundle.json'))
        entry = 'assets/aa/StandaloneLinux64/localization-string-tables-english(en)_assets_all.bundle'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, bytes(4))
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'data')
        row = {'entry': entry, 'target_offset': 0, 'size': 4, 'sha256': hashlib.sha256(b'data').hexdigest()}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-exact-steam-object-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_meshes(template, plan, intermediate, objects=True)


if __name__ == '__main__':
    unittest.main()
