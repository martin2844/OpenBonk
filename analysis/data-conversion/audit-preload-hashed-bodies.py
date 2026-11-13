#!/usr/bin/env python3
"""Inventory preload objects in hashed Android assets and exact Steam candidates.

Raw equality after a 12-byte prefix is only a candidate, not semantic identity.
This local diagnostic does not produce game assets or clear them for distribution.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import zipfile

import UnityPy


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('android_preload', type=Path)
    p.add_argument('android_obb', type=Path)
    p.add_argument('steam_data', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--expanded-steam', action='store_true', help='also search pinned resources, global assets and level files')
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    preload_bytes = a.android_preload.read_bytes()
    preload = [o for o in UnityPy.load(preload_bytes).objects if o.type.name == 'PreloadData']
    if len(preload) != 1:
        raise ValueError('expected exactly one preload object')
    obj = preload[0]
    externals = [str(x.path).replace('\\', '/').rsplit('/', 1)[-1].lower() for x in obj.assets_file.externals]
    if len(externals) != len(set(externals)):
        raise ValueError('ambiguous external names')
    targets = defaultdict(set)
    for ref in obj.read_typetree()['m_Assets']:
        file_id, path_id = ref['m_FileID'], ref['m_PathID']
        if file_id <= 0 or file_id > len(externals):
            raise ValueError('unexpected file ID')
        name = externals[file_id - 1]
        if re.fullmatch('[0-9a-f]{32}', name):
            if path_id in targets[name]:
                raise ValueError('duplicate reference')
            targets[name].add(path_id)
    if not targets:
        raise ValueError('no hashed asset references found')
    indices = [defaultdict(list), defaultdict(list)]
    steam_hashes = {}
    filenames = ['sharedassets1.assets', 'sharedassets2.assets']
    if a.expanded_steam:
        filenames += ['sharedassets0.assets', 'sharedassets4.assets', 'resources.assets',
                      'globalgamemanagers.assets', 'level1', 'level2', 'level4']
        pins = json.loads((Path(__file__).resolve().parents[2]/'tools/steam-linux-diagnostic-inputs.json').read_text())
    for filename in filenames:
        data = (a.steam_data / filename).read_bytes()
        steam_hashes[filename] = sha(data)
        if a.expanded_steam and (filename not in pins or len(data) != pins[filename]['size'] or steam_hashes[filename] != pins[filename]['sha256']):
            raise ValueError('unsupported Steam source: ' + filename)
        for source in UnityPy.load(data).objects:
            raw = source.get_raw_data()
            for i, body in enumerate((raw, raw[12:] if len(raw) >= 12 else b'')):
                if body:
                    indices[i][(source.type.name, len(body), sha(body))].append((filename, source.path_id))
    stats, missing = defaultdict(Counter), []
    with zipfile.ZipFile(a.android_obb) as z:
        if len(z.namelist()) != len(set(z.namelist())):
            raise ValueError('duplicate OBB entry')
        for filename, ids in sorted(targets.items()):
            member = 'assets/bin/Data/' + filename
            if member not in z.namelist():
                missing.append(filename)
                continue
            objects = UnityPy.load(z.read(member)).objects
            by_id = {o.path_id: o for o in objects}
            if len(by_id) != len(objects) or not ids <= by_id.keys():
                raise ValueError('preload references absent or duplicated in ' + filename)
            for path_id in ids:
                target = by_id[path_id]
                raw = target.get_raw_data()
                row = stats[target.type.name]
                row['referenced'] += 1
                for label, index, body in (('exact', indices[0], raw),
                                           ('after_12_bytes', indices[1], raw[12:] if len(raw) >= 12 else b'')):
                    candidates = index.get((target.type.name, len(body), sha(body)), []) if body else []
                    if candidates:
                        row[label + '_candidate'] += 1
                        if len(candidates) == 1:
                            row[label + '_unique'] += 1
    if missing:
        raise ValueError(f'{len(missing)} hashed asset entries missing from OBB')
    result = {'preload_sha256': sha(preload_bytes), 'obb_sha256': sha(a.android_obb.read_bytes()),
              'steam_sha256': steam_hashes, 'hashed_files': len(targets),
              'referenced_objects': sum(len(x) for x in targets.values()),
              'by_type': {k: dict(v) for k, v in sorted(stats.items())},
              'limitations': 'Candidates use raw equality, or equality after skipping 12 bytes; this does not validate object identity, pointers, ownership or distribution rights. Only the Steam asset files listed here were searched.'}
    with a.output.open('x', encoding='utf-8') as f:
        json.dump(result, f, indent=2)
        f.write('\n')
    print(f"{len(targets)} hashed files, {result['referenced_objects']} preload objects; report {a.output}")


if __name__ == '__main__':
    main()
