#!/usr/bin/env python3
"""Make a private study plan that uses the Steam KnightFalling clip directly.

This removes one Android-derived patch from a pinned research plan. The rest
of the plan and its surrounding APK remain private and are not distributable.
"""
import argparse
import hashlib
import struct
import zipfile
from pathlib import Path

PLAN = '4c7f8b9ac3561896589ceae08e074a6e6a0f0216e9347fcfc8e05ef52d62da0a'
BASE = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
STEAM = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
ENTRY = '105210dd7a5ea76449228b7809c10571'

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'base', 'steam_asset', 'output'): p.add_argument(name, type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new in an existing directory')
    plan, base, steam = a.plan.read_bytes(), a.base.read_bytes(), a.steam_asset.read_bytes()
    if (sha(plan), sha(base), sha(steam)) != (PLAN, BASE, STEAM): raise ValueError('pinned input mismatch')
    if plan[:12] != b'OBSTX003' + struct.pack('>I', 108): raise ValueError('invalid plan header')
    pos = 268; result = bytearray(plan[:pos]); changed = 0
    with zipfile.ZipFile(a.base) as archive:
        for _ in range(108):
            start = pos; name = plan[pos:pos+32].decode('ascii'); pos += 128
            target, size, linux_file, linux_offset, windows_file, windows_offset = struct.unpack_from('>IIIQIQ', plan, pos)
            pos += 32; count, = struct.unpack_from('>I', plan, pos); pos += 4
            end = pos + count * 5
            if end > len(plan): raise ValueError('truncated plan')
            record = bytearray(plan[start:end])
            if name == ENTRY:
                if changed or count != 466 or linux_file != 0: raise ValueError('unexpected Knight record')
                template = bytearray(archive.read('assets/bin/Data/' + name))
                payload = steam[linux_offset:linux_offset+size]
                if sha(template) != plan[start+32:start+64].hex() or sha(payload) != plan[start+96:start+128].hex(): raise ValueError('entry mismatch')
                if target > len(template)-size or any(template[target:target+size]): raise ValueError('invalid empty target')
                patched = bytearray(payload); last = -1
                for off in range(pos, end, 5):
                    index, value = struct.unpack_from('<IB', plan, off)
                    if index <= last or index >= size or patched[index] == value: raise ValueError('invalid patch')
                    patched[index] = value; last = index
                template[target:target+size] = patched
                if sha(template) != plan[start+64:start+96].hex(): raise ValueError('previous reconstruction mismatch')
                template[target:target+size] = payload
                record[64:96] = hashlib.sha256(template).digest()
                record = record[:160] + struct.pack('>I', 0)
                changed += 1
            result.extend(record); pos = end
    if pos != len(plan) or changed != 1: raise ValueError('unexpected plan layout')
    with a.output.open('xb') as out: out.write(result)
    print('Removed 466 Knight patch records; private study plan SHA256', sha(result))

if __name__ == '__main__': main()
