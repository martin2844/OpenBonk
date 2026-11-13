#!/usr/bin/env python3
"""Private host-only three-way particle byte scrub and reconstruction study.

The plan contains masks and offsets, not particle values. This is not yet the
Android importer and does not establish provenance or distribution rights.
"""
import argparse
import base64
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import struct
import zipfile
import zlib

module_path = Path(__file__).with_name('audit-particle-template-matches.py')
spec = importlib.util.spec_from_file_location('particle_audit', module_path)
audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)
EXPECTED_BASE = 'a6a91aeefb8095e2d096967220541eb1247a0fb27e08f1f799ee2cd71c410892'
SOURCES = {'linux': 'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da',
           'windows': 'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'}
SHARED1_BASE = '2b4d71ddf84456b17ae1624847def47acfc3d896c2cd2f67ff096016e9c99650'
SHARED1_SOURCES = {'linux': 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
                   'windows': '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'}


def digest(value): return hashlib.sha256(value).hexdigest()


def assets(linux_path, windows_zip, asset, hashes):
    linux = linux_path.read_bytes()
    with zipfile.ZipFile(windows_zip) as z:
        matches = [i for i in z.infolist() if i.filename == 'Megabonk_Data/' + asset]
        if len(matches) != 1: raise ValueError('missing Windows asset')
        windows = z.read(matches[0])
    if digest(linux) != hashes['linux'] or digest(windows) != hashes['windows']:
        raise ValueError('unsupported Steam sources')
    return linux, windows


def streams(base_path, raw, asset):
    result = []
    with zipfile.ZipFile(base_path) as z:
        for info in z.infolist():
            m = re.fullmatch(r'assets/bin/Data/' + re.escape(asset) + r'\.split(\d+)', info.filename)
            if not m: continue
            if info.compress_type != zipfile.ZIP_DEFLATED or info.flag_bits != 0: raise ValueError('invalid split')
            start = info.header_offset
            if raw[start:start+4] != b'PK\x03\x04': raise ValueError('invalid ZIP header')
            n, e = struct.unpack_from('<HH', raw, start+26); start += 30+n+e
            data = zlib.decompress(raw[start:start+info.compress_size], -15)
            if len(data) != info.file_size: raise ValueError('invalid split length')
            result.append((int(m.group(1)), start, info.compress_size, data))
    result.sort()
    if not result or [x[0] for x in result] != list(range(len(result))): raise ValueError('invalid splits')
    return result


def repack(raw, pieces, content):
    offset = 0
    for _, start, reserved, original in pieces:
        part = content[offset:offset+len(original)]; offset += len(original)
        if part == original: continue
        compressor = zlib.compressobj(6, zlib.DEFLATED, -15)
        encoded = compressor.compress(part) + compressor.flush()
        if len(encoded) > reserved: raise ValueError('split compressed stream exceeds reservation')
        raw[start:start+reserved] = encoded + bytes(reserved-len(encoded))
    if offset != len(content): raise ValueError('content length mismatch')
    return raw


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode', choices=('scrub', 'restore'))
    p.add_argument('base', type=Path); p.add_argument('linux_asset', type=Path); p.add_argument('windows_zip', type=Path)
    p.add_argument('output', type=Path); p.add_argument('plan', type=Path)
    p.add_argument('--platform', choices=('linux', 'windows'))
    p.add_argument('--best-candidates', action='store_true', help='consider repeated particle names in a scrub study')
    p.add_argument('--asset', choices=('sharedassets1.assets','sharedassets2.assets'), default='sharedassets2.assets')
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new')
    if a.mode == 'scrub' and (a.plan.exists() or a.plan.is_symlink() or a.platform): p.error('invalid scrub arguments')
    if a.mode == 'restore' and not a.platform: p.error('restore requires --platform')
    if a.mode == 'restore' and a.best_candidates: p.error('--best-candidates is for scrub studies')
    hashes = SHARED1_SOURCES if a.asset == 'sharedassets1.assets' else SOURCES
    original = SHARED1_BASE if a.asset == 'sharedassets1.assets' else EXPECTED_BASE
    raw = bytearray(a.base.read_bytes()); linux, windows = assets(a.linux_asset, a.windows_zip, a.asset, hashes)
    pieces = streams(a.base, raw, a.asset); content = bytearray(b''.join(x[3] for x in pieces))
    if a.mode == 'scrub':
        if digest(raw) != original: raise ValueError('unsupported base')
        target, ls, ws = audit.particles(bytes(content)), audit.particles(linux), audit.particles(windows)
        rows = []; total = 0
        for key, candidates in sorted(target.items()):
            left, right = ls[key], ws[key]
            if not left or not right or (not a.best_candidates and (len(left) != 1 or len(right) != 1)): continue
            for ident, pos, before in candidates:
                def select(options):
                    return max(options, key=lambda x: (sum(a != 0 and a == b for a,b in zip(before,x[2])), -x[0]))
                lc, rc = select(left), select(right)
                l, r = lc[2], rc[2]; size = len(before); mask = bytearray((size + 7)//8)
                for i, (x, y, z) in enumerate(zip(before, l, r)):
                    if x and x == y == z:
                        mask[i//8] |= 1 << (i%8); content[pos+i] = 0; total += 1
                rows.append({'target_id': ident, 'target_offset': pos, 'size': size,
                             'linux_offset': lc[1], 'windows_offset': rc[1],
                             'linux_sha256': digest(l), 'windows_sha256': digest(r),
                             'mask': base64.b64encode(zlib.compress(mask, 9)).decode('ascii')})
        result = repack(raw, pieces, content)
        plan = {'original_sha256': original, 'scrubbed_sha256': digest(result),
                'sources': hashes, 'changed_nonzero': total, 'objects': rows}
        with a.plan.open('x') as f: json.dump(plan, f, separators=(',', ':')); f.write('\n')
    else:
        plan = json.loads(a.plan.read_text())
        if plan['original_sha256'] != original or digest(raw) != plan['scrubbed_sha256'] or plan['sources'] != hashes:
            raise ValueError('plan or scrubbed base differs')
        source = linux if a.platform == 'linux' else windows
        for row in plan['objects']:
            pos, size = row['target_offset'], row['size']; off = row[a.platform+'_offset']
            value = source[off:off+size]
            if len(value) != size or digest(value) != row[a.platform+'_sha256']: raise ValueError('particle source differs')
            mask = zlib.decompress(base64.b64decode(row['mask'], validate=True))
            if len(mask) != (size+7)//8 or pos < 0 or pos+size > len(content): raise ValueError('invalid particle mask')
            for i in range(size):
                if mask[i//8] & (1 << (i%8)):
                    if content[pos+i] != 0: raise ValueError('particle byte not blank')
                    content[pos+i] = value[i]
        result = repack(raw, pieces, content)
        if digest(result) != original: raise ValueError('restored base differs')
    with a.output.open('xb') as f: f.write(result)
    print(a.mode, len(plan['objects']), 'particles;', plan['changed_nonzero'], 'nonzero bytes; SHA256', digest(result))


if __name__ == '__main__': main()
