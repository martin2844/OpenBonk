#!/usr/bin/env python3
"""Reconstruct 251 sharedassets2 mesh buffers from pinned local PC files.

This compares geometry metadata and exact buffer hashes. Other mesh fields and
other game data remain in the template; it is not a distribution artifact.
"""
import argparse,collections,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data
BASE='30cdfbeb91acec299954bdbe38502980afa4962dc073e349ca1bc2120d5182da'
PINS={'sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','level1.resS':'bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6','level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','level2.resS':'544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51','level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'}
WINDOWS_PINS={**PINS,'sharedassets2.assets':'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','level1':'9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','level2':'853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6','level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}
SOURCES=('sharedassets2.assets','level1','level2','level4')
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
  if m.m_StreamData.path not in ('sharedassets2.assets.resS','level1.resS','level2.resS'):raise ValueError('unexpected mesh resource')
  v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
 return v,bytes(m.m_IndexBuffer or [])
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--windows-source',action='store_true');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
for f,h in (WINDOWS_PINS if a.windows_source else PINS).items():require(a.pc_data/f,h)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['meshes']
 if m['version']!=1 or m['base_sha256']!=BASE or len(records)!=251 or len({r['target_id'] for r in records})!=251:raise ValueError('invalid manifest')
 require(a.input_asset,m['template_sha256'])
envs={name:UnityPy.load(str(a.pc_data/name)) for name in SOURCES};src={name:{o.path_id:o for o in env.objects} for name,env in envs.items()};env=UnityPy.load(str(a.input_asset));index=collections.defaultdict(list)
if a.action=='template':
 for name,objs in src.items():
  for o in objs.values():
   if o.type.name!='Mesh':continue
   s=o.read();v,i=payload(s)
   if v or i:index[(geometry(s),digest(v),digest(i))].append((name,o.path_id))
out_records=[]
for o in env.objects:
 if o.type.name!='Mesh':continue
 d=o.read();v,i=payload(d)
 if d.m_StreamData and d.m_StreamData.path:raise ValueError('unexpected streamed Android mesh')
 if not v and not i:raise ValueError('unexpected empty Android mesh')
 if a.action=='template':
  matches=index[(geometry(d),digest(v),digest(i))]
  if not matches:raise ValueError(f'no matching source for mesh {o.path_id}')
  name,sid=matches[0];rec={'target_id':o.path_id,'source_file':name,'source_id':sid,'name':d.m_Name,'vertices_size':len(v),'indices_size':len(i),'vertices_sha256':digest(v),'indices_sha256':digest(i)}
  out_records.append(rec);d.m_VertexData.m_DataSize=bytes(len(v));d.m_IndexBuffer=list(bytes(len(i)))
 else:
  rec=next((x for x in records if x['target_id']==o.path_id),None)
  if rec is None or rec['source_file'] not in SOURCES or rec['source_id'] not in src[rec['source_file']]:raise ValueError('mesh manifest mismatch')
  so=src[rec['source_file']][rec['source_id']]
  if so.type.name!='Mesh':raise ValueError('mesh source type mismatch')
  s=so.read();sv,si=payload(s)
  if d.m_Name!=rec['name'] or geometry(d)!=geometry(s) or len(v)!=rec['vertices_size'] or len(i)!=rec['indices_size'] or any(v) or any(i) or digest(sv)!=rec['vertices_sha256'] or digest(si)!=rec['indices_sha256']:raise ValueError('mesh source or template mismatch')
  d.m_VertexData.m_DataSize=sv;d.m_IndexBuffer=list(si);out_records.append(rec)
 d.save()
if len(out_records)!=251 or len({x['target_id'] for x in out_records})!=251:raise ValueError('unexpected mesh count')
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets2.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'mesh-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'meshes':out_records},indent=2)+'\n')
 elif h!=BASE:raise ValueError(f'restored asset differs from expected input: {h}')
 print(a.action,len(out_records),'meshes; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
