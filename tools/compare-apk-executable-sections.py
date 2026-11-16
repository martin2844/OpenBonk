#!/usr/bin/env python3
"""Compare executable ELF sections in two Android APKs without exporting them.

This is a code-byte check, not proof that metadata, assets or behavior match.
It supports unextended little-endian ELF64 section tables only.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile

LIBRARY = 'lib/arm64-v8a/libil2cpp.so'
MAX_LIBRARY = 512 * 1024 * 1024


def executable_sections(data):
    if len(data) < 64 or data[:7] != b'\x7fELF\x02\x01\x01' or struct.unpack_from('<HHI', data, 16) != (3, 183, 1):
        raise ValueError('expected a little-endian ARM64 ELF shared library')
    offset = struct.unpack_from('<Q', data, 0x28)[0]
    size, count, strings = struct.unpack_from('<HHH', data, 0x3a)
    if size != 64 or not count or strings >= count or offset + size * count > len(data):
        raise ValueError('invalid or unsupported section table')

    def section(i):
        return struct.unpack_from('<IIQQQQIIQQ', data, offset + i * size)

    table = section(strings)
    if table[1] != 3 or table[4] + table[5] > len(data):
        raise ValueError('invalid section names')
    names = data[table[4]:table[4] + table[5]]
    result = {}
    for i in range(count):
        item = section(i)
        if not item[2] & 4: continue  # SHF_EXECINSTR
        if item[1] == 8 or item[0] >= len(names) or item[4] + item[5] > len(data):
            raise ValueError('invalid executable section')
        end = names.find(b'\0', item[0])
        if end < 0: raise ValueError('unterminated section name')
        try: name = names[item[0]:end].decode('ascii')
        except UnicodeDecodeError as error: raise ValueError('invalid section name') from error
        if not name or name in result: raise ValueError('duplicate or empty executable section name')
        payload = data[item[4]:item[4] + item[5]]
        result[name] = {'address': item[3], 'size': len(payload), 'sha256': hashlib.sha256(payload).hexdigest()}
    if not result: raise ValueError('no executable sections')
    return result


def apk_sections(path):
    with zipfile.ZipFile(path) as archive:
        matches = [item for item in archive.infolist() if item.filename == LIBRARY]
        if len(matches) != 1 or not 0 < matches[0].file_size <= MAX_LIBRARY:
            raise ValueError('missing, duplicate or oversized IL2CPP library')
        return executable_sections(archive.read(matches[0]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('first', type=Path)
    parser.add_argument('second', type=Path)
    args = parser.parse_args()
    try:
        first, second = apk_sections(args.first), apk_sections(args.second)
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        parser.exit(2, f'Cannot compare APKs: {error}\n')
    print(json.dumps({'match': first == second, 'first': first, 'second': second}, indent=2))
    if first != second: parser.exit(1, 'Executable sections differ.\n')
    print('Executable section bytes match; metadata and behavior remain unverified.')


if __name__ == '__main__': main()
