#!/usr/bin/env python3
"""Reconstruct the default sky cubemap from one pinned Steam installation.

This is a local experiment; the remaining template contains other game data.
"""
import argparse,hashlib,shutil
from pathlib import Path
import UnityPy
PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
WINDOWS_PC='772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d'
STREAM='374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b'
BASE='417634be72651cf403146a280a30cd751eb676a2ae6339a34ae3b9f04d34c7ae'
RAW='b7607c1b9a4cd75ca0c688665e68429c846abada785dc63c9d367f3759efefdf'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=['template','restore']);p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--template-sha256');p.add_argument('--converted-textures',action='store_true')
p.add_argument('--windows-source',action='store_true',help='use pinned Windows data for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets0.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets0.assets.resS',STREAM)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.template_sha256 or len(a.template_sha256)!=64:p.error('restore requires --template-sha256')
 require(a.input_asset,a.template_sha256)
pc=UnityPy.load(str(a.pc_data/'sharedassets0.assets'));env=UnityPy.load(str(a.input_asset));sources={x.path_id:x for x in pc.objects};targets={x.path_id:x for x in env.objects}
s=sources[125];t=targets[123]
if s.type.name!='Cubemap' or t.type.name!='Cubemap':raise ValueError('cubemap missing')
s=s.read();t=t.read()
for x in (s,t):
 if (x.m_Name,x.m_Width,x.m_Height,x.m_ImageCount,x.m_MipCount,x.m_TextureFormat)!=('Default-Skybox-Cubemap',128,128,6,8,4):raise ValueError('cubemap metadata mismatch')
raw=s.get_image_data();old=t.image_data
if len(raw)!=524280 or hashlib.sha256(raw).hexdigest()!=RAW or len(old)!=len(raw):raise ValueError('cubemap payload mismatch')
if a.action=='template':
 if old!=raw:raise ValueError('cubemap does not match source')
 t.image_data=bytes(len(old))
else:
 if any(old):raise ValueError('template cubemap not empty')
 t.image_data=raw
t.save();a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets0.assets';out.write_bytes(env.file.save());h=sha(out)
 expected='7158ed15e1e8d134e836ff0e6c60cdf5309a4a6cb7a14d6e2e7777fceb4c62b7' if a.converted_textures else BASE
 if a.action=='restore' and h!=expected:raise ValueError('restored asset differs from expected output')
 print(a.action,'cubemap; asset SHA256',h,'; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
