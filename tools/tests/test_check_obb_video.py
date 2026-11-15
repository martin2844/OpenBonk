import importlib.util
from pathlib import Path
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / 'check-obb-video.py'
SPEC = importlib.util.spec_from_file_location('check_obb_video', SCRIPT)
module = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(module)


class VideoResourceTests(unittest.TestCase):
    def test_accepts_exact_clip_and_rejects_stale_or_invalid_offsets(self):
        resource = b'head' + b'video' + b'tail'
        module.check_payload(resource, 4, 5, b'video')
        for offset, size in ((3, 5), (4, 4), (-1, 5), (12, 5), (True, 5)):
            with self.subTest(offset=offset, size=size), self.assertRaises(ValueError):
                module.check_payload(resource, offset, size, b'video')


if __name__ == '__main__':
    unittest.main()
