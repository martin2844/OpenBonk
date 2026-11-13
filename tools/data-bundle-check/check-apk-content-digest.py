#!/usr/bin/env python3
"""Exercise signature independence and payload integrity on small ZIP fixtures."""
import importlib.util
from pathlib import Path
import tempfile
import zipfile

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('apk_content_digest', root/'tools/apk-content-digest.py')
digest = importlib.util.module_from_spec(spec); spec.loader.exec_module(digest)


def make(path, payload=b'original', signature=b'first', compression=zipfile.ZIP_STORED, duplicate=False):
    with zipfile.ZipFile(path, 'w') as z:
        z.writestr('classes.dex', payload, compress_type=compression)
        z.writestr('META-INF/TEST.RSA', signature)
        if duplicate:
            z.writestr('classes.dex', b'second')


with tempfile.TemporaryDirectory() as tmp:
    tmp = Path(tmp)
    original, resigned, changed, recompressed, duplicate = (tmp/name for name in ('a.apk', 'b.apk', 'c.apk', 'd.apk', 'e.apk'))
    make(original); make(resigned, signature=b'other signature'); make(changed, payload=b'altered')
    make(recompressed, compression=zipfile.ZIP_DEFLATED)
    import warnings
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', UserWarning)
        make(duplicate, duplicate=True)
    expected = digest.digest(original)
    assert digest.digest(resigned) == expected
    assert digest.digest(changed) != expected
    assert digest.digest(recompressed) != expected
    try:
        digest.digest(duplicate)
    except ValueError:
        pass
    else:
        raise AssertionError('duplicate archive entries accepted')
    print('APK payload digest: PASS')
