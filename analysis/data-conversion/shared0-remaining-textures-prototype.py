#!/usr/bin/env python3
"""Reconstruct two remaining sharedassets0 textures from a pinned PC build.

A lossy, local-only experiment. The template still contains other game data.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
WINDOWS_PC='772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d'
STREAM='374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b'
BASE='a36b7152ad38ba4634eba085af73c01e5366a3168f7a0a0f4e86cd09e0250203'
PAIRS=[(55,77,'Fox_Default',256,9,4,43704),(64,74,'Emission',128,8,10,10936)]
def digest(x):return hashlib.sha256(x).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=['template','restore']);p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path)
p.add_argument('--windows-source',action='store_true',help='use pinned Windows data for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets0.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets0.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['textures']
 if m['version']!=1 or m['base_sha256']!=BASE or len(records)!=2:raise ValueError('invalid manifest')
 require(a.input_asset,m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets0.assets'));env=UnityPy.load(str(a.input_asset));sources={x.path_id:x for x in pc.objects};targets={x.path_id:x for x in env.objects};out_records=[]
for idx,(tid,sid,name,width,mips,fmt,expected_size) in enumerate(PAIRS):
 so=sources[sid];to=targets[tid]
 if so.type.name!='Texture2D' or to.type.name!='Texture2D':raise ValueError('missing texture')
 s=so.read();d=to.read()
 if (s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat,s.m_ImageCount)!=(name,width,width,mips,fmt,1) or (d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_TextureFormat,d.m_ImageCount)!=(name,width,width,mips,45,1) or s.m_ColorSpace!=d.m_ColorSpace:raise ValueError('texture metadata mismatch')
 raw=s.get_image_data();old=d.image_data
 if len(old)!=expected_size:raise ValueError('invalid target length')
 rec={'target_id':tid,'source_id':sid,'name':name,'source_sha256':digest(raw),'target_size':len(old)}
 if a.action=='template':
  if not any(old):raise ValueError('texture already empty')
  d.image_data=bytes(len(old));out_records.append(rec)
 else:
  if records[idx]!=rec or any(old):raise ValueError('source or template mismatch')
  pieces=[];offset=0
  for level in range(mips):
   w=max(1,width>>level);n=w*w*4 if fmt==4 else ((w+3)//4)**2*8
   im=C.parse_image_data(raw[offset:offset+n],w,w,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA')
   data,actual=C.image_to_texture2d(im,45,d.object_reader.platform,d.m_PlatformBlob)
   if int(actual)!=45:raise ValueError('unexpected encoded format')
   pieces.append(data);offset+=n
  out=b''.join(pieces)
  if offset!=len(raw) or len(out)!=len(old):raise ValueError('texture mip data size mismatch')
  d.image_data=out
 d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets0.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'textures-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'textures':out_records},indent=2)+'\n')
 print(a.action,len(PAIRS),'textures; asset SHA256',h,'; remaining game data, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
