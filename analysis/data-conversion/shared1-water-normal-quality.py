#!/usr/bin/env python3
"""Replace one experimental Android water normal map with RGBA32 from Steam.

The input and output contain original data and are for local diagnostics only.
RGBA32 avoids the normal-map compression error measured on the test emulator.
"""
import argparse,hashlib,os
from pathlib import Path
import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';BASE='cc947601ab5e1d65c06afda412260f36d2d22bc66a818f47ac5b94c7c2227e7b'
WINDOWS_PC='64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_asset',type=Path);p.add_argument('--windows-source',action='store_true',help='use the pinned Windows texture');a=p.parse_args()
if a.output_asset.exists() or not a.output_asset.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM);require(a.input_asset,BASE)
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_asset));src={o.path_id:o for o in pc.objects};dst={o.path_id:o for o in env.objects}
if src[963].type.name!='Texture2D' or dst[1072].type.name!='Texture2D':raise ValueError('texture missing')
s=src[963].read();d=dst[1072].read()
if (s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat,s.m_ColorSpace)!=('IL3DN_WaterNormal',32,32,6,12,0) or (d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_TextureFormat,d.m_ColorSpace)!=('IL3DN_WaterNormal',32,32,6,34,0):raise ValueError('texture metadata mismatch')
raw=s.get_image_data();lut=bytes(round((max(0,1-(x/127.5-1)**2-(y/127.5-1)**2)**.5)*127.5+127.5) for x in range(256) for y in range(256));chunks=[];off=0
for level in range(6):
 w=max(1,32>>level);n=((w+3)//4)**2*16;im=C.parse_image_data(raw[off:off+n],w,w,12,s.object_reader.version,s.object_reader.platform).convert('RGBA');x=im.getchannel('A');y=im.getchannel('G');z=Image.frombytes('L',(w,w),bytes(lut[(i<<8)|j] for i,j in zip(x.tobytes(),y.tobytes())));im=Image.merge('RGBA',(x,y,z,Image.new('L',(w,w),255)))
 data,fmt=C.image_to_texture2d(im,4,d.object_reader.platform,d.m_PlatformBlob)
 if int(fmt)!=4 or len(data)!=w*w*4:raise ValueError('encoded mip mismatch')
 chunks.append(data);off+=n
out=b''.join(chunks)
if off!=len(raw) or len(out)!=5460 or len(d.image_data)!=696:raise ValueError('texture size mismatch')
d.image_data=out;d.m_TextureFormat=4;d.m_CompleteImageSize=len(out);d.save();created=False
try:
 with a.output_asset.open('xb') as f:
  created=True;f.write(env.file.save())
 print('Rebuilt water normal map RGBA32; asset SHA256',sha(a.output_asset),'not for distribution')
except Exception:
 if created:a.output_asset.unlink(missing_ok=True)
 raise
