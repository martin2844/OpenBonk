#!/usr/bin/env python3
"""Strip/rebuild 17 PC-derived textures on the pinned combined template.

Local research only: the resulting template still includes other game data.
The restored template is for the subsequent experimental build stages only.
"""
import argparse, hashlib, json, shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter

BASE='822338170b8720d2de8e954c115545d4c46549b3e033c4f7c9135650f262c855'
MANIFEST='c0b287bee5928fd5fbc4cc36c430f6c9d2cab43b99e0a9e2dc71906f7638280c'
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
METADATA={'combined.json':'73f94dbe8b3b1f725c6600ff88705dbbe97bdb0f268c709ba566caa0de0edd3b','mesh-manifest.json':'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3','outline.json':'df81d9d3a082fb46ac4d194b8869d07592c6ab79c2d5266a4579ae4eb0486e03'}
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for part in iter(lambda:f.read(1024*1024),b''):h.update(part)
 return h.hexdigest()
def require(p,d):
 if not p.is_file() or sha(p)!=d:raise ValueError(f'unsupported file: {p}')
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('action',choices=('strip','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('manifest',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.manifest,MANIFEST);require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
for name,d in METADATA.items():require(a.input_dir/name,d)
if a.action=='strip':require(a.input_dir/'sharedassets1.assets',BASE)
else:
 m=json.loads((a.input_dir/'additional.json').read_text())
 if m.get('version')!=1 or m.get('base_sha256')!=BASE or m.get('manifest_sha256')!=MANIFEST:raise ValueError('invalid template metadata')
 require(a.input_dir/'sharedassets1.assets',m['stripped_sha256'])
records=json.loads(a.manifest.read_text())['textures'];ids={r['target_id'] for r in records}
if len(records)!=17 or len(ids)!=17:raise ValueError('invalid texture records')
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));targets={o.path_id:o for o in env.objects}
for rec in records:
 tid,sid=rec['target_id'],rec['source_id']
 if tid not in targets or sid not in sources or targets[tid].type.name!='Texture2D' or sources[sid].type.name!='Texture2D':raise ValueError('missing texture')
 dst=targets[tid].read();src=sources[sid].read();raw=src.get_image_data()
 identity=lambda t:(t.m_Name,t.m_Width,t.m_Height,t.m_MipCount,t.m_ColorSpace,t.m_ImageCount)
 if identity(dst)!=identity(src) or dst.m_Name!=rec['name'] or dst.m_TextureFormat!=4 or src.m_TextureFormat not in (10,12) or dst.m_ImageCount!=1 or 'normal' in dst.m_Name.lower() or len(dst.image_data)!=rec['size'] or hashlib.sha256(raw).hexdigest()!=rec['pc_raw_sha256']:raise ValueError('texture identity mismatch')
 if a.action=='strip':dst.image_data=bytes(rec['size'])
 else:
  if dst.image_data!=bytes(rec['size']):raise ValueError('texture was not stripped')
  parts=[];offset=0
  for level in range(dst.m_MipCount):
   w,h=max(1,dst.m_Width>>level),max(1,dst.m_Height>>level);size=((w+3)//4)*((h+3)//4)*(8 if src.m_TextureFormat==10 else 16)
   image=Texture2DConverter.parse_image_data(raw[offset:offset+size],w,h,src.m_TextureFormat,src.object_reader.version,src.object_reader.platform)
   data,fmt=Texture2DConverter.image_to_texture2d(image,4,dst.object_reader.platform,dst.m_PlatformBlob)
   if int(fmt)!=4:raise ValueError('unexpected output format')
   parts.append(data);offset+=size
  payload=b''.join(parts)
  if offset!=len(raw) or len(payload)!=rec['size']:raise ValueError('mip payload size mismatch')
  dst.image_data=payload;dst.m_CompleteImageSize=len(payload)
 dst.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 for name in METADATA:shutil.copyfile(a.input_dir/name,a.output_dir/name)
 if a.action=='strip':(a.output_dir/'additional.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'manifest_sha256':MANIFEST,'stripped_sha256':sha(out)},indent=2)+'\n')
 print(a.action,len(records),'textures; SHA256',sha(out))
 print('Local experiment still contains other game data; not for distribution.')
except Exception:
 shutil.rmtree(a.output_dir);raise
