#!/usr/bin/env python3
"""Check the pinned offset-and-hash-only audio and texture metadata plans."""
import hashlib
from pathlib import Path
import re
import struct


ROOT = Path(__file__).resolve().parent.parent
DIRECTORY = ROOT / 'analysis/data-conversion/metadata-plans'
PLANS = {
    'exact.bin': (b'OBSTX001', 56, 256, '052c62fc74526a35408e24ea1e32bcf9af7bd9b7f40bf47423bcdedee288b992'),
    'audio.bin': (b'OBSAU001', 63, 128, 'ae6023707a925ce13f6b222adcc691906f0e161c4d47c026a7ddf0fdad4c206a'),
}


def validate(name, data):
    header, expected_count, hash_bytes, expected_hash = PLANS[name]
    if hashlib.sha256(data).hexdigest() != expected_hash:
        raise ValueError(name + ': digest differs')
    if len(data) != 12 + hash_bytes + expected_count * 160 or data[:8] != header:
        raise ValueError(name + ': header or length differs')
    if struct.unpack_from('>I', data, 8)[0] != expected_count:
        raise ValueError(name + ': record count differs')
    seen = set()
    for index in range(expected_count):
        start = 12 + hash_bytes + 160 * index
        record = data[start:start + 160]
        asset = record[:32]
        if not re.fullmatch(rb'[0-9a-f]{32}', asset) or asset in seen:
            raise ValueError(name + ': invalid or duplicate asset ID')
        seen.add(asset)
        if name == 'exact.bin':
            offset, size, linux_id, linux_offset, windows_id, windows_offset = struct.unpack_from('>IIIQIQ', record, 128)
            if not size or linux_id >= 4 or windows_id >= 4 or offset + size > 2**32:
                raise ValueError(name + ': invalid texture range')
            if linux_offset + size > 2**63 or windows_offset + size > 2**63:
                raise ValueError(name + ': invalid source range')
        else:
            linux_offset, windows_offset, size, original_size, template_size, extra = struct.unpack_from('>QQIIII', record, 32)
            if not size or not template_size or extra not in (0, 4) or linux_offset + size > 2**63 or windows_offset + size > 2**63:
                raise ValueError(name + ': invalid audio range')
            if original_size > 2**31 or template_size > 2**31:
                raise ValueError(name + ': invalid audio metadata size')
    return expected_count


def main():
    for name in PLANS:
        path = DIRECTORY / name
        if path.is_symlink() or not path.is_file():
            raise ValueError(name + ': missing regular file')
        print(name, validate(name, path.read_bytes()), 'records verified')


if __name__ == '__main__':
    main()
