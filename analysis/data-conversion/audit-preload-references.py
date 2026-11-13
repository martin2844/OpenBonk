#!/usr/bin/env python3
"""Compare preload reference inventories across two local Unity assets.

Equal numeric IDs are only candidates: this does not resolve object identity,
reconstruct assets, establish ownership, or make either asset distributable.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

import UnityPy


def read_asset(path):
    data = path.read_bytes()
    env = UnityPy.load(data)
    matches = [o for o in env.objects if o.type.name == 'PreloadData']
    if len(matches) != 1:
        raise ValueError(f'{path}: expected exactly one PreloadData object')
    obj = matches[0]
    tree = obj.read_typetree()
    external = obj.assets_file.externals
    names = [str(x.path).replace('\\', '/').rsplit('/', 1)[-1].lower() for x in external]
    if any(not x for x in names) or len(names) != len(set(names)):
        raise ValueError(f'{path}: ambiguous external paths')
    counters = {}
    for ref in tree['m_Assets']:
        file_id, path_id = ref['m_FileID'], ref['m_PathID']
        if file_id <= 0 or file_id > len(names):
            raise ValueError(f'{path}: unexpected preload file ID {file_id}')
        counters.setdefault(names[file_id - 1], Counter())[path_id] += 1
    return {'sha256': hashlib.sha256(data).hexdigest(), 'object_bytes': len(obj.get_raw_data()),
            'reference_count': len(tree['m_Assets']), 'external_count': len(names),
            'dependency_count': len(tree['m_Dependencies']), 'references': counters}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('android_asset', type=Path)
    p.add_argument('steam_asset', type=Path)
    p.add_argument('output', type=Path)
    args = p.parse_args()
    if args.output.exists() or args.output.is_symlink() or not args.output.parent.is_dir():
        p.error('output must be a new file in an existing directory')
    android, steam = read_asset(args.android_asset), read_asset(args.steam_asset)
    ar, sr = android.pop('references'), steam.pop('references')
    shared = []
    for name in sorted(ar.keys() & sr.keys()):
        a, s = ar[name], sr[name]
        shared.append({'external_name': name, 'android_refs': sum(a.values()),
                       'steam_refs': sum(s.values()),
                       'numeric_id_overlap': sum((a & s).values()),
                       'android_only_numeric_ids': sum((a - s).values()),
                       'steam_only_numeric_ids': sum((s - a).values())})
    result = {'android': android, 'steam': steam, 'shared_external_files': shared,
              'android_only_external_files': len(ar.keys() - sr.keys()),
              'android_only_external_refs': sum(sum(ar[n].values()) for n in ar.keys() - sr.keys()),
              'steam_only_external_files': len(sr.keys() - ar.keys()),
              'steam_only_external_refs': sum(sum(sr[n].values()) for n in sr.keys() - ar.keys()),
              'limitations': 'Numeric path ID overlap is not proof of matching objects or permission to distribute. Dependencies and semantics are not resolved.'}
    with args.output.open('x', encoding='utf-8') as f:
        json.dump(result, f, indent=2)
        f.write('\n')
    print(f"Android {android['reference_count']} references; Steam {steam['reference_count']}; "
          f"{result['android_only_external_refs']} Android references use external files absent from Steam")


if __name__ == '__main__':
    main()
