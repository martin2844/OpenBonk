#!/usr/bin/env python3
"""Strip/restore 56 exact texture payloads in hashed local OBB assets.

Other game data and metadata remain; templates are not distribution packages.
"""
import argparse,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
PINS={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'}
OBB='baf98c2c7cc11067de8276c08febc76404f07854191260101e920de1a991bf72';COMPARISON='886affe779061c77fd857434b19b9ff2e267a8788801448a346c3d11acb17aae'
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--comparison',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
for f,h in PINS.items():require(a.pc_data/f,WINDOWS_PINS[f] if a.windows_source else h)
envs={name:UnityPy.load(str(a.pc_data/name)) for name in ('sharedassets1.assets','sharedassets2.assets')};sources={name:{o.path_id:o for o in env.objects} for name,env in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.comparison:p.error('template requires --comparison')
 require(a.comparison,COMPARISON);m=json.loads(a.comparison.read_text());records=[r for r in m['results'] if r['exact_payload']]
else:
 m=json.loads((a.input/'manifest.json').read_text());records=m['textures']
 if m['version']!=1 or m['base_obb_sha256']!=OBB:raise ValueError('invalid manifest')
if len(records)!=56 or len({r['file'] for r in records})!=56:raise ValueError('unexpected texture count')
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
    if d.m_Name!=s.m_Name or (d.m_Width,d.m_Height,d.m_TextureFormat,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)!=(s.m_Width,s.m_Height,s.m_TextureFormat,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace) or not target or source!=target or raw.count(target)!=1:raise ValueError(f'texture source mismatch {name}')
    off=raw.find(target);blank=bytearray(raw);blank[off:off+len(target)]=bytes(len(target));blank=bytes(blank)
    rec={'file':name,'name':d.m_Name,'source_file':sf,'source_id':sid,'offset':off,'size':len(target),'payload_sha256':digest(target),'original_sha256':digest(raw),'template_sha256':digest(blank)}
    (a.output_dir/name).write_bytes(blank);out_records.append(rec)
   else:
    require(a.input/name,r['template_sha256']);blank=(a.input/name).read_bytes();off=r['offset'];size=r['size']
    if s.m_Name!=r['name'] or len(source)!=size or digest(source)!=r['payload_sha256'] or not isinstance(off,int) or off<0 or off+size>len(blank) or any(blank[off:off+size]):raise ValueError('texture source/template mismatch')
    data=bytearray(blank);data[off:off+size]=source
    if digest(data)!=r['original_sha256']:raise ValueError('restored texture digest mismatch')
    (a.output_dir/name).write_bytes(data);out_records.append(r)
 finally:
  if z is not None:z.close()
 if a.action=='template':(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'textures':out_records},indent=2)+'\n')
 print(a.action,len(out_records),'exact textures,',sum(x['size'] for x in out_records),'bytes; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
