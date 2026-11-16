import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).resolve().parents[1]/'build-workspace-shader-cache.py'
spec = importlib.util.spec_from_file_location('workspace_shader_cache_test', SCRIPT)
builder = importlib.util.module_from_spec(spec); spec.loader.exec_module(builder)


class WorkspaceShaderCacheTest(unittest.TestCase):
    def test_copy_is_independent(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); source = root/'source'; target = root/'target'
            source.write_bytes(b'original'); builder.safe_copy(source, target); target.write_bytes(b'changed')
            self.assertEqual(source.read_bytes(), b'original')
            with self.assertRaises(FileExistsError): builder.safe_copy(source, target)
            self.assertEqual(target.read_bytes(), b'changed')

    def test_preflight_rejects_unmarked_or_linked_workspace(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); project = root/'project'; project.mkdir(); unity = root/'unity'; unity.write_text('fixture'); unity.chmod(0o700)
            def check():
                return subprocess.run([sys.executable, str(SCRIPT), '--project', str(project), '--unity', str(unity),
                                       '--output', str(root/'output'), '--check'], capture_output=True, text=True)
            self.assertNotEqual(check().returncode, 0)
            (project/'.openbonk-workspace.json').write_text(json.dumps(builder.MARKER))
            for name in ('Assets', 'Packages', 'ProjectSettings', 'Library'): (project/name).mkdir()
            (project/'ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
            (project/'Assets/link').symlink_to(unity)
            result = check(); self.assertNotEqual(result.returncode, 0); self.assertIn('symbolic link', result.stderr)
            self.assertFalse((root/'output').exists())


if __name__ == '__main__': unittest.main()
