#!/usr/bin/env python3
"""Inspect the two-pass diagnostic APK/OBB hash and Unity GUID relationship."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile


def file_hash(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('directory', type=Path)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    first, second, apk = (args.directory / name for name in ('first.obb', 'second.obb', 'second.apk'))
    first_hash, second_hash = file_hash(first), file_hash(second)
    with zipfile.ZipFile(apk) as az, zipfile.ZipFile(first) as fz, zipfile.ZipFile(second) as sz:
        for archive in (az, fz, sz):
            if len(archive.namelist()) != len(set(archive.namelist())):
                raise ValueError('duplicate archive entries')
        apk_guid = az.read('assets/unity_obb_guid')
        first_guid, second_guid = fz.read('unity_obb_guid'), sz.read('unity_obb_guid')
        dex = az.read('classes.dex')
        report = {
            'first_obb_sha256': first_hash, 'second_obb_sha256': second_hash,
            'second_apk_sha256': file_hash(apk),
            'first_obb_hash_occurrences_in_dex': dex.count(first_hash.encode('ascii')),
            'second_obb_hash_occurrences_in_dex': dex.count(second_hash.encode('ascii')),
            'second_apk_guid': apk_guid.decode('ascii'),
            'first_obb_guid': first_guid.decode('ascii'),
            'second_obb_guid': second_guid.decode('ascii'),
            'apk_guid_matches_first': apk_guid == first_guid,
            'apk_guid_matches_second': apk_guid == second_guid,
        }
    with args.report.open('x') as output:
        json.dump(report, output, indent=2)
        output.write('\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
