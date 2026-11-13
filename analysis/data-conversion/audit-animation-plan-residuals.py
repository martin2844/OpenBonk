#!/usr/bin/env python3
"""Report remaining byte patches in an animation preparation plan, without exporting their values."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('plan', type=Path)
    parser.add_argument('manifest', type=Path)
    parser.add_argument('--linux-zip', type=Path)
    parser.add_argument('--windows-zip', type=Path)
    args = parser.parse_args()
    if bool(args.linux_zip) != bool(args.windows_zip):
        parser.error('provide both Steam ZIPs for comparison')
    def source(path):
        with zipfile.ZipFile(path) as z:
            entries = [entry for entry in z.infolist()
                       if entry.filename == 'Megabonk_Data/sharedassets1.assets']
            if len(entries) != 1 or entries[0].file_size > 64 * 1024 * 1024:
                raise ValueError('expected one bounded Steam asset file')
            return z.read(entries[0])
    linux = source(args.linux_zip) if args.linux_zip else None
    windows = source(args.windows_zip) if args.windows_zip else None
    if linux is not None and (hashlib.sha256(linux).hexdigest() != 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
                              or hashlib.sha256(windows).hexdigest() != '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'):
        raise ValueError('unsupported Steam asset inputs')
    data = args.plan.read_bytes()
    if len(data) < 268 or data[:8] != b'OBSTX003':
        raise ValueError('invalid animation plan header')
    count = struct.unpack_from('>I', data, 8)[0]
    manifest = json.loads(args.manifest.read_text(encoding='utf-8'))
    entries = manifest['animations']
    records = {row['file']: row for row in entries}
    if count != 108 or len(entries) != count or len(records) != count:
        raise ValueError('unexpected animation inventory')
    pos = 268
    seen = set()
    residuals = []
    for _ in range(count):
        if pos + 164 > len(data):
            raise ValueError('truncated animation record')
        name = data[pos:pos + 32].decode('ascii')
        if name not in records or name in seen:
            raise ValueError('unknown or duplicate animation')
        seen.add(name)
        _, size, linux_file, linux_offset, windows_file, windows_offset = struct.unpack_from('>IIIQIQ', data, pos + 128)
        expected = data[pos + 96:pos + 128]
        patches = struct.unpack_from('>I', data, pos + 160)[0]
        pos += 164
        if patches > size or pos + patches * 5 > len(data):
            raise ValueError('invalid patch count')
        last = -1
        for offset in range(pos, pos + patches * 5, 5):
            index = struct.unpack_from('<I', data, offset)[0]
            if index <= last or index >= size:
                raise ValueError('invalid patch offset')
            last = index
        pos += patches * 5
        if patches:
            row = {'file': name, 'name': records[name]['name'],
                   'patch_bytes': patches, 'clip_payload_bytes': size}
            if linux is not None:
                if linux_file != 0 or windows_file != 0:
                    raise ValueError('unsupported Steam source file index')
                left, right = linux[linux_offset:linux_offset + size], windows[windows_offset:windows_offset + size]
                if len(left) != size or len(right) != size or hashlib.sha256(left).digest() != expected:
                    raise ValueError('Steam animation payload differs from plan')
                row['linux_windows_differing_bytes'] = sum(x != y for x, y in zip(left, right))
            residuals.append(row)
    if pos != len(data):
        raise ValueError('trailing animation plan data')
    report = {'plan_sha256': hashlib.sha256(data).hexdigest(),
              'animation_count': count, 'clips_with_patches': len(residuals),
              'patch_bytes': sum(row['patch_bytes'] for row in residuals),
              'clips': residuals,
              'limitations': ['Reports only this animation plan, not other assets or rights.',
                              'A patch-free record does not establish visual equivalence.']}
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
