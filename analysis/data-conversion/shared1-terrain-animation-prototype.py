#!/usr/bin/env python3
"""Reconstruct selected Unity terrain and animation objects from pinned Steam data.

Platform-specific bytes are recorded as patches. Templates and outputs still
contain game data and are local experiments, not distribution packages.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
WINDOWS_PC='64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
BASE='74691b1e689d7541993adb6b7a6800c153731047f1c6447b64503a6af20e6e3b'
PAIRS=[(1770,2097),(1771,2098),(1772,2099),(1773,2088),(1774,2089),(1775,2090),(1776,2091),(1777,2092),(1778,2093),(1779,2095),(1780,2094),(1781,2096),(6368,9698)]
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--converted-textures',action='store_true');p.add_argument('--all-textures',action='store_true');p.add_argument('--windows-source',action='store_true')
a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PC if a.windows_source else PC)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['objects']
 if m['version']!=1 or m['base_sha256']!=BASE or [(x['target_id'],x['source_id']) for x in records]!=PAIRS:raise ValueError('invalid manifest')
 require(a.input_asset,'6c6cb5c945c309372da9f0ad24e00c7ce465a37dcc8961ae56a0e7f37dafe12a' if a.all_textures else '522bc781606b2dda42a7ac7cac8f048a5562c1ecc7f9d6e792dd1e7b82e39500' if a.converted_textures else m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_asset));sources={x.path_id:x for x in pc.objects};targets={x.path_id:x for x in env.objects};out_records=[]
for idx,(tid,sid) in enumerate(PAIRS):
 so=sources[sid];to=targets[tid];typ='TerrainData' if tid==6368 else 'AnimationClip'
 if so.type.name!=typ or to.type.name!=typ:raise ValueError('object type mismatch')
 raw=so.get_raw_data();old=to.get_raw_data()
 if len(raw)!=len(old):raise ValueError('object length mismatch')
 if a.action=='template':
  s=so.read();d=to.read()
  if s.m_Name!=d.m_Name:raise ValueError('name mismatch')
  patch=[[i,b] for i,(b,c) in enumerate(zip(old,raw)) if b!=c]
  if len(patch)>10000:raise ValueError('too many differences')
  out_records.append({'target_id':tid,'source_id':sid,'name':d.m_Name,'type':typ,'size':len(old),'source_sha256':digest(raw),'target_sha256':digest(old),'edits':patch})
  to.set_raw_data(bytes(len(old)))
 else:
  r=records[idx]
  if r['type']!=typ or r['size']!=len(raw) or r['source_sha256']!=digest(raw) or any(old):raise ValueError('source/template mismatch')
  result=bytearray(raw);seen=set()
  for offset,value in r['edits']:
   if not isinstance(offset,int) or offset<0 or offset>=len(result) or offset in seen or not isinstance(value,int) or not 0<=value<=255:raise ValueError('invalid patch')
   seen.add(offset);result[offset]=value
  if len(seen)>10000 or digest(result)!=r['target_sha256']:raise ValueError('patch mismatch')
  to.set_raw_data(bytes(result))
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'objects-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'objects':out_records},separators=(',',':'))+'\n')
 elif h!=('69252ea2b84f122bff60a0085d074e53b5f60400b13f67ef579fc469a91bd20a' if a.all_textures else '1e75e45736ffb5d433516feaa760aa1f1f0dd949163064e67936bfee01f56aec' if a.converted_textures else BASE):raise ValueError('restored asset differs from expected output')
 print(a.action,len(PAIRS),'objects; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
