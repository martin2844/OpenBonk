#!/usr/bin/env python3
"""Derive exact Steam font ranges for the private Android preparation study."""
import hashlib
import json
import struct
import zipfile
from pathlib import Path

import UnityPy

ROOT = Path(__file__).resolve().parents[2]
B = ROOT / 'build/android'
APK = B / 'localization-shared-scrubbed-base/base.apk'
TARGET = B / 'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
SOURCES = [B / 'linux-steam-regression.zip', B / 'windows-steam-full-input.zip']
MANIFEST = B / 'local-research-manifests/shared0-font-template/font-manifest.json'
OUTPUT = B / 'shared0-font-android-plan/plan.bin'


def sha(data):
    return hashlib.sha256(data).digest()


def source_offset(obj, record):
    raw = obj.get_raw_data()
    needle = struct.pack('<I', record['size'])
    start = 0
    matches = []
    while (at := raw.find(needle, start)) >= 0:
        begin = at + 4
        if sha(raw[begin:begin + record['size']]).hex() == record['sha256']:
            matches.append(obj.byte_start + begin)
        start = at + 1
    if len(matches) != 1:
        raise ValueError('ambiguous font data')
    return matches[0]


def main():
    if OUTPUT.exists():
        raise ValueError('output exists')
    with zipfile.ZipFile(APK) as apk, zipfile.ZipFile(TARGET) as target:
        names = sorted((n for n in target.namelist() if n.startswith('assets/bin/Data/sharedassets0.assets.split')), key=lambda n: int(n.rsplit('split', 1)[1]))
        if len(names) != 126:
            raise ValueError('unexpected number of splits')
        base = b''.join(apk.read(n) for n in names)
        desired = b''.join(target.read(n) for n in names)
    if len(base) != 132028896 or len(desired) != len(base):
        raise ValueError('unexpected asset length')
    target_objects = {o.path_id: o for o in UnityPy.load(desired).objects}
    source_objects, data = [], []
    for path in SOURCES:
        with zipfile.ZipFile(path) as z:
            data.append(z.read('Megabonk_Data/sharedassets0.assets'))
        source_objects.append({o.path_id: o for o in UnityPy.load(data[-1]).objects})
    records = json.loads(MANIFEST.read_text())['fonts']
    if len(records) != 7:
        raise ValueError('unexpected font inventory')
    result = bytearray(base)
    ranges = []
    for r in records:
        dst = source_offset(target_objects[r['target_id']], r)
        offsets = [source_offset(s[r['source_id']], r) for s in source_objects]
        payload = desired[dst:dst + r['size']]
        if any(base[dst:dst + r['size']]) or any(d[o:o + r['size']] != payload for d, o in zip(data, offsets)):
            raise ValueError('font source or base differs')
        result[dst:dst + r['size']] = payload
        ranges.append((dst, r['size'], offsets[0], offsets[1], sha(payload)))
    ranges.sort()
    output = bytearray(b'OBS0F001')
    output += struct.pack('>II', len(base), len(ranges)) + sha(base) + sha(result)
    for d in data:
        output += struct.pack('>Q', len(d)) + sha(d)
    for dst, size, linux, windows, digest in ranges:
        output += struct.pack('>QIQQ', dst, size, linux, windows) + digest
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_bytes(output)
    print('plan', sha(output).hex(), 'base', sha(base).hex(), 'result', sha(result).hex(), 'font bytes', sum(x[1] for x in ranges))


if __name__ == '__main__':
    main()
