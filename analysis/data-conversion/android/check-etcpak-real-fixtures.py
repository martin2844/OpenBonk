#!/usr/bin/env python3
"""Verify the private Android raw-source probe output against PC mip hashes."""
import collections
import hashlib
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[3]
path = root / 'build/android/etcpak-arm64-study' / (sys.argv[1] if len(sys.argv) > 1 else 'real-source-fixtures')
records = json.loads((path / 'fixtures.json').read_text())
counts, textures, bad = collections.Counter(), set(), []
for item in records:
    fmt = item['source_format']
    if not (path / (item['key'] + '.source')).exists(): continue
    value = (path / (item['key'] + '.source.etc')).read_bytes()
    ok = hashlib.sha256(value).hexdigest() == item['expected_sha256']
    counts[(fmt, item['mode'], ok)] += 1
    textures.add(item['key'][:32])
    if not ok:
        bad.append(item['key'])
print('mips:', sum(counts.values()), 'textures:', len(textures), 'mismatches:', len(bad))
for key, count in sorted(counts.items()):
    print('source format %s, %s, match=%s: %s' % (*key, count))
if bad:
    raise SystemExit('mismatched mip hashes: ' + ', '.join(bad))
