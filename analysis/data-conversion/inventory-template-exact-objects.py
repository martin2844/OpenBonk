#!/usr/bin/env python3
"""Find byte-identical serialized objects between a local template and Steam files.

This reports identities and hashes only. It does not export asset contents,
establish provenance, or decide which objects may be distributed.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import re
import zipfile
import zlib

import UnityPy

PREFIX = 'assets/bin/Data/'
MINIMUM = 256


def sha(data): return hashlib.sha256(data).hexdigest()


def obj_name(obj):
    try: return str(getattr(obj.read(), 'm_Name', ''))[:120]
    except Exception: return ''


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path)
    p.add_argument('steam_data', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir(): p.error('output must be new')
    targets = collections.defaultdict(list)
    examined = collections.Counter(); skipped = []
    with zipfile.ZipFile(a.base) as z:
        if len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate template entries')
        groups = collections.defaultdict(list)
        for info in z.infolist():
            if not info.filename.startswith(PREFIX) or info.is_dir(): continue
            name = info.filename[len(PREFIX):]
            match = re.fullmatch(r'(sharedassets\d+\.assets)\.split(\d+)', name)
            if match: groups[match.group(1)].append((int(match.group(2)), info.filename))
            elif re.fullmatch(r'[0-9a-f]{32}|level\d+|resources\.assets', name): groups[name].append((0, info.filename))
        for name, parts in sorted(groups.items()):
            parts.sort()
            if [x[0] for x in parts] != list(range(len(parts))): raise ValueError('non-contiguous split files')
            if sum(z.getinfo(entry).file_size for _, entry in parts) > 256 * 1024 * 1024: raise ValueError('template asset too large')
            try: data = b''.join(z.read(entry) for _, entry in parts)
            except (OSError, zipfile.BadZipFile, zlib.error) as exc:
                skipped.append({'file': name, 'error': type(exc).__name__}); continue
            try: objects = list(UnityPy.load(data).objects)
            except Exception as exc:
                skipped.append({'file': name, 'error': type(exc).__name__}); continue
            if not objects: skipped.append({'file': name, 'error': 'no objects'}); continue
            for obj in objects:
                examined[obj.type.name] += 1
                if obj.byte_size < MINIMUM: continue
                raw = obj.get_raw_data()
                if len(raw) != obj.byte_size: raise ValueError('template object size differs')
                targets[(obj.type.name, len(raw), sha(raw))].append({'file': name, 'path_id': obj.path_id, 'name': obj_name(obj)})
    source_files = sorted(path for path in a.steam_data.iterdir()
                          if path.is_file() and (re.fullmatch(r'sharedassets\d+\.assets', path.name)
                                                or re.fullmatch(r'level\d+', path.name) or path.name == 'resources.assets'))
    if not source_files: raise ValueError('no Steam assets')
    source_hashes = {}; matches = collections.defaultdict(list)
    for path in source_files:
        data = path.read_bytes()
        if len(data) > 256 * 1024 * 1024: raise ValueError('Steam asset too large')
        source_hashes[path.name] = sha(data)
        for obj in UnityPy.load(data).objects:
            if obj.byte_size < MINIMUM: continue
            raw = obj.get_raw_data(); key = (obj.type.name, len(raw), sha(raw))
            if key in targets:
                matches[key].append({'file': path.name, 'path_id': obj.path_id, 'name': obj_name(obj)})
    rows = [{'type': kind, 'size': size, 'sha256': digest, 'template_objects': targets[key], 'steam_objects': sources}
            for key, sources in matches.items() for kind, size, digest in [key]]
    rows.sort(key=lambda row: (-row['size'], row['type'], row['sha256']))
    report = {'base_sha256': sha(a.base.read_bytes()), 'source_sha256': source_hashes,
              'minimum_object_bytes': MINIMUM, 'template_objects_by_type': dict(examined),
              'matched_distinct_objects': len(rows), 'matches': rows, 'skipped_template_files': skipped,
              'limitations': ['Exact whole serialized objects only; changed metadata or partial payload matches are excluded.',
                              'A match is not evidence of rights, provenance, or runtime use.',
                              'Only the listed local Steam files and parsed template entries were scanned.']}
    with a.output.open('x', encoding='utf-8') as f: json.dump(report, f, indent=2); f.write('\n')
    print('Parsed', sum(examined.values()), 'template objects; matched', len(rows), 'distinct raw objects; skipped', len(skipped), 'template files.')


if __name__ == '__main__': main()
