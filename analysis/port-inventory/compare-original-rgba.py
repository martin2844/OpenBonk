#!/usr/bin/env python3
"""Compare decoded PC and packaged textures; matches are evidence, not rights clearance."""
import collections
import hashlib
import json
import re
import sys
import tempfile
import zipfile
import zlib
from pathlib import Path

import UnityPy

if len(sys.argv) != 3:
    raise SystemExit('usage: compare-original-rgba.py PC_DATA_DIR ANDROID_APK_OR_OBB')
pc, archive = map(Path, sys.argv[1:])
if not pc.is_dir() or not archive.is_file():
    raise SystemExit('missing input directory or archive')

data = {}

def collect(path, records, errors):
    try:
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name != 'Texture2D':
                continue
            try:
                tex = obj.read()
                image = tex.image.convert('RGBA')
                raw = image.tobytes()
                digest = hashlib.sha256(raw).hexdigest()
                data.setdefault(digest, zlib.compress(raw, 1))
                records[(tex.m_Name, *image.size)].add(digest)
            except Exception as exc:
                errors.append((path.name, obj.path_id, type(exc).__name__))
    except Exception as exc:
        errors.append((path.name, None, type(exc).__name__))

left, right = collections.defaultdict(set), collections.defaultdict(set)
pc_errors, android_errors = [], []
for path in [pc / 'resources.assets', *sorted(pc.glob('sharedassets*.assets')), *sorted(pc.glob('level[0-9]*'))]:
    if path.is_file():
        collect(path, left, pc_errors)

with tempfile.TemporaryDirectory(prefix='openbonk-rgba-audit-') as tmp, zipfile.ZipFile(archive) as z:
    names = z.namelist()
    groups = set()
    for name in names:
        match = re.fullmatch(r'assets/bin/Data/(sharedassets\d+\.assets)(?:\.split\d+)?', name)
        if match:
            groups.add(match.group(1))
    for group in sorted(groups):
        stem = 'assets/bin/Data/' + group
        parts = sorted((n for n in names if re.fullmatch(re.escape(stem) + r'\.split\d+', n)),
                       key=lambda n: int(n.rsplit('split', 1)[1]))
        if not parts:
            parts = [stem]
        path = Path(tmp) / group
        with path.open('wb') as out:
            for part in parts:
                out.write(z.read(part))
        resource = 'assets/bin/Data/' + group.replace('.assets', '.resource')
        if resource in names:
            (Path(tmp) / Path(resource).name).write_bytes(z.read(resource))
        collect(path, right, android_errors)
    for name in names:
        if not re.fullmatch(r'assets/bin/Data/[a-f0-9]{32}', name):
            continue
        path = Path(tmp) / Path(name).name
        path.write_bytes(z.read(name))
        if name + '.resource' in names:
            (Path(tmp) / (path.name + '.resource')).write_bytes(z.read(name + '.resource'))
        collect(path, right, android_errors)

common = sorted(left.keys() & right.keys())
if not common:
    raise SystemExit('no comparable textures: inspect inputs and decoder errors')
counts = collections.Counter()
for name, width, height in common:
    key = (name, width, height)
    if len(left[key]) != 1 or len(right[key]) != 1:
        counts['ambiguous'] += 1
        continue
    a = zlib.decompress(data[next(iter(left[key]))])
    b = zlib.decompress(data[next(iter(right[key]))])
    if len(a) != len(b):
        raise SystemExit('decoded byte lengths differ unexpectedly')
    exact = a == b
    error = sum(abs(x-y) for x, y in zip(a, b)) / len(a) if a else 0
    counts['exact' if exact else 'different'] += 1
    print(json.dumps({'name': name, 'width': width, 'height': height, 'rgba_exact': exact,
                      'mean_absolute_channel_difference': round(error, 4)}, ensure_ascii=False))
print('SUMMARY', json.dumps({'pc_keys': len(left), 'android_keys': len(right), 'common': len(common),
                             'counts': counts, 'pc_errors': len(pc_errors), 'android_errors': len(android_errors)}))
print('ERRORS', json.dumps({'pc': pc_errors[:15], 'android': android_errors[:15]}))
print('SCOPE: decoded RGBA; matched by name and dimensions; excludes unmatched/ambiguous objects, other data types and containers; does not establish licensing or provenance.')
