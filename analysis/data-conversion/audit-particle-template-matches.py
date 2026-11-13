#!/usr/bin/env python3
"""Compare named particle objects in a private template and two Steam assets.

Counts equal positions, not semantic equivalence, provenance, or rights.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile
import zlib

import UnityPy


def sha(data): return hashlib.sha256(data).hexdigest()


def particles(data):
    env = UnityPy.load(data)
    objects = list(env.objects)
    names = {o.path_id: o.read_typetree()['m_Name'] for o in objects if o.type.name == 'GameObject'}
    result = defaultdict(list)
    for obj in objects:
        if obj.type.name != 'ParticleSystem': continue
        tree = obj.read_typetree()
        ref = tree['m_GameObject']
        if ref['m_FileID'] != 0 or ref['m_PathID'] not in names: continue
        name = names[ref['m_PathID']]
        raw = obj.get_raw_data()
        result[(name, len(raw))].append((obj.path_id, obj.byte_start, raw))
    return result


def template(path, asset='sharedassets2.assets'):
    raw = path.read_bytes()
    with zipfile.ZipFile(path) as archive:
        entries = []
        for entry in archive.infolist():
            m = re.fullmatch(r'assets/bin/Data/' + re.escape(asset) + r'\.split(\d+)', entry.filename)
            if m: entries.append((int(m.group(1)), entry))
        entries.sort()
        if not entries or [i for i, _ in entries] != list(range(len(entries))):
            raise ValueError('invalid splits')
        chunks = []
        for _, info in entries:
            if info.compress_type != zipfile.ZIP_DEFLATED or info.flag_bits != 0:
                raise ValueError('unsupported split')
            off = info.header_offset
            if raw[off:off+4] != b'PK\x03\x04': raise ValueError('invalid header')
            n, e = struct.unpack_from('<HH', raw, off+26); off += 30+n+e
            part = zlib.decompress(raw[off:off+info.compress_size], -15)
            if len(part) != info.file_size: raise ValueError('invalid split size')
            chunks.append(part)
    return b''.join(chunks)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path); p.add_argument('linux_asset', type=Path)
    p.add_argument('windows_zip', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--asset', choices=('sharedassets1.assets', 'sharedassets2.assets'), default='sharedassets2.assets')
    p.add_argument('--best-candidates', action='store_true')
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink(): p.error('output must be new')
    target = template(a.base, a.asset); linux = a.linux_asset.read_bytes()
    with zipfile.ZipFile(a.windows_zip) as z:
        matches = [i for i in z.infolist() if i.filename == 'Megabonk_Data/' + a.asset]
        if len(matches) != 1: raise ValueError('missing Windows asset')
        windows = z.read(matches[0])
    a_targets, a_linux, a_windows = particles(target), particles(linux), particles(windows)
    counts = Counter(); rows = []
    for key, candidates in sorted(a_targets.items()):
        name, size = key
        l, w = a_linux[key], a_windows[key]
        for ident, pos, before in candidates:
            counts['particles'] += 1
            if not l or not w or (not a.best_candidates and (len(l) != 1 or len(w) != 1)):
                counts['ambiguous_or_missing'] += 1
                continue
            def select(options):
                return max(options, key=lambda x: (sum(a != 0 and a == b for a,b in zip(before,x[2])), -x[0]))
            lc, wc = select(l), select(w)
            left, right = lc[2], wc[2]
            equal = sum(x == y == z for x, y, z in zip(before, left, right))
            nonzero = sum(x != 0 and x == y == z for x, y, z in zip(before, left, right))
            counts['matched_candidates'] += 1
            counts['matching_nonzero_bytes'] += nonzero
            rows.append({'name': name, 'size': size, 'target_id': ident, 'target_offset': pos,
                         'linux_id': lc[0], 'windows_id': wc[0], 'linux_candidates': len(l), 'windows_candidates': len(w),
                         'three_way_equal_positions': equal, 'three_way_equal_nonzero': nonzero})
    report = {'template_sha256': sha(a.base.read_bytes()), 'linux_sha256': sha(linux),
              'windows_sha256': sha(windows), 'counts': dict(counts), 'objects': rows,
              'limitations': ['GameObject name, object size and optional best byte match are heuristics, not identity proofs.',
                              'Equal bytes do not prove rights or that a reconstructed particle effect is equivalent.',
                              'Only the selected local shared asset objects are examined.']}
    with a.output.open('x') as out: json.dump(report, out, indent=2); out.write('\n')
    print('particles', counts['particles'], 'matched target particles', counts['matched_candidates'],
          'three-way matching nonzero bytes', counts['matching_nonzero_bytes'])


if __name__ == '__main__': main()
