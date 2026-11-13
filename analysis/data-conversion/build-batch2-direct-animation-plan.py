#!/usr/bin/env python3
"""Make a private study plan using 27 pose-tested Steam animation clips.

No licensing or visual-equivalence conclusion follows from this experiment.
"""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

PLAN = '4c31e0fa6c7c9b95bfbbccac7cc2786e6ed5e5d4d6faa59ddaf4b0a4f6528cfb'
BASE = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
STEAM = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
MANIFEST = '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
ENTRIES = frozenset((
    '092fce90cc1a8a94587af717364ee92e',
    '1b66b1aa400e1d14b82588aa3351f9ae',
    '3e4f673b4a424ce42a29f114029b48b6',
    '421a0e06e58d95d4b942da60e7151467',
    '423504acca51724429a1637c8fca9674',
    '48dde080a5bdefa448cf24b26abd3fcc',
    '495ba194f92dd8649a37bf2f94f64288',
    '4a69e487ae9b55a4fbba40dae3842c4c',
    '509a55ef1be9494498d8c7a97c349eba',
    '59a9b9ce091e1034c999e58776417148',
    '6df67103f0be2454ca68ac855fa52fe5',
    '76ea6d58bdc5d0a43945cbcb04ae5c33',
    '82d77ca90025324439e1590f2b05fd7a',
    '8c43189040da5e9408e0495333bb75e3',
    '8c58e85098a54154b9ebf994cb4e01b2',
    '9d88648fb1363874f91c62f0c451f0cc',
    'a7e032c1ac4f20a4a8bf0c3342dba488',
    'b15374f5333584845aad25321e9dcecb',
    'b19486fb906282947a9812fdd7b0e2c6',
    'b54b8de3d8f02ef4d9cd36711bbfb1e3',
    'b69916918bd16684fa1f61dda6426e88',
    'bf8f1a0e6de1e6f419d0a2011814ef79',
    'cebca57dae55ae548aa713fc323194e1',
    'e8b271bff679f654983540ba618f2e6c',
    'e997d70d56e29924ba0cda425c7e7445',
    'f88fdb95c79d39c4b9a44d0d3d179c96',
    'fe9fcd40b867a324c9649e9f6a6029b1',
))

def sha(data): return hashlib.sha256(data).hexdigest()

def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'base', 'steam_asset', 'manifest', 'output'): p.add_argument(name, type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new in an existing directory')
    plan, base, steam, manifest = a.plan.read_bytes(), a.base.read_bytes(), a.steam_asset.read_bytes(), a.manifest.read_bytes()
    if (sha(plan), sha(base), sha(steam), sha(manifest)) != (PLAN, BASE, STEAM, MANIFEST): raise ValueError('pinned input mismatch')
    if plan[:12] != b'OBSTX003' + struct.pack('>I', 108): raise ValueError('invalid plan header')
    records = {r['file']:r for r in json.loads(manifest)['animations']}
    if len(records) != 108 or not ENTRIES <= records.keys(): raise ValueError('invalid animation manifest')
    pos = 268; result = bytearray(plan[:pos]); changed = set(); removed = 0
    with zipfile.ZipFile(a.base) as archive:
        for _ in range(108):
            start = pos; name = plan[pos:pos+32].decode('ascii'); pos += 128
            target, size, linux_file, linux_offset, windows_file, windows_offset = struct.unpack_from('>IIIQIQ', plan, pos)
            pos += 32; count, = struct.unpack_from('>I', plan, pos); pos += 4
            end = pos + count * 5
            if end > len(plan): raise ValueError('truncated plan')
            record = bytearray(plan[start:end])
            if name in ENTRIES:
                rec = records[name]
                if name in changed or rec['mode'] != 'legacy-exact' or count != rec['patch_count'] or linux_file != 0: raise ValueError('unexpected selected record')
                template = bytearray(archive.read('assets/bin/Data/' + name)); payload = steam[linux_offset:linux_offset+size]
                if sha(template) != plan[start+32:start+64].hex() or sha(payload) != plan[start+96:start+128].hex(): raise ValueError('entry mismatch')
                if target > len(template)-size or any(template[target:target+size]): raise ValueError('invalid empty target')
                patched = bytearray(payload); last = -1
                for off in range(pos, end, 5):
                    index, value = struct.unpack_from('<IB', plan, off)
                    if index <= last or index >= size or patched[index] == value: raise ValueError('invalid patch')
                    patched[index] = value; last = index
                template[target:target+size] = patched
                if sha(template) != plan[start+64:start+96].hex(): raise ValueError('previous reconstruction mismatch')
                template[target:target+size] = payload; record[64:96] = hashlib.sha256(template).digest()
                record = record[:160] + struct.pack('>I', 0)
                changed.add(name); removed += count
            result.extend(record); pos = end
    if pos != len(plan) or changed != ENTRIES: raise ValueError('unexpected plan layout')
    with a.output.open('xb') as out: out.write(result)
    print('Removed', removed, 'patch records across', len(changed), 'clips; private study plan SHA256', sha(result))

if __name__ == '__main__': main()
