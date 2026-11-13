#!/usr/bin/env python3
"""Experimental 13-texture templates; inputs and outputs are not redistributable."""
import argparse,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
from UnityPy.export import Texture2DConverter as C
PINS={'resources.assets':'a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366','resources.assets.resS':'58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50','sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'}
OBB='baeb4b96ce7e96a0f1584605a5d501419db3d91a7364c55affbe2f5d7edd4f16';STUDY='2d88b1e912c887c7d7bbbe4b354d5eb1c4d537808bbde929d3363ff156833b79';SKIP='d7f61bd2281833a4e91517c4c2f54dae';EMPTY='528cdf6e82019754a81bca667613b2cb'
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError('incompatible input '+str(p))
def convert(s,d,env,source,raw,empty):
 chunks=[];images=[];off=0
 for lv in range(s.m_MipCount):
  w=max(1,s.m_Width>>lv);h=max(1,s.m_Height>>lv);fmt=int(s.m_TextureFormat);n=w*h if fmt==1 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16) if fmt in (10,12) else 0
  if not n:raise ValueError('unsupported texture format')
  im=C.parse_image_data(source[off:off+n],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');images.append(im);off+=n
  encoded,actual=C.image_to_texture2d(im,4,d.object_reader.platform,d.m_PlatformBlob)
  if int(actual)!=4 or len(encoded)!=w*h*4:raise ValueError('RGBA encoding mismatch')
  chunks.append(encoded)
 if off!=len(source):raise ValueError('source mip length mismatch')
 if empty:
  if d.m_TextureFormat!=1 or s.m_TextureFormat!=1 or source!=b'\0' or d.image_data!=b'\0':raise ValueError('expected empty single-pixel atlas')
  return raw
 d.m_TextureFormat=4;d.m_CompleteImageSize=sum(map(len,chunks));d.image_data=b''.join(chunks);d.save();result=env.file.save();objects=list(UnityPy.load(result).objects)
 if len(objects)!=1 or objects[0].type.name!='Texture2D':raise ValueError('invalid result object')
 t=objects[0].read()
 if (t.m_Name,t.m_Width,t.m_Height,t.m_MipCount,t.m_TextureFormat,t.m_CompleteImageSize)!=(d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,4,len(d.image_data)):raise ValueError('invalid result metadata')
 off=0
 for im in images:
  w,h=im.size;n=w*h*4;got=C.parse_image_data(t.image_data[off:off+n],w,h,4,t.object_reader.version,t.object_reader.platform).convert('RGBA')
  if im.tobytes()!=got.tobytes():raise ValueError('pixel mismatch')
  off+=n
 if off!=len(t.image_data):raise ValueError('result mip length mismatch')
 return result
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--study',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output.exists() or not a.output.parent.is_dir():p.error('output must not exist and parent must exist')
for name,h in PINS.items():require(a.pc_data/name,WINDOWS_PINS[name] if a.windows_source else h)
envs={n:UnityPy.load(str(a.pc_data/n)) for n in ('resources.assets','sharedassets1.assets')};sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.study:p.error('--study required')
 require(a.study,STUDY);records=[r for r in json.loads(a.study.read_text()) if r['file']!=SKIP]
else:
 manifest=json.loads((a.input/'manifest.json').read_text());records=manifest['textures']
 if manifest['version']!=1 or manifest['base_obb_sha256']!=OBB:raise ValueError('invalid manifest')
if len(records)!=13 or len({r['file'] for r in records})!=13 or EMPTY not in {r['file'] for r in records}:raise ValueError('invalid record count')
a.output.mkdir();output=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for r in records:
   name=r['file'];sf=r['source_file'];sid=r['source_id']
   if not re.fullmatch(r'[0-9a-f]{32}',name) or sf not in sources or sid not in sources[sf] or sources[sf][sid].type.name!='Texture2D':raise ValueError('invalid source record')
   s=sources[sf][sid].read();source=s.get_image_data();raw=z.read('assets/bin/Data/'+name) if z else (a.input/name).read_bytes()
   if not z and digest(raw)!=r['template_sha256']:raise ValueError('template hash mismatch')
   env=UnityPy.load(raw);objs=list(env.objects)
   if len(objs)!=1 or objs[0].type.name!='Texture2D':raise ValueError('invalid target')
   d=objs[0].read();target=d.image_data
   if (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace)!=(d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace):raise ValueError('metadata mismatch')
   if z:
    if digest(source)!=r['source_payload_sha256'] or digest(target)!=r['target_payload_sha256']:raise ValueError('source or target digest mismatch')
    if name==EMPTY:
     if target!=b'\0':raise ValueError('empty atlas is nonzero')
     off=size=0;blank=raw
    else:
     if not target or raw.count(target)!=1:raise ValueError('payload is not uniquely located')
     off=raw.find(target);size=len(target);blank=bytearray(raw);blank[off:off+size]=bytes(size);blank=bytes(blank)
    converted=convert(s,d,env,source,raw,name==EMPTY)
    rec={'file':name,'name':d.m_Name,'source_file':sf,'source_id':sid,'offset':off,'size':size,'source_sha256':digest(source),'original_sha256':digest(raw),'template_sha256':digest(blank),'converted_sha256':digest(converted)}
    (a.output/name).write_bytes(blank);output.append(rec)
   else:
    off=r['offset'];size=r['size']
    if digest(source)!=r['source_sha256'] or d.m_Name!=r['name'] or not isinstance(off,int) or not isinstance(size,int) or min(off,size)<0 or off+size>len(raw) or any(raw[off:off+size]) or (name!=EMPTY and (len(target)!=size or any(target))):raise ValueError('invalid template')
    converted=convert(s,d,env,source,raw,name==EMPTY)
    if digest(converted)!=r['converted_sha256']:raise ValueError('converted digest mismatch')
    (a.output/name).write_bytes(converted);output.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'textures':output},indent=2)+'\n')
 print(a.action,len(output),'candidate textures; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output);raise
