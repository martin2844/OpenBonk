#!/usr/bin/env python3
"""Measure long byte matches between two Android shaders and Steam shaders.

This exploratory inventory exports no shader content. Matches do not establish
provenance or visual equivalence, and unmatched bytes are not necessarily unique.
"""
import argparse
import hashlib
import json
import zipfile
from pathlib import Path

import UnityPy

TARGETS = (
    ('1ccecd9b89b8a4f14bfb64f29ddfcc81', 'resources.assets', 'ProBuilder/Standard Vertex Color'),
    ('39354b8a5f5520b4e85f2c9fe7791c60', 'sharedassets1.assets', 'MK/Toon/Built-in/Standard/Simple'),
)


def shader(data, name):
    found = [obj.get_raw_data() for obj in UnityPy.load(data).objects
             if obj.type.name == 'Shader' and obj.read_typetree()['m_ParsedForm']['m_Name'] == name]
    if len(found) != 1:
        raise ValueError('expected exactly one shader named ' + name)
    return found[0]


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path)
    p.add_argument('linux_data', type=Path)
    p.add_argument('windows_zip', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink():
        p.error('output already exists')
    rows = []
    with zipfile.ZipFile(a.base) as base, zipfile.ZipFile(a.windows_zip) as windows:
        if len(set(base.namelist())) != len(base.namelist()) or len(set(windows.namelist())) != len(windows.namelist()):
            raise ValueError('duplicate ZIP entry')
        for entry, filename, name in TARGETS:
            target = shader(base.read('assets/bin/Data/' + entry), name)
            linux = shader((a.linux_data / filename).read_bytes(), name)
            win = shader(windows.read('Megabonk_Data/' + filename), name)
            measures = []
            for width in (16, 32):
                mask = bytearray(len(target))
                hits = 0
                for offset in range(0, len(target) - width + 1, 8):
                    block = target[offset:offset + width]
                    if sum(b != 0 for b in block) < width // 2:
                        continue
                    if linux.find(block) >= 0 and win.find(block) >= 0:
                        mask[offset:offset + width] = bytes([1]) * width
                        hits += 1
                measures.append({'window_bytes': width, 'stride_bytes': 8, 'matching_windows': hits,
                                 'covered_bytes': sum(mask),
                                 'covered_nonzero_bytes': sum(bool(m) and b != 0 for m, b in zip(mask, target))})
            rows.append({'name': name, 'entry': entry, 'android_bytes': len(target),
                         'android_nonzero_bytes': sum(b != 0 for b in target),
                         'android_sha256': digest(target), 'linux_sha256': digest(linux),
                         'windows_sha256': digest(win), 'measures': measures})
    report = {'objects': rows, 'limitations': [
        'Searches only aligned target windows with at least half nonzero bytes.',
        'Matches may occur at different offsets in the two Steam files.',
        'No full reconstruction, rights determination, or runtime equivalence is established.' ]}
    with a.output.open('x', encoding='utf-8') as out:
        json.dump(report, out, indent=2)
        out.write('\n')
    for row in rows:
        print(row['name'], row['android_nonzero_bytes'], 'nonzero bytes;',
              row['measures'][1]['covered_nonzero_bytes'], 'covered by 32-byte matches in both Steam versions')


if __name__ == '__main__':
    main()
