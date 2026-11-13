#!/usr/bin/env python3
"""Convert six DXT5 faces to ETC2 in one pinned Graveyard cubemap.

Independent research prototype: compression is lossy and other game content
remains in the template. Inputs and output are not distribution artifacts.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';TARGET='5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
if a.action=='template':require(a.input_dir/'sharedassets1.assets',TARGET)
else:
 m=json.loads((a.input_dir/'cubemap.json').read_text())
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET:raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));srcs={o.path_id:o for o in pc.objects};dsts={o.path_id:o for o in env.objects}
if srcs[2290].type.name!='Cubemap' or dsts[1907].type.name!='Cubemap':raise ValueError('missing cubemap')
s=srcs[2290].read();d=dsts[1907].read();attrs=('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
if any(getattr(s,k)!=getattr(d,k) for k in attrs) or (s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_TextureFormat,d.m_TextureFormat)!=('_Graveyard',512,512,1,6,12,45):raise ValueError('unexpected cubemap metadata')
raw=s.get_image_data();old=d.image_data
if len(raw)!=6*262144 or len(old)!=6*131072 or s.m_CompleteImageSize!=262144 or d.m_CompleteImageSize!=131072:raise ValueError('unexpected face sizes')
source_hash=hashlib.sha256(raw).hexdigest()
if a.action=='template':d.image_data=bytes(len(old))
else:
 if m.get('source_sha256')!=source_hash or old!=bytes(len(old)):raise ValueError('source or template mismatch')
 faces=[]
 for i in range(6):
  im=C.parse_image_data(raw[i*262144:(i+1)*262144],512,512,12,s.object_reader.version,s.object_reader.platform)
  data,fmt=C.image_to_texture2d(im,45,d.object_reader.platform,d.m_PlatformBlob)
  if int(fmt)!=45 or len(data)!=131072:raise ValueError('invalid face encoding')
  faces.append(data)
 d.image_data=b''.join(faces)
d.save();a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 if a.action=='template':(a.output_dir/'cubemap.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'source_sha256':source_hash},indent=2)+'\n')
 print(a.action,'six-face cubemap; SHA256',sha(out));print('Contains other game data; not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
