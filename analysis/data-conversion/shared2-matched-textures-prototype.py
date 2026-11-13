#!/usr/bin/env python3
"""Locally reconstruct 72 name-matched textures in a pinned sharedassets2.

Matches and ETC conversions require visual verification; other game data
remains in the template. This experiment is not a distributable package.
"""
import argparse,collections,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da';STREAM='e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84';BASE='fd30a5d9c3713a359bc97da4f13e5907aa4c69b5f82dfef1642c2ac275dfc9df'
WINDOWS_PC='cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'
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
require(a.pc_data/'sharedassets2.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets2.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['textures']
 if m['version']!=1 or m['base_sha256']!=BASE or len(records)!=72 or len({r['target_id'] for r in records})!=72:raise ValueError('invalid manifest')
 require(a.input_asset,m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets2.assets'));env=UnityPy.load(str(a.input_asset));index=collections.defaultdict(list)
for o in pc.objects:
 if o.type.name=='Texture2D':
  t=o.read();index[(t.m_Name,t.m_Width,t.m_Height)].append((o.path_id,t))
out_records=[]
for o in env.objects:
 # Chest-open pixels differ visibly from the PC candidate; keep the Android
 # payload pending investigation rather than replacing it with a poor match.
 if o.type.name!='Texture2D' or o.path_id in (121,167,179):continue
 d=o.read();matches=index[(d.m_Name,d.m_Width,d.m_Height)]
 if len(matches)!=1:continue
 sid,s=matches[0]
 if (s.m_ColorSpace,s.m_ImageCount,s.m_MipCount)!=(d.m_ColorSpace,d.m_ImageCount,d.m_MipCount) or s.m_ImageCount!=1 or s.m_TextureFormat not in (3,4,10,12,25) or d.m_TextureFormat not in (4,45,47):raise ValueError('texture metadata mismatch')
 raw=s.get_image_data();old=d.image_data
 if not old or d.m_CompleteImageSize!=len(old):raise ValueError('invalid target data')
 rec={'target_id':o.path_id,'source_id':sid,'name':d.m_Name,'source_sha256':digest(raw),'source_format':s.m_TextureFormat,'target_format':d.m_TextureFormat,'target_size':len(old)}
 if a.action=='template':
  if not any(old):raise ValueError('texture already empty')
  d.image_data=bytes(len(old));out_records.append(rec)
 else:
  if next((x for x in records if x['target_id']==o.path_id),None)!=rec or any(old):raise ValueError('source/template mismatch')
  chunks=[];off=0
  for lv in range(d.m_MipCount):
   w=max(1,d.m_Width>>lv);h=max(1,d.m_Height>>lv);fmt=s.m_TextureFormat
   n=w*h*(3 if fmt==3 else 4) if fmt in (3,4) else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
   im=C.parse_image_data(raw[off:off+n],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA')
   enc,actual=C.image_to_texture2d(im,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(actual)!=d.m_TextureFormat:raise ValueError('encoded format mismatch')
   chunks.append(enc);off+=n
  value=b''.join(chunks)
  if off!=len(raw) or len(value)!=len(old):raise ValueError('mip size mismatch')
  d.image_data=value;out_records.append(rec)
 d.save()
if len(out_records)!=72 or len({x['target_id'] for x in out_records})!=72:raise ValueError('unexpected texture count')
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets2.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'textures-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'textures':out_records},indent=2)+'\n')
 print(a.action,len(out_records),'textures; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
