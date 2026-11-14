import io
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile

SCRIPT = Path(__file__).resolve().parents[1] / 'inventory-launcher-template.py'
PREFIX = 'assets/openbonk-diagnostic/'
NAMES = ('animation', 'cubemap', 'generated-audio', 'mesh', 'object', 'packed-audio', 'texture')


def archive(entries):
    data = io.BytesIO()
    with zipfile.ZipFile(data, 'w') as z:
        for name, content in entries.items(): z.writestr(name, content)
    return data.getvalue()


class TemplateInventoryTest(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(); self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.apk = self.root / 'test.apk'; self.steam = self.root / 'steam.zip'; self.report = self.root / 'report.json'
        self.steam.write_bytes(archive({'game/file': b'abc'}))
        self.base = archive({'assets/bin/Data/example': b'abc', 'other': b'\x00z'})

    def run_tool(self, *options):
        return subprocess.run([sys.executable, str(SCRIPT), str(self.apk), str(self.steam), str(self.report), *options], capture_output=True, text=True)

    def test_windows_selects_its_own_template_and_plans(self):
        windows = PREFIX + 'windows/'
        entries = {PREFIX + 'generated-audio-template.obb': archive({'primary': b'else'}),
                   windows + 'generated-audio-template.obb': self.base}
        entries.update({PREFIX + name + '-plan.json': b'primary' for name in NAMES})
        entries.update({windows + name + '-plan.json': b'windows' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        result = self.run_tool('--platform', 'windows'); self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads(self.report.read_text())
        self.assertEqual(report['platform'], 'windows')
        self.assertEqual(report['base_entry'], windows + 'generated-audio-template.obb')
        self.assertEqual(report['base_nonzero_bytes'], 4)
        self.assertEqual(report['exact_steam_matches'], 1)
        self.assertTrue(all(row['entry'].startswith(windows) for row in report['plan_entries']))

    def test_missing_windows_template_is_rejected(self):
        entries = {PREFIX + 'generated-audio-template.obb': self.base}
        entries.update({PREFIX + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        self.assertNotEqual(self.run_tool('--platform', 'windows').returncode, 0)
        self.assertFalse(self.report.exists())

    def test_eight_stage_mode_requires_and_inventories_partial_plan(self):
        entries = {PREFIX + 'generated-audio-template.obb': self.base,
                   PREFIX + 'generated-audio-mode': b'OPENBONK_GENERATED_AUDIO_RESEARCH_V8'}
        entries.update({PREFIX + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        self.assertNotEqual(self.run_tool().returncode, 0); self.assertFalse(self.report.exists())
        entries[PREFIX + 'partial-rgba-plan.json'] = b'partial'
        self.apk.write_bytes(archive(entries))
        result = self.run_tool(); self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads(self.report.read_text())
        self.assertEqual(len(report['plan_entries']), 8)
        self.assertIn(PREFIX + 'partial-rgba-plan.json', [row['entry'] for row in report['plan_entries']])

    def test_current_direct_plans_and_template(self):
        entries = {PREFIX + 'generated-audio-template.obb': self.base}
        entries.update({PREFIX + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        result = self.run_tool(); self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads(self.report.read_text())
        self.assertEqual(report['base_entry'], PREFIX + 'generated-audio-template.obb')
        self.assertEqual(report['base_nonzero_bytes'], 4)
        self.assertEqual(report['exact_steam_matches'], 1)
        self.assertEqual(len(report['plan_entries']), 7)
        self.assertIsNone(report['plan_zip_sha256'])

    def test_compact_template_and_ambiguous_templates(self):
        entries = {PREFIX + 'generated-audio-template.openbonk': self.base}
        entries.update({PREFIX + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        result = self.run_tool(); self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(self.report.read_text())['base_entry'], PREFIX + 'generated-audio-template.openbonk')
        self.report.unlink()
        entries[PREFIX + 'generated-audio-template.obb'] = self.base
        self.apk.write_bytes(archive(entries))
        self.assertNotEqual(self.run_tool().returncode, 0)
        self.assertFalse(self.report.exists())

    def test_windows_compact_template(self):
        windows = PREFIX + 'windows/'
        entries = {windows + 'generated-audio-template.openbonk': self.base}
        entries.update({windows + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        result = self.run_tool('--platform', 'windows')
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(json.loads(self.report.read_text())['base_entry'], windows + 'generated-audio-template.openbonk')

    def test_legacy_layout_still_works(self):
        self.apk.write_bytes(archive({PREFIX + 'base.obb': self.base, PREFIX + 'plans.zip': archive({'plan.json': b'{}'})}))
        result = self.run_tool(); self.assertEqual(result.returncode, 0, result.stderr)
        report = json.loads(self.report.read_text())
        self.assertEqual(report['base_entry'], PREFIX + 'base.obb')
        self.assertEqual(len(report['plan_entries']), 1)
        self.assertIsNotNone(report['plan_zip_sha256'])

    def test_incomplete_and_ambiguous_layouts_are_rejected(self):
        self.apk.write_bytes(archive({PREFIX + 'generated-audio-template.obb': self.base}))
        self.assertNotEqual(self.run_tool().returncode, 0); self.assertFalse(self.report.exists())
        entries = {PREFIX + 'generated-audio-template.obb': self.base, PREFIX + 'base.obb': self.base, PREFIX + 'plans.zip': archive({})}
        entries.update({PREFIX + name + '-plan.json': b'{}' for name in NAMES})
        self.apk.write_bytes(archive(entries))
        self.assertNotEqual(self.run_tool().returncode, 0); self.assertFalse(self.report.exists())


if __name__ == '__main__': unittest.main()
