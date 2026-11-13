#!/usr/bin/env python3
"""Compare candidate scene component bodies after fixed reference prefixes.

Exact bytes after the prefix allow bit-identical reconstruction when preserving
that prefix, but do not establish semantic equivalence or distribution rights.
"""
import collections,hashlib,json,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3]);types={'MonoBehaviour':28,'RectTransform':12,'Transform':12,'MeshRenderer':12,'MeshFilter':12,'ParticleSystemRenderer':12,'AudioSource':12,'CanvasRenderer':12,'MeshCollider':12,'BoxCollider':12,'SphereCollider':12,'Animator':12,'Canvas':12};results=[];errors=[]
if out.exists():raise ValueError('output exists')
with zipfile.ZipFile(obb) as z:
 for scene in ('level1','level2','level4'):
  pre='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in z.namelist() if n.startswith(pre)),key=lambda n:int(n[len(pre):]));target=UnityPy.load(b''.join(z.read(n) for n in parts));source=UnityPy.load(str(pc/scene));index=collections.defaultdict(list)
  for o in source.objects:
   t=o.type.name
   if t not in types:continue
   try:
    raw=o.get_raw_data();n=types[t]
    if len(raw)>n:index[(t,len(raw),hashlib.sha256(raw[n:]).digest())].append(o.path_id)
   except Exception as e:errors.append(['pc',scene,o.path_id,str(e)[:100]])
  summary=collections.defaultdict(lambda:{'total':0,'bytes':0,'matched':0,'matched_body_bytes':0,'ambiguous':0});rows=[]
  for o in target.objects:
   t=o.type.name
   if t not in types:continue
   try:
    raw=o.get_raw_data();n=types[t];r=summary[t];r['total']+=1;r['bytes']+=len(raw)
    matches=index[(t,len(raw),hashlib.sha256(raw[n:]).digest())] if len(raw)>n else []
    if matches:r['matched']+=1;r['matched_body_bytes']+=len(raw)-n;r['ambiguous']+=len(matches)>1;rows.append({'type':t,'target_id':o.path_id,'prefix':n,'size':len(raw),'target_sha256':hashlib.sha256(raw).hexdigest(),'source_ids':matches})
   except Exception as e:errors.append(['android',scene,o.path_id,str(e)[:100]])
  results.append({'scene':scene,'summary':dict(summary),'rows':rows});print(scene,'matched',len(rows),'body_bytes',sum(x['matched_body_bytes'] for x in summary.values()),flush=True)
out.write_text(json.dumps({'scenes':results,'errors':errors},indent=2)+'\n');print('errors',len(errors))
