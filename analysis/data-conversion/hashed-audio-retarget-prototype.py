#!/usr/bin/env python3
"""Test 63 Steam audio streams in Android hashed clip files.

These are unvalidated candidates, not proof of identity or redistribution
rights. Inputs and outputs contain game data and are not for distribution.
"""
import hashlib,json,shutil,sys,zipfile
from pathlib import Path
import UnityPy
PINS={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.resource':'28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'}
OBB='bada4b92f779ed21d3288f17d14e9821c64a617750ddf7b64dd7fc3cf261cd60';INVENTORY='edfeb7a5b75bd70c9d5a0ae1fc6b6606e7ca89eec989d3d121198544b0dc1ba0'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError('incompatible input '+str(p))
if len(sys.argv)!=5:raise SystemExit('usage: hashed-audio-retarget-prototype.py PC_DATA OBB INVENTORY OUTPUT_DIR')
pc,obb,inventory,out=map(Path,sys.argv[1:])
if out.exists() or not out.parent.is_dir():raise SystemExit('output must not exist and parent must exist')
for name,h in PINS.items():require(pc/name,h)
require(obb,OBB);require(inventory,INVENTORY)
records=json.loads(inventory.read_text())['results'];byid={o.path_id:o for o in UnityPy.load(str(pc/'sharedassets1.assets')).objects};attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex');extra={'Hit1_0':2110,'Hit2_0':2263}
if len(records)!=63 or len({r['file'] for r in records})!=63:raise ValueError('unexpected records')
out.mkdir();report=[]
try:
 with zipfile.ZipFile(obb) as z,(pc/'sharedassets1.resource').open('rb') as audio:
  for r in records:
   if len(r['matches'])==1:
    candidate=r['matches'][0]
    if candidate['source_file']!='sharedassets1.assets' or candidate['resource']!='sharedassets1.resource':raise ValueError('unexpected source')
    id=candidate['source_id'];source_name=r['name']
   elif not r['matches'] and r['name'] in extra:id=extra[r['name']];source_name=r['name'][:-2]
   else:raise ValueError('ambiguous source')
   s=byid[id].read();raw=z.read('assets/bin/Data/'+r['file']);env=UnityPy.load(raw);objects=list(env.objects)
   if len(objects)!=1 or objects[0].type.name!='AudioClip':raise ValueError('unexpected object')
   d=objects[0].read();res=d.m_Resource;sr=s.m_Resource
   if s.m_Name!=source_name or d.m_Name!=r['name'] or any(getattr(d,k,None)!=getattr(s,k,None) for k in attrs) or sr.m_Source!='sharedassets1.resource' or sr.m_Size<=0 or sr.m_Offset<0:raise ValueError('audio metadata mismatch')
   old=z.read('assets/bin/Data/'+res.m_Source)
   if res.m_Source!=r['file']+'.resource' or res.m_Offset!=0 or res.m_Size!=len(old) or hashlib.sha256(old).hexdigest()!=r['sha256']:raise ValueError('unexpected target resource')
   audio.seek(sr.m_Offset);payload=audio.read(sr.m_Size)
   if len(payload)!=sr.m_Size or not payload.startswith(b'FSB5'):raise ValueError('invalid Steam audio stream')
   res.m_Offset=0;res.m_Size=len(payload);d.save();encoded=env.file.save();check=UnityPy.load(encoded).objects[0].read()
   if check.m_Name!=d.m_Name or check.m_Resource.m_Source!=res.m_Source or check.m_Resource.m_Offset!=0 or check.m_Resource.m_Size!=len(payload) or any(getattr(check,k,None)!=getattr(d,k,None) for k in attrs):raise ValueError('serialized audio metadata mismatch')
   (out/r['file']).write_bytes(encoded);(out/res.m_Source).write_bytes(payload)
   report.append({'file':r['file'],'name':r['name'],'source_id':id,'old_size':len(old),'new_size':len(payload),'asset_sha256':hashlib.sha256(encoded).hexdigest(),'resource_sha256':hashlib.sha256(payload).hexdigest()})
 (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
 print('retargeted',len(report),'audio streams; bytes added',sum(x['new_size']-x['old_size'] for x in report),'not redistributable')
except Exception:shutil.rmtree(out);raise
