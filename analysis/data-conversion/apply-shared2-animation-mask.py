#!/usr/bin/env python3
"""Apply position-only masks to the pinned private animation template."""
import base64
import hashlib
import json
from pathlib import Path
import sys
import zlib

BASE = 'fe677585c79002200881375cababe346a42b3a6a911763426264700a80712c27'
MASK = '63b61047b81ffc2029b86fa63153938203dc845123987e0eb3a30917478ab482'
RESULT = 'a6a91aeefb8095e2d096967220541eb1247a0fb27e08f1f799ee2cd71c410892'
OFFSET, PACKED = 2344643, 45527
if len(sys.argv) != 4: raise ValueError('usage: SCRIPT BASE MASK_JSON NEW_BASE')
src, mask_path, dest = map(Path, sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir(): raise ValueError('output must be new')
base, mask = bytearray(src.read_bytes()), mask_path.read_bytes()
if hashlib.sha256(base).hexdigest() != BASE or hashlib.sha256(mask).hexdigest() != MASK: raise ValueError('input differs')
rows = json.loads(mask)['objects']
if len(rows) != 8: raise ValueError('invalid inventory')
split = bytearray(zlib.decompress(base[OFFSET:OFFSET + PACKED], -15))
if len(split) != 1048576: raise ValueError('invalid split')
for row in rows:
    target, size = row['target'], row['size']
    positions = base64.b64decode(row['mask_base64'], validate=True)
    if len(positions) != 2 * row['preserved_positions'] or target < 0 or target + size > len(split): raise ValueError('invalid row')
    indices = [int.from_bytes(positions[i:i + 2], 'little') for i in range(0, len(positions), 2)]
    if indices != sorted(set(indices)) or any(i >= size for i in indices): raise ValueError('invalid mask')
    retained = set(indices)
    for i in range(size):
        if i not in retained: split[target + i] = 0
compressor = zlib.compressobj(6, zlib.DEFLATED, -15); encoded = compressor.compress(split) + compressor.flush()
if len(encoded) > PACKED: raise ValueError('compressed stream too large')
base[OFFSET:OFFSET + PACKED] = encoded + bytes(PACKED - len(encoded))
if hashlib.sha256(base).hexdigest() != RESULT: raise ValueError('output differs')
with dest.open('xb') as f: f.write(base)
print('Applied position-only masks; template SHA256', RESULT)
