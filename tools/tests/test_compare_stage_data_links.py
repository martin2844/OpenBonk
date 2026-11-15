import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location('stage_links', Path(__file__).resolve().parents[1] / 'compare-stage-data-links.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def inventories():
    header = {'schema': 1, 'unity_version': '2023.2.22f1'}
    def targets(kind, count):
        return {**header, kind: [{'source_group': f'{kind}-{i}', 'path_id': 1,
                                  'data': {'m_Name': f'{kind}-name-{i}'}} for i in range(count)]}
    stages = {**header, 'stages': [{'source_group': f'stage-{i}',
                                    'external_files': ['enemies-0', 'noise-0', 'map_parameters-0'],
                                    'data': {'m_Name': f'stage-{i}',
                                             'stageTimeline': {'boss': {'m_FileID': 1, 'm_PathID': 1}},
                                             'proceduralNoiseData': {'m_FileID': 2, 'm_PathID': 1},
                                             'mapParameters': {'m_FileID': 0, 'm_PathID': 0}}}
                                   for i in range(9)]}
    return [stages, targets('enemies', 53), targets('noise', 3), targets('map_parameters', 4)]


class StageLinksTests(unittest.TestCase):
    def test_resolves_null_and_changed_targets(self):
        reference = inventories()
        candidate = copy.deepcopy(reference)
        candidate[0]['stages'][0]['data']['mapParameters'] = {'m_FileID': 3, 'm_PathID': 1}
        result = module.compare(reference, candidate)
        self.assertEqual(result['reference_nonnull_links'], {'boss': 9, 'noise': 9})
        self.assertEqual(result['candidate_nonnull_links']['parameters'], 1)
        self.assertEqual(result['differences'][0]['stage'], 'stage-0')

    def test_rejects_unresolved_non_null_target(self):
        data = inventories()
        data[0]['stages'][0]['data']['stageTimeline']['boss']['m_PathID'] = 100
        with self.assertRaises(ValueError):
            module.stage_links(*data)


if __name__ == '__main__':
    unittest.main()
