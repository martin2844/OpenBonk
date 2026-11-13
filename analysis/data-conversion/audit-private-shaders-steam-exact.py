#!/usr/bin/env python3
"""Compare packaged Android shader objects with same-name Steam objects, privately.

Exact-byte comparison only; absence of a match does not establish provenance,
rights, rendering equivalence, or inability to regenerate an Android shader.
"""
import argparse
import hashlib
import io
import json
from collections import defaultdict
from pathlib import Path
from zipfile import ZipFile

import UnityPy


def sha(data):
    return hashlib.sha256(data).hexdigest()


def source_objects(raw):
    result = defaultdict(list)
    for obj in UnityPy.load(raw).objects:
        kind = obj.type.name
        if kind not in ('Shader', 'ComputeShader'):
            continue
        tree = obj.read_typetree()
        name = tree['m_ParsedForm']['m_Name'] if kind == 'Shader' else tree['m_Name']
        data = obj.get_raw_data()
        result[(kind, name)].append({'size': len(data), 'sha256': sha(data)})
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path)
    p.add_argument('inventory', type=Path)
    p.add_argument('linux_asset', type=Path)
    p.add_argument('windows_zip', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be a new file in an existing directory')
    inventory = json.loads(a.inventory.read_text())
    with ZipFile(a.apk) as apk:
        with ZipFile(io.BytesIO(apk.read('assets/openbonk-diagnostic/plans.zip'))) as plans:
            pack_data = plans.read('shared1-shader-pack.zip')
    if sha(pack_data) != inventory['pack_sha256']:
        raise ValueError('APK pack differs from inventoried pack')
    linux_data = a.linux_asset.read_bytes()
    with ZipFile(a.windows_zip) as z:
        windows_data = z.read('Megabonk_Data/sharedassets1.assets')
    pins = json.loads((Path(__file__).resolve().parents[2] / 'tools/launcher-steam-inputs.json').read_text())['platforms']
    for platform, data in (('linux', linux_data), ('windows', windows_data)):
        expected = pins[platform]['sharedassets1.assets']
        if len(data) != expected['size'] or sha(data) != expected['sha256']:
            raise ValueError('Steam asset does not match the pinned ' + platform + ' input')
    platforms = {'linux': source_objects(linux_data), 'windows': source_objects(windows_data)}
    rows = []
    with ZipFile(io.BytesIO(pack_data)) as pack:
        for row in inventory['objects']:
            blob = pack.read(row['entry'])
            if sha(blob) != row['source_sha256']:
                raise ValueError('unexpected shader bytes')
            entry = {'name': row['name'], 'type': row['type'], 'android_size': len(blob), 'android_sha256': sha(blob)}
            for platform, sources in platforms.items():
                matches = sources.get((row['type'], row['name']), [])
                entry[platform] = {'same_name_count': len(matches),
                                   'exact_matches': sum(x['sha256'] == sha(blob) for x in matches),
                                   'candidate_sizes': sorted(x['size'] for x in matches)}
            rows.append(entry)
    report = {'apk_sha256': sha(a.apk.read_bytes()), 'pack_sha256': sha(pack_data),
              'linux_asset_sha256': sha(linux_data), 'windows_asset_sha256': sha(windows_data),
              'counts': {platform: {'with_same_name': sum(r[platform]['same_name_count'] > 0 for r in rows),
                                    'with_exact_match': sum(r[platform]['exact_matches'] > 0 for r in rows)}
                         for platform in platforms},
              'objects': rows,
              'limitations': ['Only same-name objects in sharedassets1.assets are compared.',
                              'Exact serialized bytes only, not matching byte ranges or semantic equivalence.',
                              'Does not establish rights, provenance, shader regeneration or rendering behavior.']}
    with a.output.open('x', encoding='utf-8') as f:
        json.dump(report, f, ensure_ascii=False, indent=2); f.write('\n')
    print(json.dumps(report['counts']))


if __name__ == '__main__': main()
