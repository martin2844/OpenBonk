#!/usr/bin/env python3
"""Build or restore local experimental hashed-audio templates.

The template retains serialized clip metadata and other game-derived content.
None of the inputs or outputs is a distribution package.
"""
import argparse,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
PINS={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.resource':'28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'}
OBB='bada4b92f779ed21d3288f17d14e9821c64a617750ddf7b64dd7fc3cf261cd60';INVENTORY='edfeb7a5b75bd70c9d5a0ae1fc6b6606e7ca89eec989d3d121198544b0dc1ba0';ATTRS=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex');EXTRA={'Hit1_0':2110,'Hit2_0':2263}
def digest(data):return hashlib.sha256(data).hexdigest()
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(path,expected):
 if not path.is_file() or sha(path)!=expected:raise ValueError('incompatible input '+str(path))
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--inventory',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output.exists() or not a.output.parent.is_dir():p.error('output must not exist and parent must exist')
for n,h in PINS.items():require(a.pc_data/n,WINDOWS_PINS[n] if a.windows_source else h)
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects}
if a.action=='template':
 require(a.input,OBB)
 if not a.inventory:p.error('template requires --inventory')
 require(a.inventory,INVENTORY);records=json.loads(a.inventory.read_text())['results']
else:
 m=json.loads((a.input/'manifest.json').read_text());records=m['clips']
 if m['version']!=1 or m['base_obb_sha256']!=OBB:raise ValueError('invalid manifest')
if len(records)!=63 or len({x['file'] for x in records})!=63:raise ValueError('invalid clip count')
a.output.mkdir();result=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  with (a.pc_data/'sharedassets1.resource').open('rb') as audio:
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid file')
    if z:
     if len(r['matches'])==1:
      c=r['matches'][0]
      if c['source_file']!='sharedassets1.assets' or c['resource']!='sharedassets1.resource':raise ValueError('invalid source')
      sid=c['source_id'];source_name=r['name']
     elif not r['matches'] and r['name'] in EXTRA:sid=EXTRA[r['name']];source_name=r['name'][:-2]
     else:raise ValueError('ambiguous source')
     raw=z.read('assets/bin/Data/'+name)
    else:
     sid=r['source_id'];source_name=r['source_name'];require(a.input/name,r['asset_template_sha256']);raw=(a.input/name).read_bytes()
    if sid not in sources or sources[sid].type.name!='AudioClip':raise ValueError('source audio missing')
    s=sources[sid].read();env=UnityPy.load(raw);objects=list(env.objects)
    if len(objects)!=1 or objects[0].type.name!='AudioClip':raise ValueError('invalid audio object')
    d=objects[0].read();target=d.m_Resource;sr=s.m_Resource
    if d.m_Name!=r['name'] or s.m_Name!=source_name or any(getattr(d,k,None)!=getattr(s,k,None) for k in ATTRS) or sr.m_Source!='sharedassets1.resource' or sr.m_Offset<0 or sr.m_Size<=0:raise ValueError('audio metadata mismatch')
    if target.m_Source!=name+'.resource' or target.m_Offset!=0 or target.m_Size<=0:raise ValueError('unexpected Android resource')
    if z:
     old=z.read('assets/bin/Data/'+target.m_Source)
     if len(old)!=target.m_Size or digest(old)!=r['sha256']:raise ValueError('Android audio digest mismatch')
    else:
     if target.m_Size!=r['original_size']:raise ValueError('template stream size mismatch')
    audio.seek(sr.m_Offset);payload=audio.read(sr.m_Size)
    if len(payload)!=sr.m_Size or not payload.startswith(b'FSB5'):raise ValueError('invalid Steam stream')
    target.m_Offset=0;target.m_Size=len(payload);d.save();encoded=env.file.save();check=UnityPy.load(encoded).objects[0].read()
    if check.m_Name!=d.m_Name or check.m_Resource.m_Source!=target.m_Source or check.m_Resource.m_Offset!=0 or check.m_Resource.m_Size!=len(payload) or any(getattr(check,k,None)!=getattr(d,k,None) for k in ATTRS):raise ValueError('invalid serialized result')
    if z:
     rec={'file':name,'name':d.m_Name,'source_id':sid,'source_name':source_name,'original_size':len(old),'original_resource_sha256':digest(old),'asset_template_sha256':digest(raw),'asset_sha256':digest(encoded),'resource_sha256':digest(payload),'resource_size':len(payload)}
     (a.output/name).write_bytes(raw);result.append(rec)
    else:
     if digest(encoded)!=r['asset_sha256'] or digest(payload)!=r['resource_sha256'] or len(payload)!=r['resource_size']:raise ValueError('restored data mismatch')
     (a.output/name).write_bytes(encoded);(a.output/(name+'.resource')).write_bytes(payload);result.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'clips':result},indent=2)+'\n')
 print(a.action,len(result),'audio clips; original bytes',sum(x['original_size'] for x in result),'; other content remains, not redistributable')
except Exception:shutil.rmtree(a.output);raise
