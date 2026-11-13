#!/usr/bin/env python3
"""Scrub or restore locally compiled shader objects in the private base OBB.

The scrubbed template intentionally has invalid entry CRCs until restored. It
still contains other game-derived data and is not a distributable artifact.
"""
import argparse
import hashlib
from pathlib import Path
import struct
import zipfile
import zlib

ORIGINAL = 'a6a91aeefb8095e2d096967220541eb1247a0fb27e08f1f799ee2cd71c410892'
SCRUBBED = '4ec0b2e564ce611fa055972d2ccfe99cafd3cfe40887b00ba23160298a5d9bd5'
# Entry, object offset, object length, generated SHA, final SHA, reference range.
SHADERS = (
    ('1ccecd9b89b8a4f14bfb64f29ddfcc81', 240, 132256,
     '76fd8d9a46b822c481eb4c7c9ba5d8cc43a05e241f4851cdff56e65ff2fef2a9',
     'c2bea035a64f9a8881e88a71a04c3f2c42f8ff93f7494c58ca376acd1c5385a2', 132224, 24),
    ('39354b8a5f5520b4e85f2c9fe7791c60', 192, 74812,
     '87ba3bfa670fe3aa3c35df87b61e9af2bc6b8e0af23be90a01484fcb25a9625a',
     '87ba3bfa670fe3aa3c35df87b61e9af2bc6b8e0af23be90a01484fcb25a9625a', 74780, 24),
)


def sha(value):
    return hashlib.sha256(value).hexdigest()


def compress(value):
    obj = zlib.compressobj(6, zlib.DEFLATED, -15)
    return obj.compress(value) + obj.flush()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode', choices=('scrub', 'restore'))
    p.add_argument('base', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--pack', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    if (a.mode == 'restore') != (a.pack is not None):
        p.error('restore requires a shader pack')
    data = bytearray(a.base.read_bytes())
    if sha(data) != (ORIGINAL if a.mode == 'scrub' else SCRUBBED):
        raise ValueError('unexpected base')
    compiled = {}
    if a.pack:
        with zipfile.ZipFile(a.pack) as pack:
            if set(pack.namelist()) != {r[0] for r in SHADERS} or len(pack.namelist()) != len(SHADERS):
                raise ValueError('unexpected shader pack entries')
            for entry, _, size, generated, _, _, _ in SHADERS:
                if pack.getinfo(entry).file_size != size:
                    raise ValueError('generated shader size differs')
                compiled[entry] = pack.read(entry)
                if sha(compiled[entry]) != generated:
                    raise ValueError('generated shader differs')
    with zipfile.ZipFile(a.base) as archive:
        if len(archive.namelist()) != len(set(archive.namelist())):
            raise ValueError('duplicate base entries')
        for entry, offset, size, _, final, ref_offset, ref_size in SHADERS:
            info = archive.getinfo('assets/bin/Data/' + entry)
            if info.compress_type != zipfile.ZIP_DEFLATED or info.flag_bits != 0:
                raise ValueError('unexpected entry layout')
            start = info.header_offset
            if data[start:start + 4] != b'PK\x03\x04':
                raise ValueError('bad ZIP header')
            name_len, extra_len = struct.unpack_from('<HH', data, start + 26)
            start += 30 + name_len + extra_len
            end = start + info.compress_size
            if end > len(data): raise ValueError('invalid compressed range')
            inflated = bytearray(zlib.decompress(data[start:end], -15))
            if len(inflated) != info.file_size or offset + size > len(inflated):
                raise ValueError('invalid shader extent')
            original = bytes(inflated[offset:offset + size])
            refs = original[ref_offset:ref_offset + ref_size]
            if a.mode == 'scrub':
                if sha(original) != final or compress(inflated) != data[start:end]:
                    raise ValueError('source shader or compression differs')
                replacement = bytearray(size)
            else:
                if any(original[:ref_offset]) or any(original[ref_offset + ref_size:]):
                    raise ValueError('shader was not scrubbed')
                replacement = bytearray(compiled[entry])
            replacement[ref_offset:ref_offset + ref_size] = refs
            if a.mode == 'restore' and sha(replacement) != final:
                raise ValueError('restored shader differs')
            inflated[offset:offset + size] = replacement
            encoded = compress(inflated)
            if len(encoded) > info.compress_size: raise ValueError('compressed stream exceeds reservation')
            data[start:end] = encoded + bytes(info.compress_size - len(encoded))
    if a.mode == 'restore' and sha(data) != ORIGINAL:
        raise ValueError('restored base differs')
    with a.output.open('xb') as out: out.write(data)
    print(a.mode, 'two shader objects; base SHA256', sha(data))


if __name__ == '__main__':
    main()
