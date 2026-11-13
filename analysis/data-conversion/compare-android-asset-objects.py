#!/usr/bin/env python3
"""Compare serialized Unity objects by path ID across two local build archives.

This is a compatibility inventory, not proof that references, content or
rights are interchangeable. It does not modify either archive.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import re
import zipfile
import UnityPy


def load_archive(z, stem):
    name = 'assets/bin/Data/' + stem
    entries = [n for n in z.namelist() if n == name or n.startswith(name + '.split')]
    if entries == [name]: return z.read(name)
    if not entries or name in entries or any(not re.fullmatch(re.escape(name) + r'\.split\d+', n) for n in entries):
        raise ValueError('missing or ambiguous asset fragments: ' + name)
    entries.sort(key=lambda n: int(n.rsplit('split', 1)[1]))
    if [int(n.rsplit('split', 1)[1]) for n in entries] != list(range(len(entries))): raise ValueError('missing asset fragment')
    return b''.join(z.read(n) for n in entries)


def inventory(data):
    env = UnityPy.load(data); result = {}
    for obj in env.objects:
        if obj.path_id in result: raise ValueError('duplicate path ID')
        raw = obj.get_raw_data()
        result[obj.path_id] = {'type': obj.type.name, 'size': len(raw), 'sha256': hashlib.sha256(raw).hexdigest()}
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('old', type=Path); p.add_argument('new', type=Path); p.add_argument('asset'); p.add_argument('report', type=Path)
    a = p.parse_args()
    with zipfile.ZipFile(a.old) as old, zipfile.ZipFile(a.new) as new:
        if len(old.namelist()) != len(set(old.namelist())) or len(new.namelist()) != len(set(new.namelist())): raise ValueError('duplicate archive entries')
        old_data, new_data = load_archive(old, a.asset), load_archive(new, a.asset)
    old, new = inventory(old_data), inventory(new_data)
    summary = collections.defaultdict(collections.Counter); changed = []
    for key in sorted(old.keys() | new.keys()):
        o, n = old.get(key), new.get(key)
        typ = (o or n)['type']
        status = 'added' if o is None else 'removed' if n is None else 'type_changed' if o['type'] != n['type'] else 'identical' if o == n else 'changed'
        summary[typ][status] += 1
        if status != 'identical': changed.append({'path_id': key, 'status': status, 'old': o, 'new': n})
    result = {'asset': a.asset, 'old_file_sha256': hashlib.sha256(old_data).hexdigest(), 'new_file_sha256': hashlib.sha256(new_data).hexdigest(), 'old_file_size': len(old_data), 'new_file_size': len(new_data), 'summary': {k: dict(v) for k,v in sorted(summary.items())}, 'different_objects': changed}
    with a.report.open('x') as f: json.dump(result, f, indent=2); f.write('\n')
    print(a.asset, 'old objects', len(old), 'new objects', len(new), 'different', len(changed), 'report', a.report, flush=True)


if __name__ == '__main__': main()
