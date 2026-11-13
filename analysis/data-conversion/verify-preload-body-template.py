#!/usr/bin/env python3
"""Independently compare a preload body template with the original archive."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

import UnityPy


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('obb', type=Path)
    p.add_argument('template', type=Path)
    p.add_argument('restored', type=Path)
    p.add_argument('report', type=Path)
    a = p.parse_args()
    if a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():
        p.error('report must be new')
    manifest = json.loads((a.template / 'manifest.json').read_text())
    if manifest.get('version') != 3:
        raise ValueError('expected the extra-component manifest')
    with a.obb.open('rb') as f:
        if hashlib.file_digest(f, 'sha256').hexdigest() != manifest['obb_sha256']:
            raise ValueError('original OBB differs')
    counts = {'files': 0, 'objects': 0, 'zeroed_bytes': 0, 'nonzero_bytes': 0}
    with zipfile.ZipFile(a.obb) as z:
        if len(z.namelist()) != len(set(z.namelist())):
            raise ValueError('duplicate archive entry')
        for name, record in manifest['files'].items():
            if not re.fullmatch('[0-9a-f]{32}', name):
                raise ValueError('invalid asset name')
            original = z.read('assets/bin/Data/' + name)
            blank = (a.template / name).read_bytes()
            restored = (a.restored / name).read_bytes()
            if (sha(original) != record['original_sha256'] or sha(blank) != record['template_sha256']
                    or original != restored or len(original) != len(blank)):
                raise ValueError('asset mismatch: ' + name)
            objects = {o.path_id: o for o in UnityPy.load(original).objects}
            ranges = []
            for item in record['objects']:
                obj = objects[item['target_id']]
                start = obj.byte_start + 12
                end = start + item['body_size']
                if obj.type.name != item['type'] or start < 12 or end < start or end > len(original) or any(blank[start:end]):
                    raise ValueError('invalid selected body: ' + name)
                ranges.append((start, end))
            cursor = 0
            for start, end in sorted(ranges):
                if start < cursor or original[cursor:start] != blank[cursor:start]:
                    raise ValueError('unexpected change outside selected bodies: ' + name)
                counts['zeroed_bytes'] += end - start
                counts['nonzero_bytes'] += sum(byte != 0 for byte in original[start:end])
                counts['objects'] += 1
                cursor = end
            if original[cursor:] != blank[cursor:]:
                raise ValueError('unexpected trailing change: ' + name)
            counts['files'] += 1
    if counts['files'] != 68 or counts['objects'] != 635:
        raise ValueError('unexpected coverage')
    with a.report.open('x') as f:
        json.dump({'counts': counts, 'limitations': 'Byte comparison only; no rights or runtime equivalence conclusion.'}, f, indent=2)
        f.write('\n')
    print(counts)


if __name__ == '__main__':
    main()
