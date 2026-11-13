#!/usr/bin/env python3
"""Make a private study plan using 12 pose-tested Steam animation clips.

No licensing or visual-equivalence conclusion follows from this experiment.
"""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

PLAN = '5234be766f35adcef52b6b8114d0273f0be1f5a60d3e89d78e9e90a14e12ba89'
BASE = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
STEAM = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
MANIFEST = '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
ENTRIES = frozenset(('2a23f0ba0bef86640bd27a258461b2db', '3bd4e7b04ee27cb428568b583ba3405e',
    '588f405b6967afd4cafaef06092c5f46', '5e2684d41f3d33e4292eb0216efe5991',
    '5e73ce7d8f31d4c4ca0a3ebc69422d2b', '7875e1d678005a449a1f3ed2c899b60d',
    '8881832692b485a4eb8f604f70a5aa10', '88ff40bccda905d45bfe7c25d0fc1454',
    '8b0490bab8a3b2f4fa428fb0a594658c', '8b2d0c1648043194295bca80cc4a3e2c',
    'de8d2fe18363ec646b43a292397f5dc6', 'f6a87e19eaddaf64198f909f9fac9bac'))

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
