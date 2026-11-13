#!/usr/bin/env python3
"""Apply pinned position-only particle masks to a private launcher template."""
import base64
import hashlib
import json
from pathlib import Path
import sys
import zlib

BASE = '2b4d71ddf84456b17ae1624847def47acfc3d896c2cd2f67ff096016e9c99650'
MASK = '8565e5ae34615fa270c5984cbbb56e84c84127492b9d488f7809328d6fbfcf5c'
RESULT = '9060bf045d4da71d6a1663459361dc38c811a186683e3c92a482200163e41d79'
FIRST = 104 * 1048576
PARTS = ((1691102,54225,1048576), (1745438,38612,1048576))
if len(sys.argv) != 4: raise ValueError('usage: SCRIPT BASE MASK_JSON NEW_BASE')
src, mask_path, dest = map(Path, sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir(): raise ValueError('output must be new')
base, mask = bytearray(src.read_bytes()), mask_path.read_bytes()
if hashlib.sha256(base).hexdigest() != BASE or hashlib.sha256(mask).hexdigest() != MASK:
    raise ValueError('input differs')
plan = json.loads(mask); rows = plan['objects']
if len(rows) != 196 or plan['changed_nonzero'] != 307832: raise ValueError('invalid inventory')
parts = []
for off, packed, size in PARTS:
    value = zlib.decompress(base[off:off+packed], -15)
    if len(value) != size: raise ValueError('invalid split')
    parts.append(value)
content = bytearray(b''.join(parts)); ranges = []; changed = 0
for row in rows:
    start, size = row['target_offset'] - FIRST, row['size']
    if start < 0 or size <= 0 or start + size > len(content): raise ValueError('invalid particle range')
    ranges.append((start, start+size))
    bits = zlib.decompress(base64.b64decode(row['mask'], validate=True))
    if len(bits) != (size+7)//8: raise ValueError('invalid particle mask')
    for i in range(size):
        if bits[i//8] & (1 << (i%8)):
            if content[start+i] == 0: raise ValueError('particle byte unexpectedly zero')
            content[start+i] = 0; changed += 1
ranges.sort()
if any(a[1] > b[0] for a,b in zip(ranges, ranges[1:])) or changed != plan['changed_nonzero']:
    raise ValueError('invalid particle inventory')
cursor = 0
for off, reserved, size in PARTS:
    value = content[cursor:cursor+size]; cursor += size
    compressor = zlib.compressobj(6, zlib.DEFLATED, -15)
    encoded = compressor.compress(value) + compressor.flush()
    if len(encoded) > reserved: raise ValueError('compressed stream too large')
    base[off:off+reserved] = encoded + bytes(reserved-len(encoded))
if hashlib.sha256(base).hexdigest() != RESULT: raise ValueError('output differs')
with dest.open('xb') as f: f.write(base)
print('Applied particle masks; template SHA256', RESULT)
