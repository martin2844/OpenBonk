#!/usr/bin/env python3
"""Find PC name/dimension/format candidates for standalone hashed textures.

A name and dimensional match does not prove visual or semantic identity,
provenance or rights. Texture contents are not compared by this inventory.
"""
import argparse,collections,re,zipfile
from pathlib import Path
import UnityPy
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('obb',type=Path);a=p.parse_args();index=collections.defaultdict(list);envs={};errors=[]
for f in ('sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','level1','level2','level4'):
 env=UnityPy.load(str(a.pc_data/f));envs[f]=env
 for o in env.objects:
  if o.type.name!='Texture2D':continue
  try:
   d=o.read();index[(d.m_Name,d.m_Width,d.m_Height)].append((f,o.path_id,d.m_TextureFormat,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace))
  except Exception as x:errors.append((f,o.path_id,type(x).__name__,str(x)[:100]))
counts=collections.Counter();sizes=collections.Counter();formats=collections.Counter()
with zipfile.ZipFile(a.obb) as z:
 for n in z.namelist():
  if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',n):continue
  try:
   data=z.read(n);env=UnityPy.load(data);objs=list(env.objects)
   if len(objs)!=1 or objs[0].type.name!='Texture2D':continue
   d=objs[0].read();matches=index[(d.m_Name,d.m_Width,d.m_Height)];compatible=[c for c in matches if (c[3],c[4],c[5])==(d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)]
   kind='unique' if len(compatible)==1 else 'ambiguous' if compatible else 'unmatched';counts[kind]+=1;sizes[kind]+=len(d.image_data);formats[(d.m_TextureFormat,tuple(sorted(set(x[2] for x in compatible))))]+=1
   print('TEXTURE',Path(n).name,repr(d.m_Name),d.m_Width,d.m_Height,'fmt',d.m_TextureFormat,'mips',d.m_MipCount,'bytes',len(d.image_data),'stream',bool(d.m_StreamData and d.m_StreamData.path),kind,'candidates',compatible[:6])
  except Exception as x:errors.append((n,type(x).__name__,str(x)[:100]))
print('COUNTS',dict(counts),'PAYLOAD_BYTES',dict(sizes),'FORMATS',dict(formats),'ERRORS',len(errors))
for e in errors[:30]:print('ERROR',e)
