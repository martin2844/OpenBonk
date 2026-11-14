import subprocess
import sys
import importlib.util
import tempfile
from pathlib import Path
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'check-research-chain-android.py'
spec = importlib.util.spec_from_file_location('device_chain', SCRIPT)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class DeviceChainPreflightTests(unittest.TestCase):
    def test_partial_stage_precedes_rebound_objects(self):
        root = Path('/local/research')
        stages = module.stages_for(root, True)
        self.assertEqual([stage[0] for stage in stages[:2]], ['partial RGBA', 'objects'])
        self.assertTrue(stages[0][4])
        self.assertEqual(stages[1][2], root / 'canonical/objects-plan.json')
        self.assertEqual(len(module.stages_for(root, False)), 7)

    def test_invalid_device_is_rejected_before_accessing_inputs(self):
        args = [sys.executable, str(SCRIPT), '--research', '/missing', '--steam-zip', '/missing',
                '--library', '/missing', '--sdk', '/missing', '--java-home', '/missing', '--device', 'bad;command']
        result = subprocess.run(args, capture_output=True, text=True)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('invalid device serial', result.stderr)

    def test_incomplete_research_build_is_rejected_before_device_transfer(self):
        with tempfile.TemporaryDirectory() as directory:
            args = [sys.executable, str(SCRIPT), '--research', directory, '--steam-zip', directory,
                    '--library', directory, '--sdk', directory, '--java-home', directory, '--device', 'test-device']
            result = subprocess.run(args, capture_output=True, text=True)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('research build or tool inputs are incomplete', result.stderr)


if __name__ == '__main__': unittest.main()
