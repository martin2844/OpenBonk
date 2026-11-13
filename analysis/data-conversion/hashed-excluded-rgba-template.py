#!/usr/bin/env python3
"""Experimental templates for ten RGBA32 candidates, not distributable.

Template creation still needs Android game data. Restoration needs the pinned
Steam build and a template. Other data and metadata remain in the templates.
"""
import argparse,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
from UnityPy.export import Texture2DConverter as C

PINS={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'}
OBB='bad91e6c18e64cb90a72b27064f06288fd174ca16b3224f6a45b87e6c33f376a'
COMPARISON='886affe779061c77fd857434b19b9ff2e267a8788801448a346c3d11acb17aae'
FILES={'08e74df1551a6334bb06688b8ca948bf','1159c75bca52b384ab87059f7d02c173','1448d7c22bb37874980b2c17cef7d7c1','303c0b4d707421347b1619a563c8983c','3794f3d97720b3141a4be2e0289265da','48fc24aa34b0cee42909ffe20f7cff7c','6aa2f11be783f754b8a3e798c6fc9e36','6e1eb85bdb67a71479a275bfbd53b659','c6f28f3e901c2ee46b56658f5c4bbec8','fca03e78624255d468f316208c6b021f'}
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(path,expected):
 if not path.is_file() or sha(path)!=expected:raise ValueError(f'incompatible input: {path}')
def convert(s,env,d):
 raw=s.get_image_data();chunks=[];images=[];off=0
 for level in range(s.m_MipCount):
  w=max(1,s.m_Width>>level);h=max(1,s.m_Height>>level);n=w*h*4 if s.m_TextureFormat==4 else ((w+3)//4)*((h+3)//4)*8
  im=C.parse_image_data(raw[off:off+n],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA')
  if im.getchannel('A').getextrema()!=(255,255):raise ValueError('unexpected source alpha')
  data,fmt=C.image_to_texture2d(im,4,d.object_reader.platform,d.m_PlatformBlob)
  if int(fmt)!=4 or len(data)!=w*h*4:raise ValueError('RGBA encoding mismatch')
  images.append(im);chunks.append(data);off+=n
 if off!=len(raw):raise ValueError('source mip size mismatch')
 d.m_TextureFormat=4;d.m_CompleteImageSize=sum(map(len,chunks));d.image_data=b''.join(chunks);d.save();result=env.file.save()
 objects=list(UnityPy.load(result).objects)
 if len(objects)!=1 or objects[0].type.name!='Texture2D':raise ValueError('output object mismatch')
 actual=objects[0].read()
 if (actual.m_Name,actual.m_Width,actual.m_Height,actual.m_MipCount,actual.m_TextureFormat,actual.m_CompleteImageSize)!=(d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,4,len(d.image_data)):raise ValueError('output metadata mismatch')
 off=0
 for im in images:
  w,h=im.size;n=w*h*4
  got=C.parse_image_data(actual.image_data[off:off+n],w,h,4,actual.object_reader.version,actual.object_reader.platform).convert('RGBA')
  if got.tobytes()!=im.tobytes():raise ValueError('pixel mismatch')
  off+=n
 if off!=len(actual.image_data):raise ValueError('output mip size mismatch')
 return result
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--comparison',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
for name,value in PINS.items():require(a.pc_data/name,WINDOWS_PINS[name] if a.windows_source else value)
envs={x:UnityPy.load(str(a.pc_data/x)) for x in ('sharedassets1.assets','sharedassets2.assets')};sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.comparison:p.error('template requires --comparison')
 require(a.comparison,COMPARISON);records=[r for r in json.loads(a.comparison.read_text())['results'] if r['file'] in FILES]
else:
 manifest=json.loads((a.input/'manifest.json').read_text());records=manifest['textures']
 if manifest['version']!=1 or manifest['base_obb_sha256']!=OBB:raise ValueError('invalid manifest')
if len(records)!=10 or {r['file'] for r in records}!=FILES:raise ValueError('unexpected texture records')
a.output_dir.mkdir();out=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for r in records:
   name=r['file'];sf=r['source_file'];sid=r['source_id']
   if not re.fullmatch(r'[0-9a-f]{32}',name) or sf not in sources or sid not in sources[sf] or sources[sf][sid].type.name!='Texture2D':raise ValueError('invalid source')
   s=sources[sf][sid].read();source=s.get_image_data()
   raw=z.read('assets/bin/Data/'+name) if z else (a.input/name).read_bytes()
   if z is None and digest(raw)!=r['template_sha256']:raise ValueError('template hash mismatch')
   env=UnityPy.load(raw);objs=list(env.objects)
   if len(objs)!=1 or objs[0].type.name!='Texture2D':raise ValueError('unexpected target object')
   d=objs[0].read();target=d.image_data
   if d.m_TextureFormat!=45 or s.m_TextureFormat not in (4,10) or (d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)!=(s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace):raise ValueError('source and target differ')
   if z:
    if not target or raw.count(target)!=1:raise ValueError('target payload is not uniquely located')
    off=raw.find(target);blank=bytearray(raw);blank[off:off+len(target)]=bytes(len(target));blank=bytes(blank)
    result=convert(s,env,d)
    rec={'file':name,'name':d.m_Name,'source_file':sf,'source_id':sid,'offset':off,'size':len(target),'source_sha256':digest(source),'original_sha256':digest(raw),'template_sha256':digest(blank),'converted_sha256':digest(result)}
    (a.output_dir/name).write_bytes(blank);out.append(rec)
   else:
    off=r['offset'];size=r['size']
    if digest(source)!=r['source_sha256'] or d.m_Name!=r['name'] or not isinstance(off,int) or not isinstance(size,int) or min(off,size)<0 or off+size>len(raw) or any(raw[off:off+size]) or target!=bytes(size):raise ValueError('invalid template')
    result=convert(s,env,d)
    if digest(result)!=r['converted_sha256']:raise ValueError('output hash mismatch')
    (a.output_dir/name).write_bytes(result);out.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'textures':out},indent=2)+'\n')
 print(a.action,len(out),'RGBA texture candidates; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
