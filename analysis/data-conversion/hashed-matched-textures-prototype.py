#!/usr/bin/env python3
"""Experimental conversion of 53 uniquely named hashed texture candidates.

Name/dimension matches and lossy platform conversions require visual checks.
Templates retain other game data and are not for distribution.
"""
import argparse,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
from UnityPy.export import Texture2DConverter as C
PINS={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'}
OBB='bad91e6c18e64cb90a72b27064f06288fd174ca16b3224f6a45b87e6c33f376a';COMPARISON='886affe779061c77fd857434b19b9ff2e267a8788801448a346c3d11acb17aae'
# Excluded after per-mip comparison showed a mean weighted-channel difference
# above 15/255 in at least one level. These require separate visual validation.
EXCLUDED={'fca03e78624255d468f316208c6b021f','c6f28f3e901c2ee46b56658f5c4bbec8','303c0b4d707421347b1619a563c8983c','6aa2f11be783f754b8a3e798c6fc9e36','6e1eb85bdb67a71479a275bfbd53b659','1159c75bca52b384ab87059f7d02c173','48fc24aa34b0cee42909ffe20f7cff7c','1448d7c22bb37874980b2c17cef7d7c1','3794f3d97720b3141a4be2e0289265da','bd041d3ac0e847d4497dbb84c3a08985','08e74df1551a6334bb06688b8ca948bf'}
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
def convert(s,d,raw):
 chunks=[];off=0
 for lv in range(d.m_MipCount):
  w=max(1,d.m_Width>>lv);h=max(1,d.m_Height>>lv);fmt=s.m_TextureFormat
  n=w*h*(3 if fmt==3 else 4) if fmt in (3,4) else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
  im=C.parse_image_data(raw[off:off+n],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');data,actual=C.image_to_texture2d(im,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
  if int(actual)!=d.m_TextureFormat:raise ValueError('texture format mismatch')
  chunks.append(data);off+=n
 if off!=len(raw):raise ValueError('source mip size mismatch')
 return b''.join(chunks)
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--comparison',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
for f,h in PINS.items():require(a.pc_data/f,WINDOWS_PINS[f] if a.windows_source else h)
envs={name:UnityPy.load(str(a.pc_data/name)) for name in ('sharedassets1.assets','sharedassets2.assets')};sources={name:{o.path_id:o for o in env.objects} for name,env in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.comparison:p.error('template requires --comparison')
 require(a.comparison,COMPARISON);m=json.loads(a.comparison.read_text());records=[r for r in m['results'] if not r['exact_payload'] and r['file'] not in EXCLUDED]
else:
 m=json.loads((a.input/'manifest.json').read_text());records=m['textures']
 if m['version']!=1 or m['base_obb_sha256']!=OBB:raise ValueError('invalid manifest')
if len(records)!=53 or len({r['file'] for r in records})!=53 or any(r['file'] in EXCLUDED for r in records):raise ValueError('unexpected texture count')
a.output_dir.mkdir();out_records=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for r in records:
   name=r['file'];sf=r['source_file'];sid=r['source_id']
   if not re.fullmatch(r'[0-9a-f]{32}',name) or sf not in sources or sid not in sources[sf] or sources[sf][sid].type.name!='Texture2D':raise ValueError('invalid source record')
   s=sources[sf][sid].read();source=s.get_image_data()
   if a.action=='template':
    raw=z.read('assets/bin/Data/'+name);env=UnityPy.load(raw);objs=list(env.objects)
    if len(objs)!=1 or objs[0].type.name!='Texture2D':raise ValueError('invalid target asset')
    d=objs[0].read();target=d.image_data
    if d.m_Name!=s.m_Name or (d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)!=(s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace) or s.m_TextureFormat not in (3,4,10,12) or d.m_TextureFormat not in (4,45,47) or not target or raw.count(target)!=1:raise ValueError(f'texture source mismatch {name}')
    value=convert(s,d,source)
    if len(value)!=len(target):raise ValueError('encoded size mismatch')
    off=raw.find(target);blank=bytearray(raw);blank[off:off+len(target)]=bytes(len(target));converted=bytearray(blank);converted[off:off+len(target)]=value;blank=bytes(blank)
    rec={'file':name,'name':d.m_Name,'source_file':sf,'source_id':sid,'offset':off,'size':len(target),'source_sha256':digest(source),'original_sha256':digest(raw),'template_sha256':digest(blank),'converted_sha256':digest(converted)}
    (a.output_dir/name).write_bytes(blank);out_records.append(rec)
   else:
    require(a.input/name,r['template_sha256']);blank=(a.input/name).read_bytes();off=r['offset'];size=r['size'];env=UnityPy.load(blank);objs=list(env.objects)
    if len(objs)!=1 or objs[0].type.name!='Texture2D':raise ValueError('invalid template texture')
    d=objs[0].read()
    if s.m_Name!=r['name'] or d.m_Name!=r['name'] or digest(source)!=r['source_sha256'] or not isinstance(off,int) or off<0 or off+size>len(blank) or any(blank[off:off+size]) or (d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)!=(s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace):raise ValueError('texture source/template mismatch')
    value=convert(s,d,source)
    if len(value)!=size:raise ValueError('encoded size mismatch')
    data=bytearray(blank);data[off:off+size]=value
    if digest(data)!=r['converted_sha256']:raise ValueError('converted texture digest mismatch')
    (a.output_dir/name).write_bytes(data);out_records.append(r)
 finally:
  if z is not None:z.close()
 if a.action=='template':(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'textures':out_records},indent=2)+'\n')
 print(a.action,len(out_records),'lossy candidate textures,',sum(x['size'] for x in out_records),'bytes; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
