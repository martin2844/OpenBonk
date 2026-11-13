#!/usr/bin/env python3
"""Rebuild specifically reviewed renamed/multimatch textures for one PC build.

This is experimental: visual similarity is not proof of identity, ETC
encoding is lossy, and the template retains other game data. Not for release.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';TARGET='971f7f6e3ddee0dbbfaf88f82d26d6cd464b0801ebb794956a71b61898ab3a1f'
PAIRS=((448,1129),(457,480),(505,1112),(533,1025),(544,687),(558,1046),(585,516),(657,993),(730,1082),(801,1079),(827,597),(831,916),(879,689),(920,1214),(933,1209),(956,590),(967,872),(1006,1053),(1022,1334),(1030,1148),(1053,1301),(1098,686),(1147,818),(1164,866),(1251,670),(1255,1242))
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported input {p}')
def block_size(fmt,w,h):return w*h if fmt==1 else w*h*4 if fmt==4 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
if a.action=='template':require(a.input_dir/'sharedassets1.assets',TARGET)
else:
 m=json.loads((a.input_dir/'matched.json').read_text())
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET or [(r['target_id'],r['source_id']) for r in m['textures']]!=list(PAIRS):raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects};records=[]
for tid,sid in PAIRS:
 if tid not in targets or sid not in sources or targets[tid].type.name!='Texture2D' or sources[sid].type.name!='Texture2D':raise ValueError('texture missing')
 d=targets[tid].read();s=sources[sid].read();attrs=('m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
 if any(getattr(d,k)!=getattr(s,k) for k in attrs) or d.m_ImageCount!=1 or d.m_TextureFormat not in (45,47) or s.m_TextureFormat not in (1,4,10,12) or 'normal' in d.m_Name.lower():raise ValueError(f'unsupported metadata {tid}')
 raw=s.get_image_data();old=d.image_data;expected=sum(block_size(s.m_TextureFormat,max(1,s.m_Width>>i),max(1,s.m_Height>>i)) for i in range(s.m_MipCount))
 if len(raw)!=expected or not old:raise ValueError(f'unexpected source size {tid}')
 rec={'target_id':tid,'source_id':sid,'target_name':d.m_Name,'source_name':s.m_Name,'source_format':s.m_TextureFormat,'target_format':d.m_TextureFormat,'source_sha256':digest(raw),'size':len(old)}
 if a.action=='template':d.image_data=bytes(len(old));records.append(rec)
 else:
  if next((x for x in m['textures'] if x['target_id']==tid),None)!=rec or old!=bytes(len(old)):raise ValueError('source/template mismatch')
  chunks=[];offset=0
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);h=max(1,d.m_Height>>level);size=block_size(s.m_TextureFormat,w,h)
   image=C.parse_image_data(raw[offset:offset+size],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform)
   data,fmt=C.image_to_texture2d(image,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(fmt)!=d.m_TextureFormat:raise ValueError('encoded format mismatch')
   chunks.append(data);offset+=size
  data=b''.join(chunks)
  if offset!=len(raw) or len(data)!=len(old):raise ValueError(f'encoded mip mismatch {tid}')
  d.image_data=data;d.m_CompleteImageSize=len(data)
 d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 if a.action=='template':(a.output_dir/'matched.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'textures':records},indent=2)+'\n')
 print(a.action,len(PAIRS),'textures; SHA256',sha(out));print('Other game data remains; not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
