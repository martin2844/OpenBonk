#!/usr/bin/env python3
"""Identify PC audio candidates for separate Android audio resources.

Matching names and metadata do not establish equivalence or ownership. Audio
streams are inspected as bytes; this script does not assess decoded sound.
"""
import collections,hashlib,json,re,sys,zipfile
from pathlib import Path
import UnityPy
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3])
if out.exists():raise SystemExit('output exists')
attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex');sources=collections.defaultdict(list);envs=[];errors=[]
for f in ('resources.assets','sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','level1','level2','level4'):
 env=UnityPy.load(str(pc/f));envs.append(env)
 for obj in env.objects:
  if obj.type.name!='AudioClip':continue
  try:
   x=obj.read();r=x.m_Resource
   if r and r.m_Source and r.m_Size:
    name=Path(r.m_Source).name;path=pc/name
    if path.is_file() and 0<=r.m_Offset and r.m_Offset+r.m_Size<=path.stat().st_size:
     sources[x.m_Name].append((f,obj.path_id,x,path,r))
  except Exception as e:errors.append([f,obj.path_id,str(e)])
results=[]
with zipfile.ZipFile(obb) as z:
 for name in z.namelist():
  if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',name):continue
  objs=list(UnityPy.load(z.read(name)).objects)
  if len(objs)!=1 or objs[0].type.name!='AudioClip':continue
  x=objs[0].read();r=x.m_Resource;resource='assets/bin/Data/'+r.m_Source
  if z.namelist().count(resource)!=1:raise ValueError('missing Android resource')
  data=z.read(resource)
  if r.m_Offset<0 or r.m_Size<=0 or r.m_Offset+r.m_Size>len(data):raise ValueError('invalid Android resource range')
  clip=data[r.m_Offset:r.m_Offset+r.m_Size];matches=[]
  for f,id,s,path,sr in sources[x.m_Name]:
   if any(getattr(x,k,None)!=getattr(s,k,None) for k in attrs):continue
   with path.open('rb') as stream:stream.seek(sr.m_Offset);payload=stream.read(sr.m_Size)
   matches.append({'source_file':f,'source_id':id,'resource':path.name,'offset':sr.m_Offset,'size':sr.m_Size,'sha256':hashlib.sha256(payload).hexdigest(),'prefix':payload[:8].hex(),'identical':clip==payload})
  results.append({'file':Path(name).name,'name':x.m_Name,'resource':r.m_Source,'offset':r.m_Offset,'size':r.m_Size,'resource_size':len(data),'sha256':hashlib.sha256(clip).hexdigest(),'prefix':clip[:8].hex(),'matches':matches})
if len(results)!=63:raise ValueError('unexpected audio clip count')
out.write_text(json.dumps({'results':results,'errors':errors},indent=2)+'\n')
c=collections.Counter('unique' if len(r['matches'])==1 else 'multiple' if r['matches'] else 'unmatched' for r in results)
print('counts',dict(c),'exact',sum(any(x['identical'] for x in r['matches']) for r in results),'errors',len(errors))
for r in results:print(r['file'],r['name'],r['size'],len(r['matches']),[(x['source_file'],x['source_id'],x['size'],x['identical']) for x in r['matches']])
