import base64
import hashlib
import importlib.util
import json
import struct
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / 'build-generated-audio-launcher.py'
SPEC = importlib.util.spec_from_file_location('build_generated_audio_launcher', SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class GeneratedAudioBuildTest(unittest.TestCase):
    def test_pinned_migration_paths_validate_targets_versions_and_patch_bytes(self):
        suite_target = '2d39f7ded1fd3892d5e8391f4d7b98756afccad52c1196bc878395ffd125ff99'
        self.assertEqual(len(MODULE.verified_migration_patches(suite_target, 8)), 4)
        with self.assertRaisesRegex(ValueError, 'version code'):
            MODULE.verified_migration_patches(suite_target, 7)
        old_target = '9011ea73423c8418db7f7d0895c515b98a23001e947fd227b83c221a04e37475'
        new_target = 'c3a42cbb1b38bdf89ee0e8175b81f2f70da4206e9a4862a67b11cda7cf3adf34'
        self.assertEqual(len(MODULE.verified_migration_patches(old_target, 2)), 1)
        self.assertEqual(len(MODULE.verified_migration_patches(new_target, 5)), 2)
        integrated_target = 'c7e714c4fe5be7d147e0225720dd4bc5d6d9f790432a016ab1ac99dedd67f60c'
        self.assertEqual(len(MODULE.verified_migration_patches(integrated_target, 6)), 3)
        with self.assertRaisesRegex(ValueError, 'version code'):
            MODULE.verified_migration_patches(integrated_target, 5)
        with self.assertRaisesRegex(ValueError, 'version code'):
            MODULE.verified_migration_patches(new_target, 4)
        with self.assertRaisesRegex(ValueError, 'no pinned'):
            MODULE.verified_migration_patches('0' * 64, 5)
        with tempfile.TemporaryDirectory() as tmp:
            import shutil
            target = Path(tmp) / 'updates/migrations'; target.mkdir(parents=True)
            for source in MODULE.verified_migration_patches(new_target, 5):
                shutil.copyfile(source, target / source.name)
            (target / '0.1.1-to-integration.zst').write_bytes(b'corrupt')
            with self.assertRaisesRegex(ValueError, 'digest mismatch'):
                MODULE.verified_migration_patches(new_target, 5, Path(tmp))

    def test_reused_gradle_export_gets_current_launcher_sources(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); checkout = root / 'checkout'; java = root / 'export/src/main/java'
            launcher = checkout / 'unity/OpenBonk/Assets/AndroidSplitLauncher'
            modpicker = checkout / 'unity/OpenBonk/Assets/AndroidModPicker'
            destination = java / 'com/megabonk/launcher'; picker_dest = java / 'com/megabonk/modpicker'
            for path in (launcher, modpicker, destination, picker_dest): path.mkdir(parents=True)
            (launcher / 'Launch.java').write_text('new launcher')
            (launcher / 'SplitExpectedData.java').write_text('checkout placeholder')
            (modpicker / 'Picker.java').write_text('new picker')
            (destination / 'Launch.java').write_text('stale launcher')
            (destination / 'SplitExpectedData.java').write_text('generated checksum')
            MODULE.overlay_launcher_sources(checkout, java)
            self.assertEqual((destination / 'Launch.java').read_text(), 'new launcher')
            self.assertEqual((destination / 'SplitExpectedData.java').read_text(), 'generated checksum')
            self.assertEqual((picker_dest / 'Picker.java').read_text(), 'new picker')
            (destination / 'Launch.java').unlink()
            (destination / 'Launch.java').symlink_to(root / 'outside')
            with self.assertRaisesRegex(ValueError, 'symbolic link'):
                MODULE.overlay_launcher_sources(checkout, java)

    def test_localization_checks_fragments_sources_and_unrelated_entries(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp); template = root/'a.obb'; result = root/'b.obb'; plan = root/'plan.json'
            inventory = json.loads((MODULE.ROOT/'tools/launcher-steam-inputs.json').read_text())['platforms']['linux']
            prefix = 'StreamingAssets/aa/StandaloneLinux64/'
            sources = sorted(n for n in inventory if n.startswith(prefix) and n[len(prefix):].startswith(('localization-string-tables-', 'localization-assets-shared_assets_all')))
            self.assertEqual(len(sources), 18)
            rows = []
            with zipfile.ZipFile(template,'w') as a, zipfile.ZipFile(result,'w') as b:
                a.writestr('untouched', b'same'); b.writestr('untouched', b'same')
                for source in sources:
                    entry = 'assets/aa/Android/' + source[len(prefix):]; masked = b'pre\x00\x00post'; actual = b'preABpost'
                    a.writestr(entry, masked); b.writestr(entry, actual)
                    raw = (b'OBL1' + bytes.fromhex(inventory[source]['sha256']) + hashlib.sha256(masked).digest() +
                           hashlib.sha256(actual).digest() + struct.pack('>I',1) + struct.pack('>III',3,0,2) + hashlib.sha256(b'AB').digest())
                    rows.append({'entry':entry,'source':source,'plan':base64.b64encode(raw).decode(),'plan_sha256':hashlib.sha256(raw).hexdigest()})
            obj = {'schema':1,'kind':'experimental-localization-cab-obb','platform':'linux','template_sha256':MODULE.digest(template),'records':rows}
            plan.write_text(json.dumps(obj)); MODULE.verify_localization(template,plan,result,'linux')
            obj['records'][0]['plan_sha256'] = '0'*64; plan.write_text(json.dumps(obj))
            with self.assertRaises(ValueError): MODULE.verify_localization(template,plan,result,'linux')
            obj['records'][0] = obj['records'][1]; plan.write_text(json.dumps(obj))
            with self.assertRaises(ValueError): MODULE.verify_localization(template,plan,result,'linux')

    def test_package_version_override_is_validated_and_changes_only_version(self):
        with tempfile.TemporaryDirectory() as tmp:
            gradle = Path(tmp) / 'build.gradle'
            original = 'defaultConfig {\n        versionCode 1\n        versionName "old"\n        minSdkVersion 26\n}\n'
            gradle.write_text(original)
            MODULE.set_package_version(gradle, 2, '0.1.1-beta')
            self.assertEqual(gradle.read_text(), original.replace('versionCode 1', 'versionCode 2').replace('"old"', '"0.1.1-beta"'))
            for code in (0, -1, 2100000001, True):
                with self.assertRaises(ValueError): MODULE.set_package_version(gradle, code, None)
            for name in ('', 'bad"name', 'bad\nname', 'x' * 101):
                with self.assertRaises(ValueError): MODULE.set_package_version(gradle, None, name)

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

    def test_windows_localization_verifies_and_selects_rebound_inputs(self):
        root = self.root/'win'; canonical = root/'canonical'; canonical.mkdir(parents=True)
        objects = root/'plans/objects'; objects.mkdir(parents=True)
        partial = root/'plans/partial-rgba'; partial.mkdir()
        locale = root/'plans/localization'; locale.mkdir()
        files = [objects/'template.obb', locale/'template.obb', locale/'plan.json', partial/'template.obb',
                 canonical/'objects-plan.json', canonical/'partial-rgba-plan.json', canonical/'after-localization.obb',
                 canonical/'after-partial-rgba.obb', canonical/'final.obb',
                 *[canonical/(name+'-plan.json') for name in ('animation','cubemap','mesh','texture','packed','audio')],
                 *[canonical/('after-'+name+'.obb') for name in ('objects','animation','cubemap','mesh','texture','packed')]]
        for path in files: path.write_bytes(b'data')
        with patch.object(MODULE,'verify_research_platform'), patch.object(MODULE,'verify_localization') as loc, \
             patch.object(MODULE,'verify_animations') as animations, patch.object(MODULE,'verify_meshes'), \
             patch.object(MODULE,'verify_textures'), patch.object(MODULE,'verify_packed'), patch.object(MODULE,'verify'):
            selected, _, plans = MODULE.verify_windows_research(root,canonical/'final.obb',self.runtime,self.gradle,partial_rgba=True,localization=True)
        self.assertEqual(selected,locale/'template.obb')
        self.assertEqual(plans['partial-rgba'],canonical/'partial-rgba-plan.json')
        self.assertEqual(plans['localization'],locale/'plan.json')
        loc.assert_called_once_with(locale/'template.obb',locale/'plan.json',canonical/'after-localization.obb','windows')
        self.assertEqual(animations.call_args_list[0].args[0], canonical/'after-localization.obb')

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

    def test_full_rgba_normal_checks_metadata_and_payload_integrity(self):
        template, result, plan = (self.root / n for n in ('water-template.obb', 'water-result.obb', 'water-plan.json'))
        entry = 'assets/bin/Data/sharedassets1.assets.split0'
        data = bytes(range(256)) * 21 + bytes(range(84))
        self.assertEqual(len(data), 5460)
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'x' + bytes(len(data)) + b'y')
        with zipfile.ZipFile(result, 'w') as z: z.writestr(entry, b'x' + data + b'y')
        row = {'id': 0, 'normal_rgba': True, 'source': 'sharedassets1.assets.resS',
               'source_format': 12, 'target_format': 4, 'width': 32, 'height': 32, 'mips': 6,
               'source_size': 1392, 'target_size': len(data), 'target_sha256': hashlib.sha256(data).hexdigest(),
               'fragments': [{'entry': entry, 'target_offset': 1, 'size': len(data), 'converted_offset': 0}]}
        payload = {'schema': 1, 'kind': 'experimental-converted-steam-texture-offsets',
                   'template_sha256': MODULE.digest(template), 'records': [row]}
        def verify(record):
            payload['records'] = [record]; plan.write_text(json.dumps(payload))
            MODULE.verify_textures(template, plan, result)
        verify(row)
        for change in ({'source': 'sharedassets2.assets'}, {'width': 31}, {'mips': 5},
                       {'source_format': 4}, {'target_format': 45}, {'source_size': 1},
                       {'quality': True}, {'normal': True}, {'direct_normal': 'yes'},
                       {'blend': 'AA=='}, {'normal_rgba': 'yes'}):
            with self.subTest(change=change), self.assertRaises(ValueError): verify(dict(row, **change))
        verify(dict(row, normal_rgba=False, width=1))
        with zipfile.ZipFile(result, 'w') as z: z.writestr(entry, b'x' + data[:-1] + b'!' + b'y')
        with self.assertRaises(ValueError): verify(row)

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
            plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0].update(target_format=4, quality=True)
        plan.write_text(json.dumps(payload))
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0].update(target_format=47, quality=True, blend='AQ==')
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0]['blend'] = 'gA=='
        plan.write_text(json.dumps(payload))
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0].update(blend='BQ==', blend_unit=1)
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0]['blend_rgba'] = 'Ag=='
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        payload['records'][0]['blend_extra'] = [{'metric': 1, 'choices': 'BA=='}, {'metric': 3, 'choices': 'AA=='}]
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        for invalid in ([], [{'metric': 1, 'choices': 'gA=='}], [{'metric': 2, 'choices': 'AA=='}, {'metric': 2, 'choices': 'AA=='}], [{'metric': True, 'choices': 'AA=='}]):
            payload['records'][0]['blend_extra'] = invalid; plan.write_text(json.dumps(payload))
            with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        del payload['records'][0]['blend_extra']
        payload['records'][0]['blend_rgba'] = 'gA=='
        plan.write_text(json.dumps(payload))
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        del payload['records'][0]['blend_rgba']
        payload['records'][0]['blend_unit'] = True
        plan.write_text(json.dumps(payload))
        with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
        del payload['records'][0]['blend_unit']
        del payload['records'][0]['blend']
        payload['records'][0].update(target_format=4, quality=False)
        payload['records'][0].update(normal_rgba=True, source_format=12)
        plan.write_text(json.dumps(payload)); MODULE.verify_animations(template, plan, result, partial_rgba=True)
        for invalid in ({'normal_rgba': 'yes'}, {'source_format': 4}, {'target_format': 45}):
            record = payload['records'][0]; previous = {key: record.get(key) for key in invalid}
            record.update(invalid); plan.write_text(json.dumps(payload))
            with self.assertRaises(ValueError): MODULE.verify_animations(template, plan, result, partial_rgba=True)
            record.update(previous)
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
               'quality': True, 'target_format': 4, 'width': 1, 'height': 1,
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

    def test_mesh_plan_accepts_standalone_serialized_asset(self):
        template, intermediate, plan = (self.root / n for n in ('mesh-hash-template.obb', 'mesh-hash-result.obb', 'mesh-hash.json'))
        entry = 'assets/bin/Data/' + 'a' * 32
        with zipfile.ZipFile(template, 'w') as z: z.writestr(entry, b'head' + bytes(4) + b'tail')
        with zipfile.ZipFile(intermediate, 'w') as z: z.writestr(entry, b'headmesh' + b'tail')
        row = {'entry': entry, 'target_offset': 4, 'size': 4, 'sha256': hashlib.sha256(b'mesh').hexdigest()}
        plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-steam-mesh-buffer-offsets',
                                    'template_sha256': MODULE.digest(template), 'records': [row]}))
        MODULE.verify_meshes(template, plan, intermediate)

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
