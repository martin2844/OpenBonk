#!/usr/bin/env python3
"""Validate position-only scrub plans without requiring game data.

These version-specific files contain offsets, hashes and bit masks. They do
not contain the masked game asset bytes and do not build an APK by themselves.
"""
import hashlib
import io
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parent.parent / 'analysis/data-conversion'
PLANS = (
    ('scene-component-position-plan.bin', b'OBSCN001', 'a35bf01205a78f486bb3f062e83f8649ca975aa0ef43acceda1047554b8d7317', 3, 33079, 447291),
    ('shared-component-position-plan.bin', b'OBSCN001', '16ba7e7f6192831f5ba201b5347896f16f14f9dc31352107e8fd5a43132ccf0b', 2, 5887, 309464),
    ('material-position-plan.bin', b'OBMAT001', '59a579b040fd13880a1ae1dff053eb06291df5f52b1b074aadeeab5e46a72593', 2, 487, 176927),
)


def exact(stream, size):
    value = stream.read(size)
    if len(value) != size:
        raise ValueError('truncated plan')
    return value


def validate(name, signature, expected_hash, groups, expected_rows, expected_bits):
    path = ROOT / name
    if path.is_symlink() or not path.is_file():
        raise ValueError('missing regular plan: ' + name)
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected_hash:
        raise ValueError('unexpected plan hash: ' + name)
    stream = io.BytesIO(data)
    if exact(stream, 8) != signature:
        raise ValueError('unexpected plan signature: ' + name)
    exact(stream, 64)  # input and output hashes
    rows = bits = 0
    for _ in range(groups):
        header = exact(stream, 68)
        count = struct.unpack_from('>I', header)[0]
        if count > 20000:
            raise ValueError('unexpected group size: ' + name)
        ranges = []
        for _ in range(count):
            pos, _, _, size = struct.unpack('>IIII', exact(stream, 16))
            if not 0 < size <= 1048576:
                raise ValueError('unexpected record size: ' + name)
            mask = exact(stream, (size + 7) // 8)
            if size % 8 and mask[-1] & ~((1 << (size % 8)) - 1):
                raise ValueError('nonzero unused mask bits: ' + name)
            bits += sum(x.bit_count() for x in mask)
            ranges.append((pos, pos + size))
            rows += 1
        ranges.sort()
        if any(x[1] > y[0] for x, y in zip(ranges, ranges[1:])):
            raise ValueError('overlapping record ranges: ' + name)
    if stream.read(1) or rows != expected_rows or bits != expected_bits:
        raise ValueError('unexpected plan length or counts: ' + name)
    print(name, 'records:', rows, 'masked positions:', bits)


def main():
    for plan in PLANS:
        validate(*plan)
    print('Position-only plans validated. The final APK build remains unverified.')


if __name__ == '__main__':
    main()
