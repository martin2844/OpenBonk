import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

SOURCE = Path(__file__).resolve().parents[1] / 'audit_pc_character.py'
spec = importlib.util.spec_from_file_location('audit_pc_character', SOURCE)
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class AuditPcCharacter(unittest.TestCase):
    def test_realistic_nested_metadata_reports_unsupported_features(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'mod.zip'
            data = {'character': {'characterName': 'Example', 'author': 'Example author',
                                  'statModifiers': [{'stat': 0, 'modification': 1}],
                                  'colliderHeight': 3, 'colliderWidth': 1},
                    'weapon': {'weaponName': 'Example weapon', 'weaponAttackPath': 'prefab'},
                    'passive': {'passiveName': 'Example passive'}}
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('characters/example.custom.json', json.dumps(data))
                archive.writestr('characters/model', b'not read by the audit')
            result = audit.inspect_package(path)
            self.assertEqual(result['stat_modifier_count'], 1)
            self.assertEqual(result['status'], 'manual_review_required')
            self.assertIn('character_specific_base_stats', result['requirements'])
            self.assertIn('character_specific_collider_review', result['requirements'])
            self.assertIn('custom_projectile_or_equivalent', result['requirements'])

    def test_ambiguous_and_oversized_metadata_are_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / 'mod.zip'
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('a.custom.json', '{}')
                archive.writestr('b.custom.json', '{}')
            with self.assertRaises(ValueError):
                audit.inspect_package(path)
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr('a.custom.json', b' ' * (audit.MAX_METADATA + 1))
            with self.assertRaises(ValueError):
                audit.inspect_package(path)


if __name__ == '__main__':
    unittest.main()
