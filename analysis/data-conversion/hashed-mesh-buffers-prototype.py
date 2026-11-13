#!/usr/bin/env python3
"""Strip and restore exact buffers in 50 hashed OBB mesh assets.

Uses byte offsets verified against Unity's parsed mesh buffers, preserving the
serialized file byte-for-byte. Other game data and metadata remain; neither
stage is a distributable package.
"""
import argparse,collections,hashlib,json,re,shutil,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';OBB='bade593b27a5ee3c624b2e9c9efef6f26bbe5bc4ac0c2b4d367c4cfae89b3017'
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
def geometry(m):return (m.m_VertexData.m_VertexCount,tuple((x.stream,x.offset,x.format,x.dimension) for x in m.m_VertexData.m_Channels),m.m_IndexFormat)
def payload(m):
 v=bytes(m.m_VertexData.m_DataSize or b'')
 if not v and m.m_StreamData and m.m_StreamData.path:
  if m.m_StreamData.path!='sharedassets1.assets.resS':raise ValueError('unexpected mesh resource')
  v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
 return v,bytes(m.m_IndexBuffer or [])
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path,help='base OBB for template, template directory for restore');p.add_argument('output_dir',type=Path);p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PINS['sharedassets1.assets'] if a.windows_source else PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};index=collections.defaultdict(list)
if a.action=='template':
 require(a.input,OBB)
 for o in pc.objects:
  if o.type.name!='Mesh':continue
  m=o.read();v,i=payload(m)
  if v or i:index[(geometry(m),digest(v),digest(i))].append(o.path_id)
else:
 m=json.loads((a.input/'manifest.json').read_text());records=m['meshes']
 if m['version']!=1 or m['base_obb_sha256']!=OBB or len(records)!=50 or len({x['file'] for x in records})!=50:raise ValueError('invalid manifest')
a.output_dir.mkdir();out_records=[]
try:
 if a.action=='template':
  with zipfile.ZipFile(a.input) as z:
   for name in z.namelist():
    if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',name):continue
    raw=z.read(name);env=UnityPy.load(raw);objects=list(env.objects)
    if len(objects)!=1 or objects[0].type.name!='Mesh':continue
    d=objects[0].read()
    if d.m_StreamData and d.m_StreamData.path:raise ValueError('streamed target mesh unsupported')
    v,i=payload(d);matches=index[(geometry(d),digest(v),digest(i))]
    if len(matches)!=1 or not v or not i or raw.count(v)!=1 or raw.count(i)!=1:raise ValueError(f'ambiguous mesh {name}')
    vo=raw.find(v);io=raw.find(i)
    if not (vo+len(v)<=io or io+len(i)<=vo):raise ValueError('overlapping buffer offsets')
    data=bytearray(raw);data[vo:vo+len(v)]=bytes(len(v));data[io:io+len(i)]=bytes(len(i));blank=bytes(data)
    parsed=UnityPy.load(blank).objects[0].read();bv,bi=payload(parsed)
    if any(bv) or any(bi) or len(bv)!=len(v) or len(bi)!=len(i) or geometry(parsed)!=geometry(d):raise ValueError('zero template validation failed')
    filename=Path(name).name;rec={'file':filename,'target_name':d.m_Name,'source_id':matches[0],'vertex_offset':vo,'vertex_size':len(v),'vertex_sha256':digest(v),'index_offset':io,'index_size':len(i),'index_sha256':digest(i),'original_sha256':digest(raw),'template_sha256':digest(blank)}
    out_records.append(rec);(a.output_dir/filename).write_bytes(blank)
  if len(out_records)!=50:raise ValueError(f'unexpected mesh count {len(out_records)}')
  (a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'meshes':out_records},indent=2)+'\n')
 else:
  for rec in records:
   filename=rec['file']
   if not re.fullmatch(r'[0-9a-f]{32}',filename) or rec['source_id'] not in sources or sources[rec['source_id']].type.name!='Mesh':raise ValueError('invalid source record')
   path=a.input/filename;require(path,rec['template_sha256']);blank=path.read_bytes();env=UnityPy.load(blank)
   if len(env.objects)!=1 or env.objects[0].type.name!='Mesh':raise ValueError('invalid mesh template')
   d=env.objects[0].read();s=sources[rec['source_id']].read();v,i=payload(s);dv,di=payload(d)
   if d.m_Name!=rec['target_name'] or geometry(d)!=geometry(s) or len(v)!=rec['vertex_size'] or len(i)!=rec['index_size'] or len(dv)!=len(v) or len(di)!=len(i) or any(dv) or any(di) or digest(v)!=rec['vertex_sha256'] or digest(i)!=rec['index_sha256']:raise ValueError('mesh source/template mismatch')
   vo=rec['vertex_offset'];io=rec['index_offset']
   if not isinstance(vo,int) or not isinstance(io,int) or min(vo,io)<0 or vo+len(v)>len(blank) or io+len(i)>len(blank) or not(vo+len(v)<=io or io+len(i)<=vo):raise ValueError('bad mesh offsets')
   result=bytearray(blank);result[vo:vo+len(v)]=v;result[io:io+len(i)]=i
   if digest(result)!=rec['original_sha256']:raise ValueError('restored mesh digest mismatch')
   (a.output_dir/filename).write_bytes(result);out_records.append(rec)
 print(a.action,len(out_records),'hashed meshes,',sum(x['vertex_size']+x['index_size'] for x in out_records),'buffer bytes; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
