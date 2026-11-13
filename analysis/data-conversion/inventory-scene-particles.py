#!/usr/bin/env python3
"""Compare scene particle objects by GameObject name and serialized bytes.

Object names and similar bytes do not prove equivalent object references,
behavior, or rights. Empty and ambiguous matches are reported separately.
"""
import collections,hashlib,json,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3]);results=[];errors=[]
if out.exists():raise ValueError('output exists')
def getparticles(env,scene):
 r=[]
 for o in env.objects:
  if o.type.name!='ParticleSystem':continue
  try:
   d=o.read();go=d.m_GameObject.read();r.append((o.path_id,go.m_Name,o.get_raw_data()))
  except Exception as ex:errors.append([scene,o.path_id,str(ex)[:120]])
 return r
with zipfile.ZipFile(obb) as z:
 for scene in ('level1','level2','level4'):
  pre='assets/bin/Data/'+scene+'.split';ns=sorted((n for n in z.namelist() if n.startswith(pre)),key=lambda n:int(n[len(pre):]));a=UnityPy.load(b''.join(z.read(n) for n in ns));p=UnityPy.load(str(pc/scene));src=getparticles(p,'pc-'+scene);index=collections.defaultdict(list)
  for id,name,raw in src:index[(name,len(raw))].append((id,raw))
  rows=[]
  for id,name,raw in getparticles(a,'android-'+scene):
   matches=index[(name,len(raw))];c=[]
   for sid,x in matches:
    n=sum(u!=v for u,v in zip(raw,x));n12=sum(u!=v for u,v in zip(raw[12:],x[12:]));c.append({'id':sid,'different_bytes':n,'different_bytes_after_12':n12,'sha256':hashlib.sha256(x).hexdigest()})
   c.sort(key=lambda x:x['different_bytes']);rows.append({'id':id,'name':name,'size':len(raw),'sha256':hashlib.sha256(raw).hexdigest(),'matches':c})
  results.append({'scene':scene,'pc_particles':len(src),'rows':rows});print(scene,'target',len(rows),'unique',sum(len(x['matches'])==1 for x in rows),'ambiguous',sum(len(x['matches'])>1 for x in rows),'unmatched',sum(not x['matches'] for x in rows),'equal after first 12 bytes',sum(bool(x['matches']) and x['matches'][0]['different_bytes_after_12']==0 for x in rows),flush=True)
out.write_text(json.dumps({'scenes':results,'errors':errors},indent=2)+'\n')
print('errors',len(errors))
