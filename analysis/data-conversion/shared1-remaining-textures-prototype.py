#!/usr/bin/env python3
"""Experimentally rebuild 59 visually matched textures from pinned PC assets.

These matches and conversions are lossy and not fully validated in game.
The template and outputs retain other data; they are not distributable.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
WINDOWS_PC='64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
BASE='c94abec605b305c119c321241d1569e62dc0c8cae5fdab29172eb9b53cd72403'
PAIRS=[(436,995),(469,831),(480,1208),(483,756),(498,692),(507,810),(538,1086),(559,746),(560,833),(566,661),(567,1072),(572,755),(579,1115),(608,1207),(619,640),(639,894),(653,1016),(655,1000),(669,1014),(672,967),(676,1417),(687,790),(736,1077),(740,722),(743,1225),(766,718),(771,1286),(788,1023),(800,648),(804,1159),(821,1138),(860,1042),(883,1433),(896,1188),(899,1431),(914,822),(921,1395),(923,960),(929,1187),(934,1141),(961,600),(988,1213),(1011,1002),(1017,1051),(1028,1034),(1057,800),(1076,1021),(1079,622),(1082,882),(1086,896),(1087,563),(1103,887),(1110,1095),(1124,1241),(1125,986),(1128,523),(1148,1414),(1222,935),(1242,974)]
def digest(x):return hashlib.sha256(x).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--four-textures',action='store_true');p.add_argument('--windows-source',action='store_true')
a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['textures']
 if m['version']!=1 or m['base_sha256']!=BASE or [(x['target_id'],x['source_id']) for x in records]!=PAIRS:raise ValueError('invalid manifest')
 require(a.input_asset,'5b8c1b85e390c9ae2577577f19678ddf64da8c3e5d1df1b386a6c7616f5c8cdd' if a.four_textures else m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_asset));src={x.path_id:x for x in pc.objects};dst={x.path_id:x for x in env.objects};out_records=[]
for i,(tid,sid) in enumerate(PAIRS):
 if src[sid].type.name!='Texture2D' or dst[tid].type.name!='Texture2D':raise ValueError('texture missing')
 s=src[sid].read();d=dst[tid].read();raw=s.get_image_data();old=d.image_data
 if (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace)!=(d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace) or s.m_ImageCount!=1 or s.m_TextureFormat not in (1,4,10,12) or d.m_TextureFormat not in (4,45,47) or not old:raise ValueError('texture metadata mismatch')
 rec={'target_id':tid,'source_id':sid,'name':d.m_Name,'source_name':s.m_Name,'source_sha256':digest(raw),'target_size':len(old)}
 if a.action=='template':
  if not any(old):raise ValueError('texture already empty')
  d.image_data=bytes(len(old));out_records.append(rec)
 else:
  if records[i]!=rec or any(old):raise ValueError('texture/template mismatch')
  pieces=[];offset=0
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);h=max(1,d.m_Height>>level);fmt=s.m_TextureFormat
   size=w*h if fmt==1 else w*h*4 if fmt==4 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
   im=C.parse_image_data(raw[offset:offset+size],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA')
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
