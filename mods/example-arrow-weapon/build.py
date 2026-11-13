#!/usr/bin/env python3
"""Build a self-contained example mod using only the Python standard library."""
import hashlib
import json
from pathlib import Path
import runpy
import struct
import zlib
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

root = Path(__file__).resolve().parent
chunk = runpy.run_path(str(root.parent / 'example-healing-item' / 'build.py'))['chunk']
pixels = bytearray()
for y in range(64):
    pixels.append(0)
    for x in range(64):
        border = x < 3 or y < 3 or x >= 61 or y >= 61
        shaft = 13 <= x <= 50 and abs((x + y) - 64) <= 2
        head = 39 <= x <= 51 and 12 <= y <= 24 and x - y >= 25
        feather = 12 <= x <= 24 and 40 <= y <= 52 and abs(x + y - 64) <= 5
        color = ((31, 28, 54, 255) if border else (247, 221, 143, 255) if shaft or head
                 else (114, 220, 244, 255) if feather else (67, 54, 93, 255))
        pixels.extend(color)
icon = (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 64, 64, 8, 6, 0, 0, 0))
        + chunk(b'IDAT', zlib.compress(pixels, 9)) + chunk(b'IEND', b''))
payloads = {'main.lua': (root / 'main.lua').read_bytes(), 'icon.png': icon}
manifest = {'api': 3, 'kind': 'content', 'id': 'openbonk.example.arrow',
            'weapon': {'id': 'practice-arrow', 'name': 'Practice Arrow',
                       'description': 'Every third attack deals 50% more damage.',
                       'effect': 'arrow', 'icon': 'icon.png', 'damage': 10,
                       'cooldown': 1, 'range': 30, 'chainRange': 0, 'chainProc': 0, 'bounces': 0},
            'files': [{'path': name, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()}
                      for name, data in payloads.items()]}
output = root / 'example-arrow-weapon.zip'
with ZipFile(output, 'x', compression=ZIP_DEFLATED) as archive:
    for name, data in [('manifest.json', (json.dumps(manifest, indent=2) + '\n').encode()), *payloads.items()]:
        entry = ZipInfo(name, (2020, 1, 1, 0, 0, 0))
        entry.compress_type = ZIP_DEFLATED
        archive.writestr(entry, data)
print(output)
