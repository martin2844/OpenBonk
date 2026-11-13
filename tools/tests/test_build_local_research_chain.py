import importlib.util
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest

spec = importlib.util.spec_from_file_location('research_chain', Path(__file__).resolve().parents[1] / 'build-local-research-chain.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ResearchChainTests(unittest.TestCase):
    def args(self, root):
        paths = {name: root / name for name in ('output', 'steam_data', 'unity_editor', 'runtime_obb', 'runtime_apk',
                 'gradle_project', 'unity_project', 'json_jar', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home')}
        return SimpleNamespace(**paths, runner='steam-run', application_id='com.example.research')

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

    def test_missing_optional_inputs_are_generated_before_use(self):
        with tempfile.TemporaryDirectory() as temp:
            a = self.args(Path(temp))
            for name in ('steam_zip', 'host_library', 'android_library', 'decoder'): setattr(a, name, None)
            steps = module.commands(a); labels = [x[0] for x in steps]
            self.assertEqual(labels[:4], ['Steam ZIP', 'host texture converter', 'Android texture converter', 'Crunch decoder'])
            self.assertEqual(len(steps), 30)
            self.assertLess(labels.index('Crunch decoder'), labels.index('converted cubemaps'))


if __name__ == '__main__': unittest.main()
