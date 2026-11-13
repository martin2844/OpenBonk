"""Ensure a private isolated workspace includes every pinned runtime input."""
import importlib.util
import json
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


class IsolatedWorkspaceInventory(unittest.TestCase):
    def test_runtime_pins_are_in_isolated_inventory(self):
        spec = importlib.util.spec_from_file_location('runtime_pins_test', ROOT/'tools/build-current-runtime-diagnostic.py')
        runtime = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(runtime)
        files = json.loads((ROOT/'tools/isolated-workspace-inputs.json').read_text())['files']
        missing = {str(path.relative_to(ROOT)): digest for path, digest in runtime.PINS.items()
                   if files.get(str(path.relative_to(ROOT))) != digest}
        self.assertEqual(missing, {})


if __name__ == '__main__': unittest.main()
