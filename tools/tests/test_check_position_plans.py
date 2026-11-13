import importlib.util
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('check_position_plans', Path(__file__).resolve().parents[1] / 'check-position-plans.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class PositionPlanTests(unittest.TestCase):
    def test_checked_in_plans(self):
        module.main()

    def test_corrupted_plan_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            name = module.PLANS[0][0]
            shutil.copyfile(module.ROOT / name, root / name)
            with (root / name).open('r+b') as f:
                f.seek(100)
                value = f.read(1)
                f.seek(100)
                f.write(bytes([value[0] ^ 1]))
            with patch.object(module, 'ROOT', root), self.assertRaises(ValueError):
                module.validate(*module.PLANS[0])


if __name__ == '__main__':
    unittest.main()
