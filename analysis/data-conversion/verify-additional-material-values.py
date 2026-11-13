#!/usr/bin/env python3
"""Independently verify local material value templates and restorations."""
import hashlib,json
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[2]/'build/android';report={}
for i in (0,2,4):
 name=f'sharedassets{i}.assets';original=(root/'additional-material-original'/name).read_bytes()
 blank=(root/f'additional-material-template-{i}'/name).read_bytes();restored=(root/f'additional-material-restored-{i}'/name).read_bytes()
 manifest=json.loads((root/f'additional-material-template-{i}'/'manifest.json').read_text())
 if original!=restored or len(original)!=len(blank):raise ValueError('restoration differs')
 objects={o.path_id:o for o in UnityPy.load(original).objects};spans=[]
 for row in manifest['objects']:
  o=objects[row['target_id']]
  if o.type.name!='Material':raise ValueError('wrong type')
  start=o.byte_start+o.byte_size-row['length'];end=o.byte_start+o.byte_size
  if start<o.byte_start or hashlib.sha256(original[start:end]).hexdigest()!=row['value_sha256']:raise ValueError('invalid range')
  spans.append((start,end))
 spans.sort();cursor=0;changed=0
 for start,end in spans:
  if start<cursor or original[cursor:start]!=blank[cursor:start] or any(blank[start:end]):raise ValueError('unexpected change')
  changed+=sum(x!=0 for x in original[start:end]);cursor=end
 if original[cursor:]!=blank[cursor:] or changed!=manifest['nonzero_bytes']:raise ValueError('trailing data or count mismatch')
 report[name]={'objects':len(spans),'selected_bytes':sum(e-s for s,e in spans),'nonzero_bytes_removed':changed,'restored_sha256':hashlib.sha256(restored).hexdigest()}
out=root/'additional-material-verification.json'
with out.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps(report,indent=2))
