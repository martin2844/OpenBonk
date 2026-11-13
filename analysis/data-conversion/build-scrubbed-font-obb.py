#!/usr/bin/env python3
"""Make a private, deliberately non-ZIP-valid template for local research."""
import hashlib, struct, zipfile, zlib
from pathlib import Path

root = Path(__file__).resolve().parents[2] / 'build/android'
source = root / 'localization-shared-scrubbed-base/base.obb'
output = root / 'obb-no-liberation-base/base.obb'
name = 'assets/bin/Data/a4611dd71c390bc48af432c727d2e32c'
expected = 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
raw = bytearray(source.read_bytes())
if hashlib.sha256(raw).hexdigest() != expected or output.exists():
    raise ValueError('input differs or output exists')
with zipfile.ZipFile(source) as z:
    info = z.getinfo(name)
    entry = z.read(info)
    head = raw[info.header_offset:info.header_offset + 30]
    n, extra = struct.unpack_from('<HH', head, 26)
    offset = info.header_offset + 30 + n + extra
    if (offset, info.compress_size, len(entry)) != (848444, 8502, 29180):
        raise ValueError('unexpected archive layout')
    if hashlib.sha256(entry).hexdigest() != '20c812b6e2a3d3248ca0552901d6588b5ab785083d376ccba16254d0c7e08a29':
        raise ValueError('entry differs')
    codec = zlib.compressobj(6, zlib.DEFLATED, -15)
    if codec.compress(entry) + codec.flush() != raw[offset:offset + info.compress_size]:
        raise ValueError('cannot reproduce compressed entry')
    raw[offset:offset + info.compress_size] = entry[:384] + bytes(info.compress_size - 384)
output.parent.mkdir(parents=True, exist_ok=True)
with output.open('xb') as f:
    f.write(raw)
print('private scrubbed template:', len(raw), 'bytes; sha256', hashlib.sha256(raw).hexdigest())
