#!/usr/bin/env python3
"""Measure three-way position matches for serialized scene components.

Names and sizes are only matching heuristics. The report contains no game
bytes and does not establish semantic equivalence, provenance or rights.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import zipfile
import UnityPy

spec = importlib.util.spec_from_file_location('particle_audit', Path(__file__).with_name('audit-particle-template-matches.py'))
audit = importlib.util.module_from_spec(spec); spec.loader.exec_module(audit)
layout_skipped = Counter()


def objects(data, reference=None):
    env = UnityPy.load(data); items = list(env.objects)
    reference_items = list(UnityPy.load(reference).objects) if reference is not None else items
    reference_by_id = {o.path_id:o for o in reference_items}
    names = {o.path_id: o.read_typetree()['m_Name'] for o in reference_items if o.type.name == 'GameObject'}
    result = defaultdict(list)
    for obj in items:
        kind = obj.type.name; raw = obj.get_raw_data()
        ref_obj = reference_by_id.get(obj.path_id)
        if ref_obj is None or ref_obj.type.name != kind or ref_obj.byte_size != obj.byte_size:
            if reference is None: raise ValueError('source object layout differs')
            layout_skipped[kind] += 1
            continue
        reference_raw = ref_obj.get_raw_data()
        if kind == 'GameObject':
            name = names[obj.path_id]
        elif len(raw) >= 12:
            file_id, path_id = struct.unpack_from('<iq', reference_raw)
            if file_id != 0 or path_id not in names: continue
            name = names[path_id]
        else: continue
        result[(kind, name, len(raw))].append((obj.path_id, obj.byte_start, raw))
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('base', type=Path); p.add_argument('linux_data', type=Path)
    p.add_argument('windows_zip', type=Path); p.add_argument('output', type=Path)
    p.add_argument('--scene', choices=('level1','level2','level4','sharedassets1.assets','sharedassets2.assets'), required=True)
    p.add_argument('--reference-obb', type=Path, required=True)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink(): p.error('output must be new')
    target = audit.template(a.base, a.scene); linux = (a.linux_data/a.scene).read_bytes()
    with zipfile.ZipFile(a.reference_obb) as z:
        entries = [(int(i.filename.rsplit('split',1)[1]),i) for i in z.infolist()
                   if i.filename.startswith('assets/bin/Data/'+a.scene+'.split')]
        entries.sort(key=lambda x:x[0])
        if [x[0] for x in entries] != list(range(len(entries))): raise ValueError('invalid reference splits')
        reference = b''.join(z.read(i) for _,i in entries)
    with zipfile.ZipFile(a.windows_zip) as z:
        entries = [i for i in z.infolist() if i.filename == 'Megabonk_Data/'+a.scene]
        if len(entries) != 1: raise ValueError('missing Windows scene')
        windows = z.read(entries[0])
    ts, ls, ws = objects(target,reference), objects(linux), objects(windows)
    counts = Counter(); per_type = Counter(); rows=[]
    for key, candidates in sorted(ts.items()):
        kind, name, size = key
        for ident, pos, before in candidates:
            counts['target_objects'] += 1
            left, right = ls[key], ws[key]
            if not left or not right:
                counts['no_candidate'] += 1; continue
            def choose(options):
                return max(options, key=lambda o:(sum(x != 0 and x == y for x,y in zip(before,o[2])), -o[0]))
            lc, wc = choose(left), choose(right)
            nonzero = sum(x != 0 and x == y == z for x,y,z in zip(before,lc[2],wc[2]))
            counts['matched_objects'] += 1; counts['matching_nonzero_bytes'] += nonzero; per_type[kind] += nonzero
            rows.append({'type':kind, 'name':name, 'size':size, 'target_id':ident, 'target_offset':pos,
                         'linux_id':lc[0], 'windows_id':wc[0], 'linux_candidates':len(left),
                         'windows_candidates':len(right), 'matching_nonzero_bytes':nonzero})
    h=lambda b:hashlib.sha256(b).hexdigest()
    report={'scene':a.scene,'base_sha256':h(a.base.read_bytes()),'linux_sha256':h(linux),'windows_sha256':h(windows),
            'counts':dict(counts),'layout_skipped_by_type':dict(layout_skipped),'matching_nonzero_by_type':dict(per_type),'objects':rows,
            'limitations':['Matching by owner name, type, size and byte similarity is heuristic.',
                           'Objects whose reference layouts differ are skipped; fields and references were not semantically validated.',
                           'Only three-way equal bytes are counted; no distribution rights are determined.']}
    with a.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    print(a.scene,'objects',counts['target_objects'],'matched',counts['matched_objects'],
          'matching nonzero bytes',counts['matching_nonzero_bytes'],'top types',per_type.most_common(5))


if __name__ == '__main__': main()
