import hashlib
import importlib.util
import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

SCRIPT = Path(__file__).resolve().parents[1] / 'prepare-unity-passive-icons.py'
spec = importlib.util.spec_from_file_location('passive_icons', SCRIPT)
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


class PassiveIconsTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name); self.project = self.root / 'workspace'
        (self.project / 'Assets/Texture2D').mkdir(parents=True)
        (self.project / 'Assets/MonoBehaviour').mkdir()
        (self.project / '.openbonk-workspace.json').write_text(json.dumps(module.MARKER))
        self.config = {}
        for index, (name, passive) in enumerate((('Bullseye', 0), ('Stonks', 18))):
            hashes = []
            for variant in range(2):
                png = self.project / 'Assets/Texture2D' / (name + ('_0' if variant else '') + '.png')
                payload = (name + str(variant)).encode(); png.write_bytes(payload)
                hashes.append(hashlib.sha256(payload).hexdigest())
                Path(str(png) + '.meta').write_text('guid: ' + (str(index * 2 + variant + 1) * 32) + '\n')
            self.config[name] = (passive, *hashes)
            (self.project / 'Assets/MonoBehaviour' / (name + '.asset')).write_text(
                f'MonoBehaviour:\n  m_Name: {name}\n  ePassive: {passive}\n'
                f'  icon: {{fileID: 2800000, guid: {str(index * 2 + 1) * 32}, type: 3}}\n  other: retained\n')
        self.backup = self.root / 'backup'

    def run_main(self, *args):
        with patch.object(module, 'ICONS', self.config), patch.object(sys, 'argv', [str(SCRIPT), '--project', str(self.project), *args]):
            module.main()

    def test_prepares_both_references_and_backs_up_originals(self):
        assets = self.project / 'Assets/MonoBehaviour'
        before = {p.name: p.read_bytes() for p in assets.iterdir()}
        with self.assertRaises(SystemExit): self.run_main('--check')
        self.run_main('--backup', str(self.backup))
        for index, name in enumerate(('Bullseye', 'Stonks')):
            self.assertEqual((self.backup / (name + '.asset')).read_bytes(), before[name + '.asset'])
            actual = (assets / (name + '.asset')).read_bytes()
            self.assertIn(('guid: ' + str(index * 2 + 2) * 32).encode(), actual)
            self.assertIn(b'  other: retained', actual)
        self.run_main('--check'); self.run_main()

    def test_unexpected_texture_or_reference_rejects_all_changes(self):
        assets = self.project / 'Assets/MonoBehaviour'
        before = {p.name: p.read_bytes() for p in assets.iterdir()}
        (self.project / 'Assets/Texture2D/Stonks_0.png').write_bytes(b'other')
        with self.assertRaises(SystemExit): self.run_main('--backup', str(self.backup))
        self.assertFalse(self.backup.exists())
        self.assertEqual(before, {p.name: p.read_bytes() for p in assets.iterdir()})


if __name__ == '__main__': unittest.main()
