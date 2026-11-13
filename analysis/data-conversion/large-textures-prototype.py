#!/usr/bin/env python3
"""Reconstruct a DXT5 sprite and BC7 shader texture for a pinned PC build.

The Unity template still contains other original content. Local research only.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';TARGET='5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
PAIRS=((875,1004,'sprite_0091',12,47),(1141,1349,'2StepLerp',25,4))
def digest(data):return hashlib.sha256(data).hexdigest()
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
 m=json.loads((a.input_dir/'manifest.json').read_text())
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET or len(m.get('textures',[]))!=len(PAIRS):raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));srcs={o.path_id:o for o in pc.objects};dsts={o.path_id:o for o in env.objects};records=[]
for tid,sid,name,sfmt,dfmt in PAIRS:
 if tid not in dsts or sid not in srcs or dsts[tid].type.name!='Texture2D' or srcs[sid].type.name!='Texture2D':raise ValueError('texture missing')
 dst=dsts[tid].read();src=srcs[sid].read();attrs=('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
 if dst.m_Name!=name or any(getattr(dst,k)!=getattr(src,k) for k in attrs) or src.m_TextureFormat!=sfmt or dst.m_TextureFormat!=dfmt or dst.m_ImageCount!=1:raise ValueError('metadata mismatch')
 raw=src.get_image_data();expected=sum(((max(1,src.m_Width>>i)+3)//4)*((max(1,src.m_Height>>i)+3)//4)*16 for i in range(src.m_MipCount))
 if len(raw)!=expected or not dst.image_data:raise ValueError('source payload size mismatch')
 if tid==875 and (srcs[1309].read().get_image_data()!=raw or srcs[1309].read().m_Name!=name):raise ValueError('sprite duplicates differ')
 rec={'target_id':tid,'source_id':sid,'name':name,'size':len(dst.image_data),'source_sha256':digest(raw)}
 if a.action=='template':dst.image_data=bytes(rec['size']);records.append(rec)
 else:
  if next((r for r in m['textures'] if r['target_id']==tid),None)!=rec or dst.image_data!=bytes(rec['size']):raise ValueError('source or target payload mismatch')
  parts=[];offset=0
  for level in range(dst.m_MipCount):
   w=max(1,dst.m_Width>>level);h=max(1,dst.m_Height>>level);size=((w+3)//4)*((h+3)//4)*16
   image=C.parse_image_data(raw[offset:offset+size],w,h,sfmt,src.object_reader.version,src.object_reader.platform)
   enc,fmt=C.image_to_texture2d(image,dfmt,dst.object_reader.platform,dst.m_PlatformBlob)
   if int(fmt)!=dfmt:raise ValueError('unexpected encoded format')
   parts.append(enc);offset+=size
  out=b''.join(parts)
  if offset!=len(raw) or len(out)!=rec['size']:raise ValueError('encoded mip size mismatch')
  dst.image_data=out;dst.m_CompleteImageSize=len(out)
 dst.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 if a.action=='template':(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'textures':records},indent=2)+'\n')
 print(a.action,len(PAIRS),'textures; SHA256',sha(out));print('Other game data remains; not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
