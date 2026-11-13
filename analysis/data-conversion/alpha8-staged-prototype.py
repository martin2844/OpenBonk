#!/usr/bin/env python3
"""Experimentally reconstruct six alpha textures from pinned local PC data.

Converts Alpha8 source to RGBA32, preserving alpha and zero RGB. Game data
remains in these files, which are local research inputs and not release assets.
"""
import argparse, collections, hashlib, json, shutil
from pathlib import Path
import UnityPy

PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET='f5d3112398a37fa475a2a49eec9e80f35df3fa354387e3f729381a49ef26bd5b'
META={'additional.json':'069c259dc94d696f15f01f8feea5a0c888b0567bbd5b663c3c29791243b41048','combined.json':'73f94dbe8b3b1f725c6600ff88705dbbe97bdb0f268c709ba566caa0de0edd3b','mesh-manifest.json':'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3','outline.json':'df81d9d3a082fb46ac4d194b8869d07592c6ab79c2d5266a4579ae4eb0486e03'}
EXPECTED={442:513,479:462,596:464,737:559,884:1438,1046:463}
def digest(data):return hashlib.sha256(data).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
 return h.hexdigest()
def require(p,d):
 if not p.is_file() or sha(p)!=d:raise ValueError(f'unsupported file: {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
for name,expected in META.items():require(a.input_dir/name,expected)
if a.action=='template':require(a.input_dir/'sharedassets1.assets',TARGET)
else:
 m=json.loads((a.input_dir/'manifest.json').read_text())
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET or {r['target_id']:r['source_id'] for r in m['textures']}!=EXPECTED:raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));targets={o.path_id:o for o in env.objects};records=[]
for tid,sid in EXPECTED.items():
 if tid not in targets or sid not in sources or targets[tid].type.name!='Texture2D' or sources[sid].type.name!='Texture2D':raise ValueError('texture missing')
 dst=targets[tid].read();src=sources[sid].read();identity=lambda t:(t.m_Name,t.m_Width,t.m_Height,t.m_MipCount,t.m_ColorSpace,t.m_ImageCount)
 if identity(dst)!=identity(src) or dst.m_MipCount!=1 or dst.m_ImageCount!=1 or src.m_TextureFormat!=1 or dst.m_TextureFormat!=47:raise ValueError('texture metadata mismatch')
 raw=src.get_image_data();image=src.image.convert('RGBA')
 if len(raw)!=src.m_Width*src.m_Height or any(image.getchannel(c).getbbox() is not None for c in 'RGB'):raise ValueError('unexpected alpha source')
 rec={'target_id':tid,'source_id':sid,'name':dst.m_Name,'size':len(dst.image_data),'source_sha256':digest(raw),'rgba_sha256':digest(image.tobytes())}
 if a.action=='template':
  if not dst.image_data:raise ValueError('empty image')
  dst.image_data=bytes(rec['size']);records.append(rec)
 else:
  expected=next(r for r in m['textures'] if r['target_id']==tid)
  if rec!=expected or dst.image_data!=bytes(rec['size']):raise ValueError('source or template mismatch')
  dst.set_image(image,target_format=4,mipmap_count=1)
  if dst.m_TextureFormat!=4 or dst.m_MipCount!=1:raise ValueError('conversion failed')
 dst.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 for name in META:shutil.copyfile(a.input_dir/name,a.output_dir/name)
 if a.action=='template':(a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'textures':records},indent=2)+'\n')
 print(a.action,len(EXPECTED),'textures, asset sha256',sha(out));print('Other game data remains: not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
