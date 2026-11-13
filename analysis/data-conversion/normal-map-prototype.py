#!/usr/bin/env python3
"""Experimental DXT5nm to ETC1 normal-map conversion for two local textures.

PC alpha and green carry X/Y; reconstructed Z and ETC compression are lossy.
The template still contains other game data. Not a release artifact.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET='5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
PAIRS=((802,1044,'Tiles1_Normal'),(870,764,'Tiles4_Normal'))
def digest(data):return hashlib.sha256(data).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported file: {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
if a.action=='template':require(a.input_dir/'sharedassets1.assets',TARGET)
else:
 manifest=json.loads((a.input_dir/'manifest.json').read_text())
 if manifest.get('version')!=1 or manifest.get('target_sha256')!=TARGET or len(manifest.get('textures',[]))!=len(PAIRS):raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',manifest['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects}
lut=bytes(round((max(0,1-(x/127.5-1)**2-(y/127.5-1)**2)**0.5)*127.5+127.5) for x in range(256) for y in range(256))
records=[]
for tid,sid,name in PAIRS:
 if tid not in targets or sid not in sources or targets[tid].type.name!='Texture2D' or sources[sid].type.name!='Texture2D':raise ValueError('missing texture')
 dst=targets[tid].read();src=sources[sid].read();attrs=('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
 if dst.m_Name!=name or any(getattr(dst,k)!=getattr(src,k) for k in attrs) or src.m_TextureFormat!=12 or dst.m_TextureFormat!=34 or dst.m_ImageCount!=1:raise ValueError('unexpected normal map metadata')
 raw=src.get_image_data();old=dst.image_data;expected=sum(((max(1,src.m_Width>>i)+3)//4)*((max(1,src.m_Height>>i)+3)//4)*16 for i in range(src.m_MipCount))
 if len(raw)!=expected or not old:raise ValueError('bad source payload')
 rec={'target_id':tid,'source_id':sid,'name':name,'size':len(old),'source_sha256':digest(raw)}
 if a.action=='template':dst.image_data=bytes(len(old));records.append(rec)
 else:
  if next((r for r in manifest['textures'] if r['target_id']==tid),None)!=rec or old!=bytes(rec['size']):raise ValueError('source or template mismatch')
  parts=[];offset=0
  for level in range(dst.m_MipCount):
   w,h=max(1,dst.m_Width>>level),max(1,dst.m_Height>>level);size=((w+3)//4)*((h+3)//4)*16
   im=C.parse_image_data(raw[offset:offset+size],w,h,12,src.object_reader.version,src.object_reader.platform).convert('RGBA')
   x=im.getchannel('A');y=im.getchannel('G');z=Image.frombytes('L',(w,h),bytes(lut[(i<<8)|j] for i,j in zip(x.tobytes(),y.tobytes())))
   normal=Image.merge('RGBA',(x,y,z,Image.new('L',(w,h),255)))
   enc,fmt=C.image_to_texture2d(normal,34,dst.object_reader.platform,dst.m_PlatformBlob)
   if int(fmt)!=34:raise ValueError('unexpected ETC output format')
   parts.append(enc);offset+=size
  result=b''.join(parts)
  if offset!=len(raw) or len(result)!=rec['size']:raise ValueError('encoded mip size mismatch')
  dst.image_data=result;dst.m_CompleteImageSize=len(result)
 dst.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 if a.action=='template':(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'textures':records},indent=2)+'\n')
 print(a.action,len(PAIRS),'normal maps; sha256',sha(out));print('Local research only; not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
