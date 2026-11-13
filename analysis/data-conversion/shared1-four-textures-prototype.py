#!/usr/bin/env python3
"""Experimental conversion of four remaining textures from one pinned PC build.

Two mappings are visually ambiguous, and all conversions are lossy. The
remaining template contains other game data and is not for distribution.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';BASE='aec2fa3f6c82363cad4b821aa8641c31d0d357157611ed4e5d17704f54f760b6'
WINDOWS_PC='64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
PAIRS=[(872,621),(1072,963),(1171,1154),(1188,728)]
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--windows-source',action='store_true');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['textures']
 if m['version']!=1 or m['base_sha256']!=BASE or [(x['target_id'],x['source_id']) for x in records]!=PAIRS:raise ValueError('invalid manifest')
 require(a.input_asset,m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_asset));src={x.path_id:x for x in pc.objects};dst={x.path_id:x for x in env.objects};out_records=[]
lut=bytes(round((max(0,1-(x/127.5-1)**2-(y/127.5-1)**2)**.5)*127.5+127.5) for x in range(256) for y in range(256)) if a.action=='restore' else None
for i,(tid,sid) in enumerate(PAIRS):
 if src[sid].type.name!='Texture2D' or dst[tid].type.name!='Texture2D':raise ValueError('texture missing')
 s=src[sid].read();d=dst[tid].read();raw=s.get_image_data();old=d.image_data
 if (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace)!=(d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace) or s.m_ImageCount!=1 or (s.m_TextureFormat,d.m_TextureFormat) not in ((10,45),(12,34),(3,45)) or not old:raise ValueError('texture metadata mismatch')
 rec={'target_id':tid,'source_id':sid,'name':d.m_Name,'source_name':s.m_Name,'source_sha256':digest(raw),'target_size':len(old)}
 if a.action=='template':
  if not any(old):raise ValueError('texture already empty')
  d.image_data=bytes(len(old));out_records.append(rec)
 else:
  if records[i]!=rec or any(old):raise ValueError('texture/template mismatch')
  pieces=[];offset=0
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);h=max(1,d.m_Height>>level);fmt=s.m_TextureFormat
   size=w*h*3 if fmt==3 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
   im=C.parse_image_data(raw[offset:offset+size],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA')
   if tid==1072:
    x=im.getchannel('A');y=im.getchannel('G');z=Image.frombytes('L',(w,h),bytes(lut[(i<<8)|j] for i,j in zip(x.tobytes(),y.tobytes())));im=Image.merge('RGBA',(x,y,z,Image.new('L',(w,h),255)))
   encoded,actual=C.image_to_texture2d(im,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(actual)!=d.m_TextureFormat:raise ValueError('encoded format mismatch')
   pieces.append(encoded);offset+=size
  out=b''.join(pieces)
  if offset!=len(raw) or len(out)!=len(old):raise ValueError('mip size mismatch')
  d.image_data=out
 d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'textures-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'textures':out_records},indent=2)+'\n')
 print(a.action,len(PAIRS),'textures; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
