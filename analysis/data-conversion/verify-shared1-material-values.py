#!/usr/bin/env python3
"""Verify the local material template changes only recorded value tails."""
import hashlib,json
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[2]/'build/android'
original=(root/'material-values-original.assets').read_bytes()
blank=(root/'material-values-template/sharedassets1.assets').read_bytes()
restored=(root/'material-values-restored/sharedassets1.assets').read_bytes()
manifest=json.loads((root/'material-values-template/manifest.json').read_text())
if original!=restored or len(original)!=len(blank):raise ValueError('restoration differs')
objects={o.path_id:o for o in UnityPy.load(original).objects};spans=[]
for row in manifest['objects']:
 o=objects[row['target_id']]
 if o.type.name!='Material':raise ValueError('wrong type')
 start=o.byte_start+o.byte_size-row['length'];end=o.byte_start+o.byte_size
 if start<o.byte_start or hashlib.sha256(original[start:end]).hexdigest()!=row['value_sha256']:raise ValueError('bad range')
 spans.append((start,end))
spans.sort();cursor=0;changed=0
for start,end in spans:
 if start<cursor or original[cursor:start]!=blank[cursor:start] or any(blank[start:end]):raise ValueError('unintended change')
 changed+=sum(x!=0 for x in original[start:end]);cursor=end
if original[cursor:]!=blank[cursor:] or changed!=manifest['nonzero_bytes']:raise ValueError('trailing data or count mismatch')
report={'objects':len(spans),'selected_bytes':sum(e-s for s,e in spans),'nonzero_bytes_removed':changed,'restored_sha256':hashlib.sha256(restored).hexdigest()}
out=root/'material-values-verification.json'
with out.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps(report))
