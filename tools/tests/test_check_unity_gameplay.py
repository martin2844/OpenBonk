import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('gameplay_checks', Path(__file__).resolve().parents[1] / 'check-unity-gameplay.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class GameplayCheckTests(unittest.TestCase):
    def test_requires_successful_exit_and_specific_success_marker(self):
        with tempfile.TemporaryDirectory() as temp:
            log = Path(temp) / 'check.log'
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 0, log)
            log.write_text('AnotherCheck PASS\n')
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 0, log)
            log.write_text('ExampleCheck PASS\n')
            with self.assertRaises(RuntimeError): module.check_result('ExampleCheck', 'PASS', 1, log)
            module.check_result('ExampleCheck', 'PASS', 0, log)


if __name__ == '__main__': unittest.main()
