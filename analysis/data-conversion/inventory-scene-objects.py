#!/usr/bin/env python3
"""Compare raw serialized object bytes in Android scene files and local PC data.

Exact bytes are not proof of provenance or rights. Names may be nonunique;
results do not establish semantic equivalence or portable references.
"""
import collections,hashlib,json,re,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3])
if out.exists():raise ValueError('output exists')
def object_name(o, typ):
 if typ not in ('GameObject','MonoBehaviour','Mesh','Texture2D','AudioClip','Material','AnimationClip','Shader','LightmapSettings','TextAsset'):return ''
 try:return getattr(o.read(),'m_Name','') or ''
 except Exception:return ''
files=('level0','level1','level2','level3','level4','resources.assets','sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','globalgamemanagers.assets');index=collections.defaultdict(list);byname=collections.defaultdict(list);envs=[];errors=[]
for f in files:
 e=UnityPy.load(str(pc/f));envs.append(e)
 for o in e.objects:
  try:
   raw=o.get_raw_data();typ=o.type.name;h=hashlib.sha256(raw).hexdigest();name=object_name(o,typ)
   index[(typ,len(raw),h)].append((f,o.path_id));
   if name:byname[(typ,name)].append((f,o.path_id))
  except Exception as ex:errors.append([f,o.path_id,type(ex).__name__,str(ex)[:120]])
print('indexed',sum(map(len,index.values())),'objects; errors',len(errors),flush=True);result=[]
with zipfile.ZipFile(obb) as z:
 for scene in ('level0','level1','level2','level3','level4'):
  stem='assets/bin/Data/'+scene;names=[n for n in z.namelist() if n==stem or n.startswith(stem+'.split')]
  if not names:print(scene,'missing');continue
  if stem in names:
   if len(names)!=1:raise ValueError('duplicate scene')
  else:
   names.sort(key=lambda n:int(n.rsplit('split',1)[1]));ids=[int(n.rsplit('split',1)[1]) for n in names]
   if ids!=list(range(len(ids))):raise ValueError('missing scene fragment')
  raw=b''.join(z.read(n) for n in names);e=UnityPy.load(raw);summary=collections.defaultdict(lambda: {'objects':0,'bytes':0,'exact_objects':0,'exact_bytes':0,'same_name_objects':0});examples=[]
  for o in e.objects:
   try:
    data=o.get_raw_data();typ=o.type.name;name=object_name(o,typ);matches=index[(typ,len(data),hashlib.sha256(data).hexdigest())];summary[typ]['objects']+=1;summary[typ]['bytes']+=len(data)
    if matches:summary[typ]['exact_objects']+=1;summary[typ]['exact_bytes']+=len(data)
    if name and byname[(typ,name)]:summary[typ]['same_name_objects']+=1
    if len(examples)<20 and matches and len(data)>2048:examples.append({'type':typ,'name':name,'bytes':len(data),'candidates':matches[:3]})
   except Exception as ex:errors.append([scene,o.path_id,type(ex).__name__,str(ex)[:120]])
  print(scene,'file bytes',len(raw),'objects',sum(x['objects'] for x in summary.values()),'exact bytes',sum(x['exact_bytes'] for x in summary.values()),'errors',len(errors),flush=True)
  result.append({'scene':scene,'file_bytes':len(raw),'summary':dict(summary),'examples':examples})
out.write_text(json.dumps({'results':result,'errors':errors,'note':__doc__},indent=2)+'\n')
