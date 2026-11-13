#!/usr/bin/env python3
"""Locally blank or restore two exact Steam clips in a private OBB template.

The resulting base still contains other game-derived content and is not a
distributable artifact. It deliberately contains invalid ZIP entry CRCs until
restored; no input is modified.
"""
import argparse
import hashlib
from pathlib import Path
import struct
import zipfile
import zlib

BASE = '3f6f96ef96a2b976d6862d37f7b5c4a06c15eb4ef5bcbd8efa49af17da8aeb76'
ENTRY = 'assets/bin/Data/sharedassets2.assets.split17'
SOURCES = {
    5824160: 'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da',
    5503192: 'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9',
}
# Target offset within the split, size, Linux/Windows source offsets and digest.
CLIPS = ((18655856 - 17 * 1048576, 2304, 2402600, 2081632, '7318f74aab3f42f6e12b1c232fc545f9c9328f929abeafcb095bf1b615ee4f45'),
         (18870536 - 17 * 1048576, 2500, 2396720, 2075752, 'be9f3631749314fc7cf2ec844f765a88894e7607b29f7bf1d991bc56db4eff4f'))


def sha(data): return hashlib.sha256(data).hexdigest()
def compress(data):
    compressor = zlib.compressobj(6, zlib.DEFLATED, -15)
    return compressor.compress(data) + compressor.flush()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode', choices=('scrub', 'restore'))
    p.add_argument('base', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--steam-zip', type=Path)
    args = p.parse_args()
    if args.output.exists() or args.output.is_symlink() or not args.output.parent.is_dir(): p.error('output must be new')
    if (args.mode == 'restore') != (args.steam_zip is not None): p.error('restore requires a Steam ZIP')
    base = bytearray(args.base.read_bytes())
    if args.mode == 'scrub' and sha(base) != BASE: raise ValueError('unexpected base')
    with zipfile.ZipFile(args.base) as archive:
        info = archive.getinfo(ENTRY)
        if info.compress_type != zipfile.ZIP_DEFLATED or info.flag_bits != 0 or info.file_size != 1048576:
            raise ValueError('unexpected entry layout')
        start = info.header_offset
        if base[start:start + 4] != b'PK\x03\x04': raise ValueError('invalid local header')
        name_len, extra_len = struct.unpack_from('<HH', base, start + 26)
        start += 30 + name_len + extra_len
        end = start + info.compress_size
        if end > len(base): raise ValueError('invalid compressed range')
        value = bytearray(zlib.decompress(base[start:end], -15))
        if len(value) != info.file_size: raise ValueError('invalid inflated size')
        if args.mode == 'scrub' and compress(value) != base[start:end]: raise ValueError('unexpected compression')
        if args.mode == 'restore':
            with zipfile.ZipFile(args.steam_zip) as steam:
                matches = [e for e in steam.infolist() if e.filename == 'Megabonk_Data/sharedassets2.assets']
                if len(matches) != 1 or matches[0].file_size not in SOURCES: raise ValueError('missing Steam source')
                raw = steam.read(matches[0])
            if sha(raw) != SOURCES[len(raw)]: raise ValueError('Steam source differs')
        for target, size, linux, windows, digest in CLIPS:
            before = value[target:target + size]
            if args.mode == 'scrub':
                if sha(before) != digest: raise ValueError('original clip differs')
                value[target:target + size] = bytes(size)
            else:
                if any(before): raise ValueError('clip was not blank')
                source = linux if len(raw) == 5824160 else windows
                clip = raw[source:source + size]
                if sha(clip) != digest: raise ValueError('Steam clip differs')
                value[target:target + size] = clip
        encoded = compress(value)
        if len(encoded) > info.compress_size: raise ValueError('compressed stream exceeds reserved space')
        base[start:end] = encoded + bytes(info.compress_size - len(encoded))
        if args.mode == 'restore' and sha(base) != BASE: raise ValueError('restored template differs')
    with args.output.open('xb') as out: out.write(base)
    print(args.mode, 'clips', len(CLIPS), 'asset bytes', sum(x[1] for x in CLIPS), 'template SHA256', sha(base))


if __name__ == '__main__': main()
