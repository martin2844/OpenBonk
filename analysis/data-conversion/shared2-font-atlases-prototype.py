#!/usr/bin/env python3
"""Experimentally reconstruct two sharedassets2 font atlases from Steam.

Conversion to ETC2 is lossy. Both the input and template contain other game
data and remain local research artifacts, not distribution packages.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da';STREAM='e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84';BASE='2725e1dfef1bcddfd50959b0e894c3c5d44210c2c17b56a32fe5209aee95d0f8'
WINDOWS_PC='cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'
PAIRS=[(167,110,'PressStart2P-Regular SDF Atlas',1024),(179,188,'AlagardMegaPadding Atlas',2048)]
def digest(x):return hashlib.sha256(x).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--matched-textures',action='store_true');p.add_argument('--windows-source',action='store_true');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets2.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets2.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['textures']
 if m['version']!=1 or m['base_sha256']!=BASE or [(x['target_id'],x['source_id']) for x in records]!=[(x[0],x[1]) for x in PAIRS]:raise ValueError('invalid manifest')
 require(a.input_asset,'865b24b181c2640cc5865b649d2b21a892548d651047b5a9cde084c90ace3145' if a.matched_textures else m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets2.assets'));env=UnityPy.load(str(a.input_asset));src={x.path_id:x for x in pc.objects};dst={x.path_id:x for x in env.objects};out_records=[]
for i,(tid,sid,name,width) in enumerate(PAIRS):
 if src[sid].type.name!='Texture2D' or dst[tid].type.name!='Texture2D':raise ValueError('texture missing')
 s=src[sid].read();d=dst[tid].read();raw=s.get_image_data();old=d.image_data
 if (s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_TextureFormat)!=(name,width,width,1,1,1) or (d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_TextureFormat)!=(name,width,width,1,1,47) or s.m_ColorSpace!=d.m_ColorSpace or len(raw)!=width*width or len(old)!=width*width:raise ValueError('atlas metadata mismatch')
 rec={'target_id':tid,'source_id':sid,'name':name,'source_sha256':digest(raw),'target_size':len(old)}
 if a.action=='template':
  if not any(old):raise ValueError('atlas already empty')
  d.image_data=bytes(len(old));out_records.append(rec)
 else:
  if records[i]!=rec or any(old):raise ValueError('atlas/template mismatch')
  data,actual=C.image_to_texture2d(s.image.convert('RGBA'),47,d.object_reader.platform,d.m_PlatformBlob)
  if int(actual)!=47 or len(data)!=len(old):raise ValueError('encoded format mismatch')
  d.image_data=data
 d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets2.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'atlases-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'textures':out_records},indent=2)+'\n')
 print(a.action,len(PAIRS),'atlases; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
