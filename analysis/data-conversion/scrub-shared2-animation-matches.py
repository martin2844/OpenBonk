#!/usr/bin/env python3
"""Blank Steam-identical bytes in eight private animation objects.

The emitted mask records byte positions only; differing bytes remain in the
template. The template still contains other game-derived material.
"""
import base64
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile
import zlib

BASE = 'fe677585c79002200881375cababe346a42b3a6a911763426264700a80712c27'
LINUX = 'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da'
WINDOWS = 'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'
ROWS = ((447,793920,8216,2358376,2037408),(448,802136,8028,2412192,2091224),
        (449,810168,9900,2420224,2099256),(450,820072,3800,2379784,2058816),
        (451,823872,3188,2383584,2062616),(452,827064,3000,2386776,2065808),
        (454,832368,3204,2389776,2068808),(457,1041008,3732,2392984,2072016))
OFFSET, PACKED = 2344643, 45527

def sha(value): return hashlib.sha256(value).hexdigest()
def source(path, expected):
    with zipfile.ZipFile(path) as z:
        matches = [i for i in z.infolist() if i.filename == 'Megabonk_Data/sharedassets2.assets']
        if len(matches) != 1: raise ValueError('missing Steam asset')
        data = z.read(matches[0])
    if sha(data) != expected: raise ValueError('Steam asset differs')
    return data

def main():
    if len(sys.argv) != 6: raise ValueError('usage: SCRIPT BASE LINUX_ZIP WINDOWS_ZIP NEW_BASE NEW_MASKS')
    original, linux_path, windows_path, out, manifest = map(Path, sys.argv[1:])
    if out.exists() or manifest.exists() or out.is_symlink() or manifest.is_symlink() or not out.parent.is_dir() or not manifest.parent.is_dir(): raise ValueError('outputs must be new')
    base = bytearray(original.read_bytes())
    if sha(base) != BASE: raise ValueError('base differs')
    linux, windows = source(linux_path, LINUX), source(windows_path, WINDOWS)
    stream = bytearray(zlib.decompress(base[OFFSET:OFFSET + PACKED], -15))
    if len(stream) != 1048576: raise ValueError('split size differs')
    rows = []; changed_nonzero = 0
    for ident, offset, size, lo, wo in ROWS:
        before, left, right = bytes(stream[offset:offset + size]), linux[lo:lo + size], windows[wo:wo + size]
        if len(before) != size or len(left) != size or left != right: raise ValueError('source animation differs')
        indices = [i for i, (a, b) in enumerate(zip(before, left)) if a != b]
        for i in range(size):
            if before[i] == left[i]:
                if before[i] != 0: changed_nonzero += 1
                stream[offset + i] = 0
        mask = b''.join(struct.pack('<H', i) for i in indices)
        rows.append({'id': ident, 'target': offset, 'size': size, 'linux_offset': lo, 'windows_offset': wo,
                     'source_sha256': sha(left), 'preserved_positions': len(indices), 'mask_base64': base64.b64encode(mask).decode('ascii')})
    compressor = zlib.compressobj(6, zlib.DEFLATED, -15); encoded = compressor.compress(stream) + compressor.flush()
    if len(encoded) > PACKED: raise ValueError('compressed stream exceeds reservation')
    base[OFFSET:OFFSET + PACKED] = encoded + bytes(PACKED - len(encoded))
    report = {'template_sha256': sha(base), 'previous_sha256': BASE, 'objects': rows,
              'limitations': 'Differing bytes and other game data remain; private research only.'}
    out_created = manifest_created = False
    try:
        with out.open('xb') as f:
            out_created = True; f.write(base)
        with manifest.open('x', encoding='utf-8') as f:
            manifest_created = True; json.dump(report, f, indent=2); f.write('\n')
    except BaseException:
        if out_created: out.unlink(missing_ok=True)
        if manifest_created: manifest.unlink(missing_ok=True)
        raise
    print('matching byte positions:', sum(row['size'] - row['preserved_positions'] for row in rows),
          'nonzero bytes blanked:', changed_nonzero,
          'preserved differences:', sum(row['preserved_positions'] for row in rows), 'template SHA256:', sha(base))

if __name__ == '__main__': main()
