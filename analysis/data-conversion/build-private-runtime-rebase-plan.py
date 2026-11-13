#!/usr/bin/env python3
"""Make a pinned private delta for the runtime research OBB; not distributable."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import zipfile

SOURCE = '632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29'
TARGET = '3b5ddc2fa06a97bf04c6ccdf442a0454b49ec02fa86b32917b74d827ee4f79e7'

def sha(data): return hashlib.sha256(data).hexdigest()
def file_sha(path):
    with path.open('rb') as file: return hashlib.file_digest(file, 'sha256').hexdigest()

def write_fixed(archive, name, data):
    entry = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
    entry.compress_type = zipfile.ZIP_DEFLATED
    archive.writestr(entry, data, compress_type=zipfile.ZIP_DEFLATED, compresslevel=9)

def encode(old, new):
    if len(old) != len(new): return b'\x00' + new
    spans = []
    pos, size = 0, len(old)
    while pos < size:
        if old[pos] == new[pos]: pos += 1; continue
        start = last = pos
        pos += 1
        while pos < size:
            if old[pos] != new[pos]: last = pos
            if pos - last > 16: break
            pos += 1
        spans.append((start, new[start:last + 1]))
    patch = bytearray(b'\x01' + struct.pack('>I', len(spans)))
    for start, data in spans:
        patch.extend(struct.pack('>II', start, len(data)))
        patch.extend(data)
    return bytes(patch) if len(patch) < len(new) + 1 else b'\x00' + new

def apply(old, patch, target_size):
    if not patch: raise ValueError('empty patch')
    if patch[0] == 0:
        result = patch[1:]
        if len(result) != target_size: raise ValueError('replacement size differs')
        return result
    if patch[0] != 1 or len(old) != target_size or len(patch) < 5: raise ValueError('invalid patch')
    count = struct.unpack_from('>I', patch, 1)[0]
    if count > len(old): raise ValueError('excess patch count')
    pos, last, result = 5, 0, bytearray(old)
    for _ in range(count):
        if pos + 8 > len(patch): raise ValueError('truncated patch')
        offset, size = struct.unpack_from('>II', patch, pos); pos += 8
        if not size or offset < last or offset + size > len(result) or pos + size > len(patch): raise ValueError('invalid patch range')
        result[offset:offset + size] = patch[pos:pos + size]
        pos += size; last = offset + size
    if pos != len(patch): raise ValueError('extra patch bytes')
    return bytes(result)

def main():
    if len(sys.argv) != 4: raise ValueError('usage: SCRIPT SOURCE_OBB TARGET_OBB NEW_PLAN_ZIP')
    source, target, output = map(Path, sys.argv[1:])
    if not source.is_file() or file_sha(source) != SOURCE or not target.is_file() or file_sha(target) != TARGET:
        raise ValueError('unexpected input OBB')
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    try:
        with zipfile.ZipFile(source) as old, zipfile.ZipFile(target) as new:
            if len(old.namelist()) != len(set(old.namelist())) or old.namelist() != new.namelist(): raise ValueError('archive inventory differs')
            records = []
            with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as plan:
                for name in old.namelist():
                    a, b = old.getinfo(name), new.getinfo(name)
                    if a.is_dir() or b.is_dir() or a.file_size > 64 * 1024 * 1024 or b.file_size > 64 * 1024 * 1024:
                        raise ValueError('unsupported archive entry')
                    before, after = old.read(name), new.read(name)
                    if before == after: continue
                    patch = encode(before, after)
                    if apply(before, patch, len(after)) != after: raise ValueError('patch failed round-trip')
                    patch_name = 'records/%04d.bin' % len(records)
                    write_fixed(plan, patch_name, patch)
                    records.append({'entry': name, 'source_size': len(before), 'source_sha256': sha(before),
                                    'target_size': len(after), 'target_sha256': sha(after), 'patch': patch_name,
                                    'patch_size': len(patch), 'patch_sha256': sha(patch), 'mode': patch[0]})
                if len(records) != 222: raise ValueError('unexpected changed entry count')
                write_fixed(plan, 'manifest.json', json.dumps({'version': 1, 'source_obb_sha256': SOURCE,
                    'target_reference_obb_sha256': TARGET, 'entries': records,
                    'notice': 'Private research data, including replacement scene bytes; not distributable.'}, separators=(',', ':')).encode('utf-8'))
        with zipfile.ZipFile(output) as plan:
            if plan.testzip() is not None or len(plan.namelist()) != len(records) + 1: raise ValueError('invalid plan archive')
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    print('entries', len(records), 'full', sum(r['mode'] == 0 for r in records),
          'patch', sum(r['mode'] == 1 for r in records), 'raw patch bytes', sum(r['patch_size'] for r in records),
          'ZIP bytes', output.stat().st_size)
    print('Private research plan; contains game-derived data and must not be distributed.')

if __name__ == '__main__': main()
