#!/usr/bin/env python3
"""Extract two pinned locally compiled shaders into a private build-time pack."""
import argparse
import hashlib
from pathlib import Path
import zipfile
import UnityPy

EXPECTED = {
    'ProBuilder/Standard Vertex Color': ('1ccecd9b89b8a4f14bfb64f29ddfcc81', 132256,
        '76fd8d9a46b822c481eb4c7c9ba5d8cc43a05e241f4851cdff56e65ff2fef2a9'),
    'MK/Toon/Built-in/Standard/Simple': ('39354b8a5f5520b4e85f2c9fe7791c60', 74812,
        '87ba3bfa670fe3aa3c35df87b61e9af2bc6b8e0af23be90a01484fcb25a9625a'),
}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('bundle', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    found = {}
    for obj in UnityPy.load(str(a.bundle)).objects:
        if obj.type.name != 'Shader': continue
        name = obj.read_typetree()['m_ParsedForm']['m_Name']
        if name not in EXPECTED: continue
        if name in found: raise ValueError('duplicate shader')
        entry, size, expected = EXPECTED[name]
        data = obj.get_raw_data()
        if len(data) != size or hashlib.sha256(data).hexdigest() != expected:
            raise ValueError('generated shader differs: ' + name)
        found[name] = (entry, data)
    if set(found) != set(EXPECTED): raise ValueError('missing shader')
    created = False
    try:
        with a.output.open('xb') as output:
            created = True
            with zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as pack:
                for name in sorted(found):
                    entry, data = found[name]
                    info = zipfile.ZipInfo(entry, (1980, 1, 1, 0, 0, 0))
                    info.compress_type = zipfile.ZIP_DEFLATED
                    pack.writestr(info, data)
    except BaseException:
        if created: a.output.unlink(missing_ok=True)
        raise
    print('Packed two verified locally generated shaders; private research only.')


if __name__ == '__main__': main()
