#!/usr/bin/env python3
"""Strip/restore six exact Unity texture payloads using pinned PC data.

These local experimental templates retain other game content and are not for
release. Object IDs and hashes only cover one explicitly pinned PC build.
"""
import argparse, hashlib, json, shutil
from pathlib import Path
import UnityPy

PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
BASE='35019740b8aecf8a1cc3858779b52636f5fb8c9aeba63ccba02a618d434e6c2b'
META={'additional.json':'069c259dc94d696f15f01f8feea5a0c888b0567bbd5b663c3c29791243b41048','combined.json':'73f94dbe8b3b1f725c6600ff88705dbbe97bdb0f268c709ba566caa0de0edd3b','manifest.json':'03dc1283092b74435522cd58669c465bbeaf417f72908b8ab08cc07b5a97e355','mesh-manifest.json':'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3','outline.json':'df81d9d3a082fb46ac4d194b8869d07592c6ab79c2d5266a4579ae4eb0486e03'}
PAIRS=((698,602,'t_ghost_black','t_ghost_black'),(777,1238,'FBX_Scorpion_Color_0','FBX_Scorpion_Color'),(834,791,'15_0','15'),(964,490,'t_ghost_red','t_ghost_red'),(1042,951,'t_ghost_purple','t_ghost_purple'),(1179,780,'FBX_Scorpion_Color','FBX_Scorpion_Color'))
def digest(data):return hashlib.sha256(data).hexdigest()
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(path,expected):
 if not path.is_file() or sha(path)!=expected:raise ValueError(f'unsupported file: {path}')
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('action',choices=('strip','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
for name,h in META.items():require(a.input_dir/name,h)
if a.action=='strip':require(a.input_dir/'sharedassets1.assets',BASE)
else:
 m=json.loads((a.input_dir/'exact-renamed.json').read_text())
 if m.get('version')!=1 or m.get('base_sha256')!=BASE or len(m.get('textures',[]))!=len(PAIRS):raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['stripped_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects};records=[]
for tid,sid,target_name,source_name in PAIRS:
 if tid not in targets or sid not in sources or targets[tid].type.name!='Texture2D' or sources[sid].type.name!='Texture2D':raise ValueError('missing texture')
 dst=targets[tid].read();src=sources[sid].read();raw=src.get_image_data()
 attrs=('m_Width','m_Height','m_TextureFormat','m_MipCount','m_ImageCount','m_ColorSpace')
 if dst.m_Name!=target_name or src.m_Name!=source_name or any(getattr(dst,k)!=getattr(src,k) for k in attrs) or dst.m_TextureFormat!=4 or dst.m_ImageCount!=1 or not raw or len(dst.image_data)!=len(raw):raise ValueError('texture metadata mismatch')
 rec={'target_id':tid,'source_id':sid,'target_name':target_name,'source_name':source_name,'size':len(raw),'sha256':digest(raw)}
 if a.action=='strip':
  if dst.image_data!=raw:raise ValueError('payload is not byte-identical')
  dst.image_data=bytes(len(raw));records.append(rec)
 else:
  if next((x for x in m['textures'] if x['target_id']==tid),None)!=rec or dst.image_data!=bytes(len(raw)):raise ValueError('source or template mismatch')
  dst.image_data=raw
 dst.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 for name in META:shutil.copyfile(a.input_dir/name,a.output_dir/name)
 if a.action=='strip':(a.output_dir/'exact-renamed.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'stripped_sha256':sha(out),'textures':records},indent=2)+'\n')
 if a.action=='restore' and sha(out)!=BASE:raise ValueError('restored template differs from original')
 print(a.action,len(PAIRS),'textures; sha256',sha(out));print('Contains other game data; local research only.')
except Exception:shutil.rmtree(a.output_dir);raise
