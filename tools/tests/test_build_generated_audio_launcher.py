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
    def test_gradle_offline_by_default_and_online_only_when_requested(self):
        self.assertIn('--offline', MODULE.gradle_command('java', 'gradle.jar'))
        online = MODULE.gradle_command('java', 'gradle.jar', 'steam-run', True)
        self.assertNotIn('--offline', online)
        self.assertEqual(online[0:2], ['steam-run', 'java'])

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

    def test_windows_research_requires_same_final_archive(self):
        root = self.root / 'windows'; canonical = root / 'canonical'; canonical.mkdir(parents=True)
        objects = root / 'plans/objects'; objects.mkdir(parents=True)
        for path in [objects / 'template.obb', objects / 'plan.json', canonical / 'final.obb',
                     *[canonical / (name + '-plan.json') for name in ('animation', 'cubemap', 'mesh', 'texture', 'packed', 'audio')],
                     *[canonical / ('after-' + name + '.obb') for name in ('objects', 'animation', 'cubemap', 'mesh', 'texture', 'packed')]]:
            path.write_bytes(b'different')
        with self.assertRaisesRegex(ValueError, 'final OBBs differ'):
            MODULE.verify_windows_research(root, self.expected, self.runtime, self.gradle)

    def test_dual_plan_rejects_wrong_platform_sources(self):
        inventory = json.loads((MODULE.ROOT / 'tools/launcher-steam-inputs.json').read_text())['platforms']
        path = self.root / 'object-plan.json'; name = 'sharedassets1.assets'
        path.write_text(json.dumps({'sources': {name: inventory['linux'][name]['sha256']}}))
        MODULE.verify_research_platform(path, 'linux')
        with self.assertRaisesRegex(ValueError, 'incompatible Steam sources'):
            MODULE.verify_research_platform(path, 'windows')

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
        entry = 'assets/bin/Data/sharedassets4.resource'
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

    def test_texture_plan_accepts_shared0_split(self):
        template, result, plan = (self.root / n for n in ('shared0-template.obb', 'shared0-result.obb', 'shared0-plan.json'))
        entry = 'assets/bin/Data/sharedassets0.assets.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x' + bytes(4))
        with zipfile.ZipFile(result, 'w') as z: z.writestr(entry, b'xdata')
        row = {'id': 0, 'target_size': 4, 'target_sha256': hashlib.sha256(b'data').hexdigest(),
               'fragments': [{'entry': entry, 'target_offset': 1, 'size': 4, 'converted_offset': 0}]}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_textures(template, plan, result)

    def test_partial_rgba_verifies_exact_mask_and_unchanged_bytes(self):
        template, result, plan = (self.root / n for n in ('partial-template.obb', 'partial-result.obb', 'partial-plan.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x\x00B\x00y')
        with zipfile.ZipFile(result, 'w') as z: z.writestr(entry, b'xABCy')
        fragment = {'entry': entry, 'target_offset': 1, 'size': 3, 'converted_offset': 0, 'mask': 'BQ==', 'masked_bytes': 2,
                    'masked_sha256': hashlib.sha256(b'\x00B\x00').hexdigest(), 'original_sha256': hashlib.sha256(b'ABC').hexdigest()}
        payload = {'schema': 1, 'kind': 'experimental-partial-rgba-texture-offsets', 'template_sha256': MODULE.digest(template),
                   'records': [{'id': 0, 'target_size': 3, 'target_format': 4, 'fragments': [fragment]}]}
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        for fmt in (45, 47):
            payload['records'][0].update(target_format=fmt, quality=True)
            plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
            payload['records'][0]['quality'] = False
            plan.write_text(json.dumps(payload))
            with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0].update(target_format=4, quality=False)
        plan.write_text(json.dumps(payload))
        with zipfile.ZipFile(result, 'w') as z: z.writestr(entry, b'xAXCy')
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)

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

    def test_quality_texture_plan_rejects_incompatible_format(self):
        template, intermediate, plan = (self.root / n for n in ('quality-template.obb', 'quality-intermediate.obb', 'quality-plan.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, bytes(4))
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'data')
        row = {'id': 0, 'target_size': 4, 'target_sha256': hashlib.sha256(b'data').hexdigest(),
               'quality': True, 'target_format': 45, 'width': 1, 'height': 1,
               'fragments': [{'entry': entry, 'target_offset': 0, 'size': 4, 'converted_offset': 0}]}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        with self.assertRaisesRegex(ValueError, 'quality texture metadata'): MODULE.verify_textures(template, plan, intermediate)

    def test_mesh_plan_checks_payload_and_unchanged_bytes(self):
        template, intermediate, plan = (self.root / n for n in ('mesh-template.obb', 'mesh-intermediate.obb', 'mesh.json'))
        entry = 'assets/bin/Data/sharedassets4.assets.split0'
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
