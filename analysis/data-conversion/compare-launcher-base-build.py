#!/usr/bin/env python3
"""Compare APK entry contents without copying them or assuming distribution rights."""
import argparse
import hashlib
import json
from collections import Counter
from pathlib import Path
import zipfile


def inventory(path):
    result = {}
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        if len(entries) > 5000 or len({entry.filename for entry in entries}) != len(entries):
            raise ValueError('too many entries or duplicate entry names')
        for entry in entries:
            if entry.is_dir():
                continue
            if entry.file_size > 512 * 1024 * 1024:
                raise ValueError('entry exceeds the inspection limit')
            digest = hashlib.sha256()
            with archive.open(entry) as source:
                while block := source.read(1024 * 1024):
                    digest.update(block)
            result[entry.filename] = {'size': entry.file_size, 'sha256': digest.hexdigest(),
                                      'compression': entry.compress_type}
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('base', type=Path)
    parser.add_argument('built', type=Path)
    parser.add_argument('report', type=Path)
    args = parser.parse_args()
    if not args.base.is_file() or not args.built.is_file() or args.report.exists() or args.report.is_symlink() or not args.report.parent.is_dir():
        parser.error('inputs must exist; report must be a new file in an existing directory')
    base, built = inventory(args.base), inventory(args.built)
    entries = []
    for name in sorted(base.keys() | built.keys()):
        left, right = base.get(name), built.get(name)
        status = ('built-only' if left is None else 'base-only' if right is None else
                  'same-content' if left['sha256'] == right['sha256'] and left['size'] == right['size'] else 'different')
        entries.append({'entry': name, 'status': status, 'base': left, 'built': right})
    result = {'counts': dict(Counter(entry['status'] for entry in entries)), 'entries': entries,
              'scope': 'Content hashes only; does not establish runtime equivalence, provenance, privacy or distribution rights.'}
    with args.report.open('x', encoding='utf-8') as output:
        json.dump(result, output, indent=2)
        output.write('\n')
    print(json.dumps(result['counts'], sort_keys=True))


if __name__ == '__main__':
    main()
