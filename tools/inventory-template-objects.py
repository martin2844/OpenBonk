#!/usr/bin/env python3
"""Classify readable hashed Unity template files by contained object types.

Requires UnityPy. Counts file bytes rather than attributing ownership of bytes
inside a file; this is an inventory, not a provenance determination.
"""
import argparse
from collections import Counter, defaultdict
import io
import json
from pathlib import Path
import re
from zipfile import ZipFile
import UnityPy

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('apk', type=Path); p.add_argument('inventory', type=Path); p.add_argument('report', type=Path)
p.add_argument('--reference-obb', type=Path, help='optional local reconstructed OBB for identifying masked shaders')
a = p.parse_args()
if a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir(): p.error('report must be new')
inv = json.loads(a.inventory.read_text())
prefix = 'assets/bin/Data/'
entries = [x for x in inv['entries'] if re.fullmatch(re.escape(prefix) + r'[0-9a-f]{32}', x['entry'])]
groups = defaultdict(lambda: {'files': 0, 'size': 0, 'nonzero_bytes': 0, 'object_counts': Counter(), 'examples': []})
errors = []
shaders = []
reference = ZipFile(a.reference_obb) if a.reference_obb else None
with ZipFile(a.apk) as outer:
    import hashlib
    with a.apk.open('rb') as f:
        if hashlib.file_digest(f, 'sha256').hexdigest() != inv['apk_sha256']: raise ValueError('inventory APK mismatch')
    manager_env = UnityPy.load(outer.read('assets/bin/Data/globalgamemanagers'))
    managers = [o for o in manager_env.objects if o.type.name == 'ResourceManager']
    if len(managers) != 1: raise ValueError('expected one ResourceManager')
    manager = managers[0]
    externals = manager.assets_file.externals
    resource_names = defaultdict(list)
    for name, ref in manager.read_typetree()['m_Container']:
        fid = ref['m_FileID']
        if 0 < fid <= len(externals):
            path = str(externals[fid - 1].path).replace('\\', '/').rsplit('/', 1)[-1].lower()
            resource_names[path].append(name)
    base_entry = inv.get('base_entry', 'assets/openbonk-diagnostic/base.obb')
    if base_entry not in ('assets/openbonk-diagnostic/base.obb', 'assets/openbonk-diagnostic/generated-audio-template.obb',
                          'assets/openbonk-diagnostic/windows/generated-audio-template.obb'):
        raise ValueError('unsupported template entry')
    with ZipFile(io.BytesIO(outer.read(base_entry))) as base:
        for row in entries:
            try:
                raw = base.read(row['entry'])
                if hashlib.sha256(raw).hexdigest() != row['sha256']: raise ValueError('entry hash mismatch')
                env = UnityPy.load(raw)
                types = Counter(obj.type.name for obj in env.objects)
                if not types: raise ValueError('no Unity objects')
                if set(types) == {'Shader'}:
                    try:
                        if reference:
                            original = reference.read(row['entry'])
                            if len(original) != len(raw) or any(x and x != y for x, y in zip(raw, original)):
                                raise ValueError('reference shader differs from unmasked bytes')
                            original_objects = UnityPy.load(original).objects
                            layout = lambda items: [(o.type.name, o.path_id, o.byte_start, o.byte_size) for o in items]
                            if layout(original_objects) != layout(env.objects):
                                raise ValueError('reference shader layout differs')
                            source_objects = original_objects
                        else:
                            source_objects = env.objects
                        names = []
                        for obj in source_objects:
                            shader_name = obj.read_typetree().get('m_ParsedForm', {}).get('m_Name', '')
                            names.append(shader_name if isinstance(shader_name, str) and len(shader_name) <= 512 else '')
                        shaders.append({'entry': row['entry'], 'name': names[0] if len(names) == 1 else None,
                                        'names': names, 'file_nonzero_bytes': row['nonzero_bytes'],
                                        'resource_manager_names': resource_names.get(row['entry'].rsplit('/', 1)[-1], []),
                                        'name_source': 'reference-obb' if reference else 'template'})
                    except Exception as error:
                        shaders.append({'entry': row['entry'], 'name': None, 'file_nonzero_bytes': row['nonzero_bytes'], 'error': type(error).__name__})
                key = ','.join(sorted(types))
                group = groups[key]; group['files'] += 1; group['size'] += row['size']; group['nonzero_bytes'] += row['nonzero_bytes']; group['object_counts'].update(types)
                if len(group['examples']) < 5: group['examples'].append(row['entry'])
            except Exception as e:
                errors.append({'entry': row['entry'], 'error': type(e).__name__})
if reference: reference.close()
result = {'apk_sha256': inv['apk_sha256'], 'hashed_files_considered': len(entries),
          'groups': dict(sorted(groups.items())), 'unclassified': errors, 'shaders': shaders,
          'limitations': 'Groups describe entire readable files. Zero bytes, headers, metadata and payloads are all counted together; no ownership, rendering use or licensing is determined. Reference shader names, when requested, are identified from a local OBB whose unmasked bytes match; this does not establish the identity of masked bytes. Unreadable entries in the prior inventory are excluded.'}
with a.report.open('x', encoding='utf-8') as f: json.dump(result, f, indent=2); f.write('\n')
print('hashed files:', len(entries), 'classified:', sum(g['files'] for g in groups.values()), 'unclassified:', len(errors))
