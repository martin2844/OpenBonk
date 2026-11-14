import importlib.util
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
            self.assertEqual(len(steps), 26)
            self.assertLess(labels.index('exact object plan'), labels.index('restore and verify objects'))
            self.assertLess(labels.index('restore and verify objects'), labels.index('bind animation plan'))
            self.assertLess(labels.index('restore and verify cubemaps'), labels.index('bind mesh plan'))
            self.assertLess(labels.index('restore and verify textures'), labels.index('bind packed audio plan'))
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

    def test_missing_optional_inputs_are_generated_before_use(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp))
            for name in ('steam_zip', 'host_library', 'android_library', 'decoder'): setattr(a, name, None)
            steps = module.commands(a); labels = [x[0] for x in steps]
            self.assertEqual(labels[:4], ['Steam ZIP', 'host texture converter', 'Android texture converter', 'Crunch decoder'])
            self.assertEqual(len(steps), 30)
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
            a.shadow_fallback = True
            self.assertIn('--shadow-fallback', dict(module.commands(a))['converted textures'])


if __name__ == '__main__': unittest.main()
