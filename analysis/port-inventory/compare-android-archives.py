#!/usr/bin/env python3
"""Compare uncompressed entry bytes in two APKs or OBBs, without extracting them."""
import argparse
import hashlib
import json
import zipfile
from pathlib import Path


def digest(stream):
    result = hashlib.sha256()
    for chunk in iter(lambda: stream.read(1024 * 1024), b''):
        result.update(chunk)
    return result.hexdigest()


def contents(path):
    with zipfile.ZipFile(path) as archive:
        infos = archive.infolist()
        if len({i.filename for i in infos}) != len(infos):
            raise ValueError(f'duplicate archive entry in {path}')
        entries = {}
        for info in infos:
            if info.is_dir():
                continue
            with archive.open(info) as stream:
                entries[info.filename] = {'size': info.file_size, 'sha256': digest(stream)}
        return entries


def file_digest(path):
    with path.open('rb') as stream:
        return digest(stream)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('old', type=Path)
    parser.add_argument('new', type=Path)
    parser.add_argument('report', type=Path, help='new JSON report path')
    args = parser.parse_args()
    old, new = contents(args.old), contents(args.new)
    common = old.keys() & new.keys()
    changed = sorted(name for name in common if old[name] != new[name])
    report = {
        'old': str(args.old), 'new': str(args.new),
        'old_archive_sha256': file_digest(args.old),
        'new_archive_sha256': file_digest(args.new),
        'unchanged_count': len(common) - len(changed),
        'removed': {name: old[name] for name in sorted(old.keys() - new.keys())},
        'added': {name: new[name] for name in sorted(new.keys() - old.keys())},
        'changed': {name: {'old': old[name], 'new': new[name]} for name in changed},
    }
    with args.report.open('x') as output:
        json.dump(report, output, indent=2)
        output.write('\n')
    print(f"unchanged={report['unchanged_count']} changed={len(changed)} removed={len(report['removed'])} added={len(report['added'])}; report={args.report}")


if __name__ == '__main__':
    main()
