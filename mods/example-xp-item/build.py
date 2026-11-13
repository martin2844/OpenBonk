#!/usr/bin/env python3
"""Build a local API 3 example mod using only Python's standard library."""
import hashlib
import json
import struct
import zlib
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

ROOT = Path(__file__).resolve().parent

def chunk(kind, data):
    return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)

def icon():
    # An original geometric icon; no game assets are used.
    pixels = bytearray()
    for y in range(64):
        pixels.append(0)
        for x in range(64):
            a, b = (x - 32) / 24, (29 - y) / 24
            heart = (a*a + b*b - 1)**3 - a*a*b*b*b < 0
            border = x < 3 or y < 3 or x >= 61 or y >= 61
            color = (38, 28, 56, 255) if border else (247, 204, 71, 255) if heart else (77, 52, 96, 255)
            pixels.extend(color)
    return b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 64, 64, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(pixels, 9)) + chunk(b'IEND', b'')

def main():
    payloads = {'main.lua': (ROOT / 'main.lua').read_bytes(), 'icon.png': icon()}
    manifest = {
        'api': 3, 'kind': 'content', 'id': 'openbonk.example.xp',
        'item': {
            'id': 'xp-spark', 'name': 'XP Spark',
            'description': 'Grants 3 XP per second per stack while alive.',
            'icon': 'icon.png', 'rarity': 'Rare', 'inPool': True,
            'bonuses': [], 'hooks': ['tick'],
        },
        'files': [{'path': n, 'size': len(data), 'sha256': hashlib.sha256(data).hexdigest()} for n, data in payloads.items()],
    }
    destination = ROOT / 'example-xp-item.zip'
    with ZipFile(destination, 'x', compression=ZIP_DEFLATED) as archive:
        for name, data in [('manifest.json', (json.dumps(manifest, indent=2) + '\n').encode()), *payloads.items()]:
            entry = ZipInfo(name, (2020, 1, 1, 0, 0, 0)); entry.compress_type = ZIP_DEFLATED
            archive.writestr(entry, data)
    print(destination)

if __name__ == '__main__': main()
