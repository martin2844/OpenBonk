#!/usr/bin/env python3
"""Remove one private Bandit patch from the pinned local animation plan."""
import argparse
import hashlib
import struct
import zipfile
from pathlib import Path

PLAN = 'c6344331782f79bcc2c26dfb10ecfc942ef435c6e31bd237a5bca49c9b8aeaec'
BASE = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
STEAM = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
ENTRY = 'b77dbb5018582534a8ef8e8a1a61cf65'

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'base', 'steam_asset', 'output'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    if args.output.exists() or args.output.is_symlink(): parser.error('output exists')
    plan, base, steam = args.plan.read_bytes(), args.base.read_bytes(), args.steam_asset.read_bytes()
    if (sha(plan), sha(base), sha(steam)) != (PLAN, BASE, STEAM): raise ValueError('pinned input mismatch')
    if plan[:12] != b'OBSTX003' + struct.pack('>I', 108): raise ValueError('invalid plan header')
    pos = 268; result = bytearray(plan[:pos]); changed = 0
    with zipfile.ZipFile(args.base) as archive:
        for _ in range(108):
            start = pos; name = plan[pos:pos+32].decode('ascii'); pos += 128
            target, size, linux_file, linux_offset, windows_file, windows_offset = struct.unpack_from('>IIIQIQ', plan, pos)
            pos += 32; count, = struct.unpack_from('>I', plan, pos); pos += 4
            end = pos + count * 5
            if end > len(plan): raise ValueError('truncated plan')
            record = bytearray(plan[start:end])
            if name == ENTRY:
                if changed or count != 2121 or linux_file != 0 or size != 49412: raise ValueError('unexpected Bandit record')
                template = bytearray(archive.read('assets/bin/Data/' + name))
                payload = steam[linux_offset:linux_offset+size]
                if sha(template) != plan[start+32:start+64].hex() or sha(payload) != plan[start+96:start+128].hex(): raise ValueError('input entry mismatch')
                if target > len(template)-size or any(template[target:target+size]): raise ValueError('invalid empty target')
                template[target:target+size] = payload
                record[64:96] = hashlib.sha256(template).digest()
                record = record[:160] + struct.pack('>I', 0)
                changed += 1
            result.extend(record); pos = end
    if pos != len(plan) or changed != 1: raise ValueError('unexpected plan layout')
    with args.output.open('xb') as out: out.write(result)
    print('Bandit patch removed: 2121 changed-byte records; new plan SHA256', sha(result))

if __name__ == '__main__': main()
