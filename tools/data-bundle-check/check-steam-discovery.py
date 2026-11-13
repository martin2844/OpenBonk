#!/usr/bin/env python3
"""Exercise library discovery without touching any real Steam library."""
import importlib.util
from pathlib import Path
import tempfile

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('steam_finder', root/'tools/find-local-steam-data.py')
finder = importlib.util.module_from_spec(spec); spec.loader.exec_module(finder)

with tempfile.TemporaryDirectory() as tmp:
    base = Path(tmp); primary = base/'Steam Home'; other = base/'Library With Spaces'
    manifest = primary/'steamapps/libraryfolders.vdf'; manifest.parent.mkdir(parents=True)
    manifest.write_text('"libraryfolders"\n{\n "0" {\n "path" "' + str(primary) + '"\n }\n'
                        ' "1" {\n "path" "' + str(other) + '"\n }\n}\n')
    copy = other/finder.APP_DIR; copy.mkdir(parents=True)
    def verify(path):
        if not (path/'compatible').is_file(): raise ValueError('unsupported version')
    try:
        finder.discover([primary], verify)
    except ValueError:
        pass
    else:
        raise AssertionError('unsupported copy accepted')
    (copy/'compatible').touch()
    assert finder.discover([primary, other], verify) == copy.resolve()
    second = primary/finder.APP_DIR; second.mkdir(parents=True); (second/'compatible').touch()
    try:
        finder.discover([primary], verify)
    except ValueError as exc:
        assert '--steam-data' in str(exc)
    else:
        raise AssertionError('multiple compatible copies selected silently')
print('Steam library discovery: PASS')
