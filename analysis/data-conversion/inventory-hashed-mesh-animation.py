#!/usr/bin/env python3
"""Find exact mesh-buffer and animation-object candidates for OBB hash files.

Names, size and byte-difference counts are investigative evidence only; they
do not prove animation equivalence, provenance or rights.
"""
import argparse,collections,hashlib,re,tempfile,zipfile
from pathlib import Path
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('obb',type=Path);a=p.parse_args()
def geometry(m):return (m.m_VertexData.m_VertexCount,tuple((x.stream,x.offset,x.format,x.dimension) for x in m.m_VertexData.m_Channels),m.m_IndexFormat)
def payload(m):
 v=bytes(m.m_VertexData.m_DataSize or b'')
 if not v and m.m_StreamData and m.m_StreamData.path:v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
 return v,bytes(m.m_IndexBuffer or [])
def dig(x):return hashlib.sha256(x).hexdigest()
meshes=collections.defaultdict(list);anims=collections.defaultdict(list);envs={};errors=[]
for f in ('sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','level1','level2','level4'):
 env=UnityPy.load(str(a.pc_data/f));envs[f]=env
 for o in env.objects:
  if o.type.name not in ('Mesh','AnimationClip'):continue
  try:
   d=o.read()
   if o.type.name=='Mesh':
    v,i=payload(d)
    if v or i:meshes[(geometry(d),dig(v),dig(i))].append((f,o.path_id,d.m_Name))
   else:anims[(d.m_Name,o.byte_size)].append((f,o.path_id,o.get_raw_data()))
  except Exception as ex:errors.append((f,o.path_id,type(ex).__name__,str(ex)[:100]))
counts=collections.Counter();sizes=collections.Counter()
with tempfile.TemporaryDirectory(prefix='openbonk-hash-inventory-') as tmp,zipfile.ZipFile(a.obb) as z:
 names=[n for n in z.namelist() if re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}(?:\.resource)?',n)]
 for n in names:(Path(tmp)/Path(n).name).write_bytes(z.read(n))
 for n in names:
  if n.endswith('.resource'):continue
  try:
   e=UnityPy.load(str(Path(tmp)/Path(n).name));objects=list(e.objects)
   if len(objects)!=1 or objects[0].type.name not in ('Mesh','AnimationClip'):continue
   o=objects[0];d=o.read();typ=o.type.name
   if typ=='Mesh':
    v,i=payload(d)
    if not v and not i:counts['mesh_empty']+=1;continue
    matches=meshes[(geometry(d),dig(v),dig(i))];kind='unique' if len(matches)==1 else 'ambiguous' if matches else 'unmatched';counts['mesh_'+kind]+=1;sizes['mesh_'+kind]+=len(v)+len(i)
    print('MESH',Path(n).name,repr(d.m_Name),'bytes',len(v)+len(i),kind,'matches',matches[:5])
   else:
    data=o.get_raw_data();matches=anims[(d.m_Name,o.byte_size)];dist=[]
    for f,oid,b in matches:dist.append((sum(x!=y for x,y in zip(data,b)),f,oid))
    dist.sort();kind='unique' if len(dist)==1 else 'ambiguous' if dist else 'unmatched';counts['animation_'+kind]+=1;sizes['animation_'+kind]+=len(data)
    print('ANIM',Path(n).name,repr(d.m_Name),'bytes',len(data),kind,'nearest',dist[:5])
  except Exception as ex:errors.append((n,type(ex).__name__,str(ex)[:100]))
print('COUNTS',dict(counts),'PAYLOAD_BYTES',dict(sizes),'ERRORS',len(errors))
for e in errors[:30]:print('ERROR',e)
