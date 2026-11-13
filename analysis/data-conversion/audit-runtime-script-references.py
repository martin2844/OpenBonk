#!/usr/bin/env python3
"""Inventory script identities and references in a pinned private Unity build."""
import collections
import hashlib
import json
from pathlib import Path
import re
import struct
import sys
import zipfile

import UnityPy

PINS = (
    '0bf818e3978f4363623931d5636eee73a9ac5458b6aa52e0458ebfd9495043d9',
    'f50aff5a9af4ae78968d4c56e721e961ae69c94e21cf012715fe07e6cfd8b260',
    '632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29',
)
PREFIX = 'assets/bin/Data/globalgamemanagers.assets.split'

def digest(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()

def script_index(apk):
    with zipfile.ZipFile(apk) as z:
        names = z.namelist()
        if len(names) != len(set(names)): raise ValueError('duplicate APK entry')
        parts = sorted((n for n in names if n.startswith(PREFIX)), key=lambda n: int(n[len(PREFIX):]))
        if not parts or [int(n[len(PREFIX):]) for n in parts] != list(range(len(parts))):
            raise ValueError('missing script registry parts')
        environment = UnityPy.load(b''.join(z.read(n) for n in parts))
    by_id, by_identity = {}, collections.defaultdict(list)
    for obj in environment.objects:
        if obj.type.name != 'MonoScript': continue
        x = obj.read()
        key = (x.m_Name, x.m_ClassName, x.m_Namespace, x.m_AssemblyName)
        by_id[obj.path_id] = key
        by_identity[key].append(obj.path_id)
    return by_id, by_identity

def main():
    if len(sys.argv) != 5: raise ValueError('usage: SCRIPT OLD_APK NEW_APK OBB NEW_REPORT')
    old_apk, new_apk, obb, report = map(Path, sys.argv[1:])
    if report.exists() or report.is_symlink() or not report.parent.is_dir(): raise ValueError('report must be new')
    allowed = ({PINS[0]}, {PINS[1], '5543620d74804f198d8c864dc45962c83b97d82787f13ea21bd7b3f93b19c37f'}, {PINS[2]})
    for path, pins in zip((old_apk, new_apk, obb), allowed):
        if not path.is_file() or digest(path) not in pins: raise ValueError('unexpected input')
    old, old_names = script_index(old_apk)
    new, new_names = script_index(new_apk)
    mapping, status = {}, collections.Counter()
    for path_id, key in old.items():
        candidates = new_names.get(key, [])
        state = 'missing' if not candidates else 'ambiguous' if len(candidates) > 1 else 'same' if candidates[0] == path_id else 'changed'
        status[state] += 1
        if len(candidates) == 1: mapping[path_id] = candidates[0]
    counts = collections.Counter()
    skipped, examples = [], []
    with zipfile.ZipFile(obb) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)): raise ValueError('duplicate OBB entry')
        candidates = [n for n in names if re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}', n)]
        for name in candidates:
            info = archive.getinfo(name)
            if info.file_size > 64 * 1024 * 1024: skipped.append({'entry': name, 'reason': 'size'}); continue
            try:
                env = UnityPy.load(archive.read(info))
                file = env.file
                if file is None or not hasattr(file, 'externals'): raise ValueError('not a serialized file')
                externals = [x.path.rsplit('/', 1)[-1] for x in file.externals]
                asset_counts, asset_examples = collections.Counter(), []
                def record(index, path_id, kind):
                    if path_id == 0: return
                    if index <= 0 or index > len(externals) or externals[index - 1] != 'globalgamemanagers.assets': return
                    state = ('unknown' if path_id not in old else 'missing_or_ambiguous' if path_id not in mapping else
                             'same' if mapping[path_id] == path_id else 'changed')
                    asset_counts[kind + '_' + state] += 1
                    if state == 'changed' and len(asset_examples) < 12:
                        asset_examples.append({'entry': name, 'kind': kind, 'old_id': path_id, 'new_id': mapping[path_id], 'identity': old[path_id]})
                for script in file.script_types:
                    record(script.local_serialized_file_index, script.local_identifier_in_file, 'metadata')
                for obj in env.objects:
                    if obj.type.name != 'MonoBehaviour': continue
                    data = obj.get_raw_data()
                    if len(data) < 28 or obj.reader.endian != '<': raise ValueError('unsupported MonoBehaviour layout')
                    index, path_id = struct.unpack_from('<iq', data, 16)
                    record(index, path_id, 'behaviour')
                counts.update(asset_counts)
                counts['assets_parsed'] += 1
                examples.extend(asset_examples[:max(0, 12 - len(examples))])
            except Exception as e:
                skipped.append({'entry': name, 'reason': type(e).__name__})
    result = {'old_apk_sha256': digest(old_apk), 'new_apk_sha256': digest(new_apk), 'obb_sha256': digest(obb),
              'old_scripts': len(old), 'new_scripts': len(new), 'old_identity_duplicates': sum(len(v) > 1 for v in old_names.values()),
              'registry_mapping': dict(status), 'candidate_assets': len(candidates), 'reference_counts': dict(counts),
              'skipped': skipped, 'examples': examples,
              'scope': 'Only 32-hex top-level serialized files and MonoBehaviour script pointers; not all OBB assets or references. Identity equality does not establish layout or behaviour compatibility.'}
    with report.open('x', encoding='utf-8') as out: json.dump(result, out, indent=2); out.write('\n')
    print(json.dumps({k: result[k] for k in ('old_scripts', 'new_scripts', 'registry_mapping', 'candidate_assets', 'reference_counts')}, sort_keys=True))
    print('skipped_assets=' + str(len(skipped)))

if __name__ == '__main__': main()
