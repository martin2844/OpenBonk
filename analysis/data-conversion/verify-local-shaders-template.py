#!/usr/bin/env python3
"""Independently verify blank and restored local local Shader bodies."""
import hashlib,json
from pathlib import Path
import UnityPy
root=Path(__file__).resolve().parents[2]/'build/android'
original=(root/'fog-shader-template/sharedassets1.assets').read_bytes();blank=(root/'local-shader-template/sharedassets1.assets').read_bytes();restored=(root/'local-shader-restored/sharedassets1.assets').read_bytes();m=json.loads((root/'local-shader-template/manifest.json').read_text())
if original!=restored or len(original)!=len(blank):raise ValueError('restoration differs')
objects={o.path_id:o for o in UnityPy.load(original).objects};ranges=[]
for row in m['objects']:
 o=objects[row['target_id']];start=o.byte_start;end=start+o.byte_size
 if o.type.name!='Shader' or end-start!=row['size'] or hashlib.sha256(original[start:end]).hexdigest()!=row['sha256']:raise ValueError('unexpected shader')
 ranges.append((start,end))
ranges.sort();cursor=0;nonzero=0
for start,end in ranges:
 if start<cursor or original[cursor:start]!=blank[cursor:start] or any(blank[start:end]):raise ValueError('unexpected change')
 nonzero+=sum(x!=0 for x in original[start:end]);cursor=end
if original[cursor:]!=blank[cursor:] or nonzero!=m['nonzero_bytes']:raise ValueError('unexpected trailing bytes or count')
report={'objects':len(ranges),'selected_bytes':sum(e-s for s,e in ranges),'nonzero_bytes_removed':nonzero,'restored_sha256':hashlib.sha256(restored).hexdigest()}
with (root/'local-shader-verification.json').open('x') as f:json.dump(report,f,indent=2);f.write('\n')
print(json.dumps(report))
