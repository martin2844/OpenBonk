#!/usr/bin/env python3
"""Convert seven pinned PC cubemaps to Android ETC2 in a local template.

Six require the separately built six-face Unity Crunch adapter. Input hashes
are checked before native decoding. This research template still contains game
content and is not suitable for distribution.
"""
import argparse,hashlib,json,shutil,subprocess,tempfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';TARGET='901d7e06a4bebaaab0ec8fc069b40a707ee665bca709586d77dfcdeeeb5dae72'
META={'additional.json':'069c259dc94d696f15f01f8feea5a0c888b0567bbd5b663c3c29791243b41048','combined.json':'73f94dbe8b3b1f725c6600ff88705dbbe97bdb0f268c709ba566caa0de0edd3b','exact-renamed.json':'37f2efb47b603148210cd66887b0a57f2c5ad12fd3049ea71d19642531b86984','large.json':'716afb7065e565d32b6e74ad7db2e83284451d97dddb4c8db01a1144d4057487','manifest.json':'03dc1283092b74435522cd58669c465bbeaf417f72908b8ab08cc07b5a97e355','mesh-manifest.json':'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3','normal.json':'52410f94894e876fd2ef07f964db77380d91147dd999ef95af510787c5c61308','outline.json':'df81d9d3a082fb46ac4d194b8869d07592c6ab79c2d5266a4579ae4eb0486e03'}
PAIRS=((1902,2284,'Desert1',29),(1903,2285,'Desert2',29),(1904,2286,'Forest3',29),(1905,2287,'Forest2',29),(1906,2289,'Desert3',29),(1907,2290,'_Graveyard',12),(1908,2288,'Forest1',29))
def sha_bytes(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('decoder',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',PC);require(a.pc_data/'sharedassets1.assets.resS',STREAM)
for name,expected in META.items():require(a.input_dir/name,expected)
if not a.decoder.is_file():raise ValueError('missing six-face Unity Crunch decoder')
if a.action=='template':require(a.input_dir/'sharedassets1.assets',TARGET)
else:
 m=json.loads((a.input_dir/'cubemaps.json').read_text())
 if m.get('version')!=1 or m.get('target_sha256')!=TARGET or len(m.get('cubemaps',[]))!=len(PAIRS):raise ValueError('invalid manifest')
 require(a.input_dir/'sharedassets1.assets',m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));env=UnityPy.load(str(a.input_dir/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects};records=[]
with tempfile.TemporaryDirectory(prefix='.openbonk-cubemap-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp)
 for tid,sid,name,sfmt in PAIRS:
  if tid not in targets or sid not in sources or targets[tid].type.name!='Cubemap' or sources[sid].type.name!='Cubemap':raise ValueError('cubemap missing')
  d=targets[tid].read();s=sources[sid].read();attrs=('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
  if any(getattr(d,k)!=getattr(s,k) for k in attrs) or (s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_TextureFormat,d.m_TextureFormat)!=(name,512,512,1,6,sfmt,45):raise ValueError('metadata mismatch')
  raw=s.get_image_data();old=d.image_data
  if len(old)!=786432 or d.m_CompleteImageSize!=131072:raise ValueError('unexpected destination face size')
  if sfmt==29:
   src=tmp/'source.crn';out=tmp/'faces.dxt5';src.write_bytes(raw);out.unlink(missing_ok=True)
   subprocess.run([str(a.decoder.resolve()),str(src),str(out)],check=True,stdout=subprocess.DEVNULL)
   decoded=out.read_bytes()
  else:decoded=raw
  if len(decoded)!=1572864:raise ValueError('unexpected DXT5 face size')
  rec={'target_id':tid,'source_id':sid,'name':name,'source_sha256':sha_bytes(raw),'decoded_sha256':sha_bytes(decoded),'size':len(old)}
  if a.action=='template':d.image_data=bytes(len(old));records.append(rec)
  else:
   if next((r for r in m['cubemaps'] if r['target_id']==tid),None)!=rec or old!=bytes(len(old)):raise ValueError('source or target data mismatch')
   faces=[]
   for face in range(6):
    im=C.parse_image_data(decoded[face*262144:(face+1)*262144],512,512,12,s.object_reader.version,s.object_reader.platform)
    data,fmt=C.image_to_texture2d(im,45,d.object_reader.platform,d.m_PlatformBlob)
    if int(fmt)!=45 or len(data)!=131072:raise ValueError('invalid face encoding')
    faces.append(data)
   d.image_data=b''.join(faces)
  d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 for name in META:shutil.copyfile(a.input_dir/name,a.output_dir/name)
 if a.action=='template':(a.output_dir/'cubemaps.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':sha(out),'cubemaps':records},indent=2)+'\n')
 print(a.action,len(PAIRS),'six-face cubemaps; SHA256',sha(out));print('Contains other game data; not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
