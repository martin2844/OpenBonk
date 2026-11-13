#!/usr/bin/env python3
"""Compare project file names with exact metadata strings in Steam and Android.

This detects names, not code identity, authorship or distribution rights. The
result is local research evidence and does not make any input redistributable.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('apk', type=Path)
    parser.add_argument('steam_metadata', type=Path)
    parser.add_argument('scripts', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    if not args.scripts.is_dir(): parser.error('scripts directory missing')
    if args.output.exists() or args.output.is_symlink() or not args.output.parent.is_dir():
        parser.error('output must be a new file in an existing directory')
    steam = args.steam_metadata.read_bytes()
    with zipfile.ZipFile(args.apk) as archive:
        metadata_name = 'assets/bin/Data/Managed/Metadata/global-metadata.dat'
        code_name = 'lib/arm64-v8a/libil2cpp.so'
        if archive.namelist().count(metadata_name) != 1 or archive.namelist().count(code_name) != 1:
            raise ValueError('APK must have exactly one metadata and runtime entry')
        android = archive.read(metadata_name)
        code = archive.read(code_name)
    names = sorted({p.stem for p in args.scripts.glob('*.cs')
                    if len(p.stem) >= 4 and p.stem.isidentifier() and p.stem.isascii()})
    def contains(data, name):
        needle = name.encode('ascii') + b'\0'
        return data.startswith(needle) or b'\0' + needle in data
    rows = [{'name': name, 'steam': contains(steam, name), 'android': contains(android, name)} for name in names]
    summary = {'files_examined': len(rows),
               'names_in_both': sum(r['steam'] and r['android'] for r in rows),
               'names_only_steam': sum(r['steam'] and not r['android'] for r in rows),
               'names_only_android': sum(r['android'] and not r['steam'] for r in rows),
               'names_in_neither': sum(not r['android'] and not r['steam'] for r in rows)}
    report = {'apk': str(args.apk), 'apk_sha256': digest(args.apk.read_bytes()),
              'android_runtime_size': len(code), 'android_runtime_sha256': digest(code),
              'android_metadata_sha256': digest(android), 'steam_metadata_sha256': digest(steam),
              'scripts': str(args.scripts), 'method': 'ASCII script filename stem bounded by NUL bytes (or file start); names need not identify classes or implementation',
              'summary': summary, 'names': rows, 'limitations': __doc__}
    with args.output.open('x', encoding='utf-8') as out:
        json.dump(report, out, indent=2); out.write('\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__': main()
