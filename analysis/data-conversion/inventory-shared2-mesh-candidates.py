#!/usr/bin/env python3
"""Compare sharedassets2 mesh buffers to candidate PC serialized assets.

Exact bytes and geometry metadata are evidence of matching buffers only;
other mesh fields, external pointers, provenance and rights are not verified.
"""
import collections,hashlib
from pathlib import Path
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
pc=Path('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data');target=Path('build/android/share-source-experiment/shared2-inventory/android-sharedassets2.assets')
def geometry(m):return (m.m_VertexData.m_VertexCount,tuple((x.stream,x.offset,x.format,x.dimension) for x in m.m_VertexData.m_Channels),m.m_IndexFormat)
def payload(m):
 v=bytes(m.m_VertexData.m_DataSize or b'')
 if not v and m.m_StreamData and m.m_StreamData.path:v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
 return v,bytes(m.m_IndexBuffer or [])
def dig(b):return hashlib.sha256(b).hexdigest()
index=collections.defaultdict(list);errors=[]
for f in ('sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','level1','level2','level4'):
 e=UnityPy.load(str(pc/f))
 for o in e.objects:
  if o.type.name!='Mesh':continue
  try:
   m=o.read();v,i=payload(m)
   if v or i:index[(geometry(m),dig(v),dig(i))].append((f,o.path_id,m.m_Name))
  except Exception as x:errors.append((f,o.path_id,type(x).__name__,str(x)))
e=UnityPy.load(str(target));counts=collections.Counter();size=collections.Counter()
for o in e.objects:
 if o.type.name!='Mesh':continue
 try:
  m=o.read();v,i=payload(m)
  if not v and not i:counts['empty']+=1;continue
  matches=index[(geometry(m),dig(v),dig(i))];kind='unique' if len(matches)==1 else 'ambiguous' if matches else 'unmatched';counts[kind]+=1;size[kind]+=len(v)+len(i)
  print('MESH',o.path_id,repr(m.m_Name),'vertices',len(v),'indices',len(i),'kind',kind,'matches',matches[:4])
 except Exception as x:errors.append(('Android',o.path_id,type(x).__name__,str(x)))
print('COUNTS',dict(counts),'PAYLOAD_BYTES',dict(size),'ERRORS',len(errors))
for x in errors:print('ERROR',x)
