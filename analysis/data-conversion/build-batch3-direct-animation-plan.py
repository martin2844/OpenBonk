#!/usr/bin/env python3
"""Make a private study plan using 29 pose-tested Steam animation clips.

No licensing or visual-equivalence conclusion follows from this experiment.
"""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

PLAN = 'c744c59fa5d60e6f73285e8277c7615db8fd38201298d6466ca7d4bde47a04a7'
BASE = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
STEAM = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
MANIFEST = '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
ENTRIES = frozenset((
    '1d3bf757c5a84564d9da68f423be1bfb',
    '216617a5f587c91489f849626b97dabc',
    '3ada34c37f4784f448ebdc93a4aa3b43',
    '3d0854e33e20ba24cb74a7b7cf21ef4e',
    '44d435e4acfe5a54ab1f82e9a312a45e',
    '546a0e8add941314c97e7dcce1dc17d0',
    '551d6a2f21bfa87448935a2848471d55',
    '5685cc39f73712d4a8e3f6865bf97341',
    '59361321944b3f946a437d165e64cea4',
    '5e86657e262f299468ba533a4e13a6b2',
    '625b72e4d62c60e4c88db481082305bd',
    '67aba0cbe11e8824ba5e0adb05ebe13e',
    '6b80753f2616b974f826e8da76624769',
    '78eebffc0665e574e833d7fb3043c74b',
    '89272ce2712cce540a5c0b8467c9e04f',
    '8a5c1a6f38b929c4bbbf7699a69e9365',
    '9e091b9700cf9d148b4401bcaca6bcc7',
    'a1b787f1821163744adfd997807d3feb',
    'a5dcfe93f716b974d85895464804d516',
    'b947cd5e81ce4b64aa9f02b84e3be122',
    'bee7e9aed9cbb29449fd0cab4584eb5c',
    'c048f095a81987f479fc2316bcc08fb4',
    'c06cf2a159c96f140aa38b635da50640',
    'c3d77f7843b91464aa61a5e40f338a08',
    'cdd80f5d8c86e2b469aed8086fe922eb',
    'd2712c34ebef02746b3bda5b287025e8',
    'd8c5034f441c1b24fa8b7e578655f29d',
    'df789e14353836641a8c9cac3f19f41c',
    'f708298fd60c85a48a11c222bbdb7180',
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
