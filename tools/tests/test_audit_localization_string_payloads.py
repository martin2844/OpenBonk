import importlib.util
import json
from pathlib import Path
import struct
import tempfile
import unittest
import zipfile

spec = importlib.util.spec_from_file_location('localization_string_audit', Path(__file__).resolve().parents[1] / 'audit-localization-string-payloads.py')
module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)


def field(value):
    data = value.encode('utf-8')
    result = struct.pack('<I', len(data)) + data
    return result + bytes((-len(result)) % 4)


class LocalizationStringAuditTests(unittest.TestCase):
    def test_distinct_and_repeated_strings_are_located_by_serialized_length(self):
        body = field('cat') + field('caterpillar') + field('cat') + field('árbol')
        matches, skipped = module.string_offsets(body, ['cat', 'caterpillar', 'cat', 'árbol', ''])
        self.assertEqual(skipped, 0)
        self.assertEqual([data for _, data in matches], [b'cat', b'caterpillar', b'cat', 'árbol'.encode()])
        self.assertTrue(all(body[offset:offset + len(data)] == data for offset, data in matches))

    def test_ambiguous_duplicate_is_skipped_instead_of_guessed(self):
        body = field('cat') + field('cat')
        matches, skipped = module.string_offsets(body, ['cat'])
        self.assertEqual(matches, [])
        self.assertEqual(skipped, 1)

    def test_missing_or_invalid_value_is_not_silently_ignored(self):
        with self.assertRaises(ValueError):
            module.string_offsets(field('cat'), ['cat', None])

    def test_shared_keys_and_translated_values_are_separate_fields(self):
        tree = {'m_Entries': [{'m_Key': 'jump'}], 'm_TableData': [{'m_Localized': 'Jump'}]}
        self.assertEqual(module.table_values(tree, True), ['jump'])
        self.assertEqual(module.table_values(tree, False), ['Jump'])
        self.assertIsNone(module.table_values({}, True))
        with self.assertRaises(ValueError): module.table_values({'m_Entries': [{'m_Key': None}]}, True)

    def test_packaged_template_must_match_audited_entry(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); stage = root / 'stage.zip'; apk = root / 'test.apk'
            with zipfile.ZipFile(stage, 'w') as archive: archive.writestr('localization', b'correct')
            with zipfile.ZipFile(apk, 'w') as archive:
                archive.write(stage, 'assets/openbonk-diagnostic/generated-audio-template.openbonk')
            module.check_packaged_template(apk, stage, ['localization'], 'linux')
            with zipfile.ZipFile(stage, 'w') as archive: archive.writestr('localization', b'different')
            with self.assertRaises(ValueError): module.check_packaged_template(apk, stage, ['localization'], 'linux')
            with self.assertRaises(ValueError): module.check_packaged_template(apk, stage, ['localization'], 'windows')

    def test_plan_must_be_bound_to_the_template(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); template = root / 'template'; plan = root / 'plan.json'
            template.write_bytes(b'template')
            plan.write_text(json.dumps({'schema': 1, 'kind': 'experimental-localization-cab-obb',
                                        'platform': 'linux', 'template_sha256': '0' * 64}))
            with self.assertRaises(ValueError): module.audit(root / 'reference', template, plan)


if __name__ == '__main__': unittest.main()
