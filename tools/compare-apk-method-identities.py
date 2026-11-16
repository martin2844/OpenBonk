#!/usr/bin/env python3
"""Compare selected Unity 2023.2 IL2CPP metadata method identities in two APKs.

This ignores return and parameter types, generic context, method bodies, data,
attributes and runtime behavior. Matching identities do not prove equivalence.
"""
import argparse
from collections import Counter
import json
from pathlib import Path
import struct
import zipfile

ENTRY = 'assets/bin/Data/Managed/Metadata/global-metadata.dat'
MAX_METADATA = 256 * 1024 * 1024


def method_identities(data):
    if len(data) < 256 or struct.unpack_from('<II', data, 0) != (0xFAB11BAF, 29):
        raise ValueError('expected Unity 2023.2 IL2CPP metadata version 29')

    def table(header, record_size=1):
        offset, size = struct.unpack_from('<II', data, header)
        if offset < 256 or not size or offset + size > len(data) or size % record_size:
            raise ValueError('invalid metadata table')
        return offset, size

    string_offset, string_size = table(24)
    strings = data[string_offset:string_offset + string_size]

    def string(index):
        if index >= len(strings): raise ValueError('invalid metadata string index')
        end = strings.find(b'\0', index)
        if end < 0: raise ValueError('unterminated metadata string')
        return strings[index:end]

    type_offset, type_size = table(160, 88)
    types = [struct.unpack_from('<II', data, pos) for pos in range(type_offset, type_offset + type_size, 88)]
    owner = [None] * len(types)
    image_offset, image_size = table(168, 40)
    for pos in range(image_offset, image_offset + image_size, 40):
        name, _, start, count = struct.unpack_from('<IIII', data, pos)
        if start + count > len(types): raise ValueError('invalid image type range')
        image_name = string(name)
        for index in range(start, start + count):
            if owner[index] is not None: raise ValueError('overlapping image type ranges')
            owner[index] = image_name
    if any(name is None for name in owner): raise ValueError('unassigned metadata type')

    method_offset, method_size = table(48, 32)
    identities = []
    for pos in range(method_offset, method_offset + method_size, 32):
        name, declaring, _, _, _, token, flags, iflags, slot, count = struct.unpack_from('<IiiiiIHHHH', data, pos)
        if declaring < 0 or declaring >= len(types): raise ValueError('invalid declaring type')
        type_name, namespace = types[declaring]
        identities.append((owner[declaring], string(namespace), string(type_name), string(name), token, flags, iflags, slot, count))
    return identities


def apk_methods(path):
    with zipfile.ZipFile(path) as archive:
        entries = [entry for entry in archive.infolist() if entry.filename == ENTRY]
        if len(entries) != 1 or not 0 < entries[0].file_size <= MAX_METADATA:
            raise ValueError('missing, duplicate or oversized metadata')
        return method_identities(archive.read(entries[0]))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('first', type=Path); parser.add_argument('second', type=Path)
    args = parser.parse_args()
    try:
        first, second = apk_methods(args.first), apk_methods(args.second)
    except (OSError, ValueError, zipfile.BadZipFile, struct.error) as error:
        parser.exit(2, f'Cannot compare metadata: {error}\n')
    a, b = Counter(first), Counter(second)
    report = {'match': a == b, 'first_methods': len(first), 'second_methods': len(second),
              'different_ordinals': sum(x != y for x, y in zip(first, second)) + abs(len(first) - len(second)),
              'unmatched_first': sum((a - b).values()), 'unmatched_second': sum((b - a).values())}
    print(json.dumps(report, indent=2))
    if a != b: parser.exit(1, 'Selected method identities differ.\n')
    print('Selected method identities match; types, method bodies and behavior remain unverified.')


if __name__ == '__main__': main()
