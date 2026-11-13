#!/usr/bin/env python3
"""Count remaining nonzero template bytes by Unity object type.

This describes bounded serialized object ranges, not their provenance or rights.
Names, object contents, profiles and source bytes are not written to the report.
"""
import argparse
from collections import Counter
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import zipfile
import UnityPy

spec = importlib.util.spec_from_file_location('particle_template', Path(__file__).with_name('audit-particle-template-matches.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
SCENES = ('level1', 'level2', 'level4', 'sharedassets1.assets', 'sharedassets2.assets')
BASE = 'd1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431'
REFERENCE = 'c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5'

def digest(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('base',type=Path);p.add_argument('reference',type=Path);p.add_argument('report',type=Path)
    a=p.parse_args()
    if a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():p.error('report must be new')
    if digest(a.base)!=BASE or digest(a.reference)!=REFERENCE:raise ValueError('unexpected input hash')
    results={}
    with zipfile.ZipFile(a.reference) as z:
        for name in SCENES:
            target=module.template(a.base,name)
            entries=[]
            for info in z.infolist():
                m=re.fullmatch(r'assets/bin/Data/'+re.escape(name)+r'\.split(\d+)',info.filename)
                if m:entries.append((int(m.group(1)),info))
            entries.sort(key=lambda x:x[0])
            if not entries or [n for n,_ in entries]!=list(range(len(entries))):raise ValueError('reference splits missing')
            reference=b''.join(z.read(i) for _,i in entries)
            objects=list(UnityPy.load(target).objects);ref=list(UnityPy.load(reference).objects)
            by_id={o.path_id:o for o in ref}
            if len(by_id)!=len(ref):raise ValueError('duplicate reference object ID')
            types=Counter();counts=Counter();occupied=[];unverified=Counter()
            for o in objects:
                other=by_id.get(o.path_id)
                lo,hi=o.byte_start,o.byte_start+o.byte_size
                if lo<0 or hi>len(target):raise ValueError('invalid object bounds')
                occupied.append((lo,hi));value=target[lo:hi];nonzero=len(value)-value.count(0)
                types[o.type.name]+=nonzero;counts[o.type.name]+=1
                if other is None or (o.type.name,o.byte_size)!=(other.type.name,other.byte_size):unverified[o.type.name]+=1
            occupied.sort()
            if any(occupied[i][0]<occupied[i-1][1] for i in range(1,len(occupied))):raise ValueError('overlapping objects')
            total=len(target)-target.count(0)
            object_total=sum(types.values())
            results[name]={'size':len(target),'total_nonzero':total,'object_nonzero':object_total,
                           'outside_objects_nonzero':total-object_total,
                           'objects_by_type':dict(counts),'nonzero_by_type':dict(types),
                           'objects_without_matching_reference_type_and_size':dict(unverified)}
            print(name,'total',total,'outside_objects',total-object_total,'top',types.most_common(6),flush=True)
    out={'base_sha256':BASE,'reference_sha256':REFERENCE,'assets':results,
         'limitations':'Nonzero bytes do not measure intellectual property, semantic completeness, or distribution rights. Only five serialized assets are counted; metadata and other APK data are outside this report.'}
    with a.report.open('x') as f:json.dump(out,f,indent=2);f.write('\n')
if __name__=='__main__':main()
