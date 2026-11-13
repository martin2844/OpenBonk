#!/usr/bin/env python3
"""Verify zeroed shader bodies retain their Android dependency references."""
import hashlib,json
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[2]/'build/android'
a=(root/'instancing-shader-template/sharedassets1.assets').read_bytes();b=(root/'instancing-reference-template/sharedassets1.assets').read_bytes();c=(root/'instancing-reference-restored/sharedassets1.assets').read_bytes();m=json.loads((root/'instancing-reference-template/manifest.json').read_text())
if a!=c or len(a)!=len(b):raise ValueError('restored asset differs')
objects={o.path_id:o for o in UnityPy.load(a).objects};ranges=[];references=[]
for row in m['objects']:
 o=objects[row['target_id']];start=o.byte_start;end=start+o.byte_size;rs=start+row['reference_start'];re=start+row['reference_end']
 if o.type.name!='Shader' or hashlib.sha256(a[start:end]).hexdigest()!=row['original_sha256'] or hashlib.sha256(a[rs:re]).hexdigest()!=row['reference_sha256']:raise ValueError('invalid source')
 if a[rs:re]!=b[rs:re]:raise ValueError('reference changed')
 ranges.extend(((start,rs),(re,end)));references.append((rs,re))
ranges.sort();cursor=0;nonzero=0
for start,end in ranges:
 if start<cursor or a[cursor:start]!=b[cursor:start] or any(b[start:end]):raise ValueError('unexpected change')
 nonzero+=sum(x!=0 for x in a[start:end]);cursor=end
if a[cursor:]!=b[cursor:] or nonzero!=m['nonzero_bytes']:raise ValueError('unexpected trailing data')
report={'objects':len(m['objects']),'selected_bytes':sum(e-s for s,e in ranges),'nonzero_bytes_removed':nonzero,'reference_bytes_retained':sum(e-s for s,e in references),'restored_sha256':hashlib.sha256(c).hexdigest()}
with (root/'instancing-reference-verification.json').open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps(report))
