import importlib.util
import hashlib
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch
import zipfile

spec = importlib.util.spec_from_file_location('research_chain', Path(__file__).resolve().parents[1] / 'build-local-research-chain.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ResearchChainTests(unittest.TestCase):
    def test_localization_stage_precedes_partial_and_binds_packager_inputs(self):
        with tempfile.TemporaryDirectory() as temp:
            args = self.args(Path(temp)); args.partial_rgba = True; args.localization_cab = True; args.localization_small = True
            steps = module.commands(args); labels = [name for name, _ in steps]; commands = dict(steps)
            self.assertLess(labels.index('normalize Android localization'), labels.index('individual Steam audio'))
            self.assertIn(str(args.output/'localization-normalized/converted.obb'), commands['individual Steam audio'])
            self.assertLess(labels.index('localization plan'), labels.index('restore and verify localization'))
            self.assertIn('--small-fragments', commands['normalize Android localization'])
            self.assertIn('--small-fragments', commands['localization plan'])
            self.assertLess(labels.index('restore and verify localization'), labels.index('bind partial RGBA plan'))
            self.assertLess(labels.index('bind partial RGBA plan'), labels.index('restore and verify partial RGBA'))
            self.assertIn(str(args.output/'canonical/partial-rgba-plan.json'), commands['restore and verify partial RGBA'])
            final = commands['research launcher APK']
            self.assertIn('--localization-plan', final)
            self.assertIn(str(args.output/'plans/localization/template.obb'), final)
            self.assertIn(str(args.output/'canonical/after-localization.obb'), final)

    def test_generated_zip_preflight_rejects_unpinned_input(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); data = root / 'data'
            (data / 'StreamingAssets/aa/StandaloneLinux64').mkdir(parents=True)
            inventory = {}
            for index in range(39):
                name = f'file-{index}'
                (data / name).write_bytes(b'known')
                inventory[name] = {'size': 5, 'sha256': hashlib.sha256(b'known').hexdigest()}
            manifest = root / 'inputs.json'
            manifest.write_text(json.dumps({'platforms': {'linux': inventory}}))
            with patch.object(module, 'STEAM_INPUTS', manifest):
                module.validate_steam_inputs(data)
                (data / 'file-1').write_bytes(b'other')
                with self.assertRaisesRegex(ValueError, 'unsupported or damaged Steam input'):
                    module.validate_steam_inputs(data)

    def test_shared0_option_enables_both_font_stages(self):
        with tempfile.TemporaryDirectory() as temp:
            args = self.args(Path(temp)); args.shared0_fonts = True
            commands = dict(module.commands(args))
            self.assertIn('--shared0-sdf', commands['converted textures'])
            self.assertIn('--shared0-fonts', commands['animation plan'])
            self.assertIn('--font-payloads', commands['exact object plan'])
            self.assertIn('--shared0-cubemap', commands['exact object plan'])

    def test_online_flag_reaches_packager(self):
        with tempfile.TemporaryDirectory() as temp:
            args = self.args(Path(temp))
            self.assertNotIn('--gradle-online', module.commands(args)[-1][1])
            args.gradle_online = True
            self.assertIn('--gradle-online', module.commands(args)[-1][1])

    def test_version_overrides_reach_packager(self):
        with tempfile.TemporaryDirectory() as temp:
            args = self.args(Path(temp)); args.version_code = 2; args.version_name = '0.1.1-beta'
            command = module.commands(args)[-1][1]
            self.assertEqual(command[command.index('--version-code') + 1], '2')
            self.assertEqual(command[command.index('--version-name') + 1], '0.1.1-beta')

    def test_optional_animation_curves_feed_audio_plan_and_final_check(self):
        with tempfile.TemporaryDirectory() as temp:
            args = self.args(Path(temp))
            self.assertNotIn('compatible Steam animation curves', dict(module.commands(args)))
            args.animation_curve_fallback = True
            args.animation_curve_relaxed = True
            steps = module.commands(args); labels = [name for name, _ in steps]
            self.assertIn('--relaxed', dict(steps)['compatible Steam animation curves'])
            args.animation_curve_relaxed = False; args.animation_curve_extended = True
            self.assertIn('--extended', dict(module.commands(args))['compatible Steam animation curves'])
            args.animation_curve_hybrid = True
            self.assertIn('--hybrid', dict(module.commands(args))['compatible Steam animation curves'])
            self.assertLess(labels.index('converted cubemaps'), labels.index('compatible Steam animation curves'))
            self.assertLess(labels.index('compatible Steam animation curves'), labels.index('individual audio plan'))
            commands = dict(steps)
            expected = str(args.output / 'animation-curve-study/converted.obb')
            self.assertIn(expected, commands['individual audio plan'])
            self.assertIn(expected, commands['restore and verify individual audio'])

    def test_preflight_checks_zip_against_local_linux_data(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp); data = root / 'data'; bundle = data / 'StreamingAssets/aa/StandaloneLinux64'
            bundle.mkdir(parents=True); (bundle / 'table.bundle').write_bytes(b'locale')
            (data / 'sharedassets1.assets').write_bytes(b'shared')
            manifest = root / 'inputs.json'
            manifest.write_text(json.dumps({'platforms': {'linux': {
                'sharedassets1.assets': {}, 'StreamingAssets/aa/StandaloneLinux64/table.bundle': {}}, 'windows': {
                'sharedassets1.assets': {}, 'StreamingAssets/aa/StandaloneWindows64/table.bundle': {}}}}))
            archive = root / 'steam.zip'
            def write_zip(shared):
                with zipfile.ZipFile(archive, 'w') as z:
                    z.writestr('Megabonk_Data/sharedassets1.assets', shared)
                    z.writestr('Megabonk_Data/StreamingAssets/aa/StandaloneLinux64/table.bundle', b'locale')
            with patch.object(module, 'STEAM_INPUTS', manifest):
                write_zip(b'shared'); module.validate_steam_inputs(data, archive)
                with patch.object(module, 'MAX_STEAM_ZIP_BYTES', archive.stat().st_size - 1):
                    with self.assertRaisesRegex(ValueError, 'launcher size limit'):
                        module.validate_steam_inputs(data, archive)
                write_zip(b'broken')
                with self.assertRaisesRegex(ValueError, 'differs'): module.validate_steam_inputs(data, archive)
                bundle.rename(data / 'StreamingAssets/aa/StandaloneWindows64')
                with self.assertRaisesRegex(ValueError, 'requires linux'): module.validate_steam_inputs(data, archive)
                with zipfile.ZipFile(archive, 'w') as z:
                    z.writestr('Megabonk_Data/sharedassets1.assets', b'shared')
                    z.writestr('Megabonk_Data/StreamingAssets/aa/StandaloneWindows64/table.bundle', b'locale')
                module.validate_steam_inputs(data, archive, 'windows')

    def args(self, root):
        paths = {name: root / name for name in ('output', 'steam_data', 'unity_editor', 'runtime_obb', 'runtime_apk',
                 'gradle_project', 'unity_project', 'json_jar', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home')}
        return SimpleNamespace(**paths, runner='steam-run', application_id='com.example.research', platform='linux')

    def test_stages_build_seven_stage_chain_in_order(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp)); steps = module.commands(a); labels = [x[0] for x in steps]
            self.assertEqual(len(steps), 27)
            self.assertLess(labels.index('exact object plan'), labels.index('restore and verify objects'))
            self.assertLess(labels.index('restore and verify objects'), labels.index('bind animation plan'))
            self.assertLess(labels.index('restore and verify cubemaps'), labels.index('bind mesh plan'))
            self.assertLess(labels.index('restore and verify textures'), labels.index('bind packed audio plan'))
            self.assertLess(labels.index('restore and verify individual audio'), labels.index('verify video resource'))
            self.assertLess(labels.index('verify video resource'), labels.index('research launcher APK'))
            self.assertEqual(labels[-1], 'research launcher APK')
            build = steps[-1][1]
            for option in ('--object-plan', '--animation-plan', '--cubemap-plan', '--mesh-plan', '--texture-plan', '--packed-plan', '--audio-plan'):
                self.assertIn(option, build)
            self.assertEqual(build[build.index('--audio-template') + 1], str(a.output / 'plans/objects/template.obb'))

    def test_partial_rgba_is_restored_before_object_plan(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp)); a.partial_rgba = True
            steps = module.commands(a); labels = [label for label, _ in steps]
            self.assertLess(labels.index('exact object plan'), labels.index('partial RGBA plan'))
            self.assertLess(labels.index('restore and verify partial RGBA'), labels.index('bind object plan'))
            self.assertLess(labels.index('bind object plan'), labels.index('restore and verify objects'))
            build = steps[-1][1]
            self.assertEqual(build[build.index('--audio-template') + 1], str(a.output / 'plans/partial-rgba/template.obb'))
            self.assertEqual(build[build.index('--object-plan') + 1], str(a.output / 'canonical/objects-plan.json'))
            self.assertIn('--partial-rgba-plan', build)
            a.partial_etc2 = True
            steps = dict(module.commands(a))
            self.assertIn('--etc2-quality-library', steps['partial RGBA plan'])
            self.assertIn('--etc2-quality-library', steps['bind Android partial RGBA plan'])
            a.partial_etc2_small = True
            steps = dict(module.commands(a))
            self.assertIn('--include-small-etc2', steps['partial RGBA plan'])
            self.assertNotIn('--include-small-etc2', steps['bind Android partial RGBA plan'])
            a.partial_etc2_fast = True
            steps = dict(module.commands(a))
            self.assertIn('--fast-etc2', steps['partial RGBA plan'])
            self.assertNotIn('--fast-etc2', steps['bind Android partial RGBA plan'])
            a.partial_etc2_blend = True
            steps = dict(module.commands(a))
            self.assertIn('--blend-etc2', steps['partial RGBA plan'])
            a.partial_etc2_byte = True
            steps = dict(module.commands(a))
            self.assertIn('--byte-etc2', steps['partial RGBA plan'])
            a.transparent_atlas_scrub = True
            steps = dict(module.commands(a))
            self.assertEqual(steps['individual audio plan'][steps['individual audio plan'].index('--obb') + 1],
                             str(a.output / 'transparent-atlas-study/converted.obb'))

    def test_missing_optional_inputs_are_generated_before_use(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp))
            for name in ('steam_zip', 'host_library', 'android_library', 'decoder'): setattr(a, name, None)
            steps = module.commands(a); labels = [x[0] for x in steps]
            self.assertEqual(labels[:4], ['Steam ZIP', 'host texture converter', 'Android texture converter', 'Crunch decoder'])
            self.assertEqual(len(steps), 31)
            self.assertLess(labels.index('Crunch decoder'), labels.index('converted cubemaps'))
            a.platform = 'windows'
            first = module.commands(a)[0][1]
            self.assertEqual(first[first.index('--platform') + 1], 'windows')

    def test_quality_mode_builds_and_uses_both_quality_libraries(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp)); a.quality_textures = True
            a.host_library = None; a.android_library = None
            steps = dict(module.commands(a))
            self.assertTrue(steps['host texture converter'][1].endswith('build-etc2-quality-jni.py'))
            self.assertIn('--target', steps['Android texture converter'])
            for name in ('converted textures', 'texture plan'):
                self.assertIn('--quality-library', steps[name])
            a.normal_fallback = True
            self.assertIn('--normal-fallback', dict(module.commands(a))['converted textures'])
            a.font_fallback = True
            self.assertIn('--font-fallback', dict(module.commands(a))['converted textures'])
            a.resource_fallback = True
            self.assertIn('--resource-fallback', dict(module.commands(a))['converted textures'])
            a.nodraw_fallback = True
            self.assertIn('--nodraw-fallback', dict(module.commands(a))['converted textures'])
            a.atlas_fallback = True
            self.assertIn('--atlas-fallback', dict(module.commands(a))['converted textures'])
            a.lightmap_fallback = True
            self.assertIn('--lightmap-fallback', dict(module.commands(a))['converted textures'])
            a.emission_fallback = True
            self.assertIn('--emission-fallback', dict(module.commands(a))['converted textures'])
            a.fox_fallback = True
            self.assertIn('--fox-fallback', dict(module.commands(a))['converted textures'])
            a.bounded_fallback = True
            self.assertIn('--bounded-fallback', dict(module.commands(a))['converted textures'])
            a.shadow_fallback = True
            self.assertIn('--shadow-fallback', dict(module.commands(a))['converted textures'])
            a.emoji_fallback = True
            self.assertIn('--emoji-fallback', dict(module.commands(a))['converted textures'])


if __name__ == '__main__': unittest.main()
