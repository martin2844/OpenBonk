#!/usr/bin/env python3
"""Local experiment: store ten Steam texture candidates uncompressed.

Input and outputs contain game data and are not redistributable. Name and size
matching does not prove identity; this script does not establish licensing.
"""
import hashlib, json, sys, zipfile
from pathlib import Path
from PIL import ImageChops
import UnityPy
from UnityPy.export import Texture2DConverter as C

p=Path('build/android/share-source-experiment')
if len(sys.argv)!=3: raise SystemExit('usage: hashed-excluded-rgba-prototype.py PC_DATA OUTPUT_DIR')
pc=Path(sys.argv[1]);out=Path(sys.argv[2])
pins={'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'}
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for chunk in iter(lambda:f.read(1024*1024),b''):h.update(chunk)
 return h.hexdigest()
for name,expected in pins.items():
 if sha(pc/name)!=expected:raise ValueError('incompatible PC source: '+name)
if sha(p/'shared-hashed-all-scrubbed-base/base.obb')!='bad91e6c18e64cb90a72b27064f06288fd174ca16b3224f6a45b87e6c33f376a':raise ValueError('incompatible base OBB')
if sha(p/'hashed-texture-comparison.json')!='886affe779061c77fd857434b19b9ff2e267a8788801448a346c3d11acb17aae':raise ValueError('incompatible comparison')
if out.exists(): raise SystemExit('output already exists')
comp=json.loads((p/'hashed-texture-comparison.json').read_text())['results']
quality={x['file']:x for x in json.loads((p/'hashed-matched64-quality.json').read_text())}
records=[r for r in comp if not r['exact_payload'] and r['file'] in quality and max(max(m['weighted_rgb'])+m['alpha'] for m in quality[r['file']]['mips'])>15]
envs={x:UnityPy.load(str(pc/x)) for x in ('sharedassets1.assets','sharedassets2.assets')}
sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()}
out.mkdir(); report=[]
with zipfile.ZipFile(p/'shared-hashed-all-scrubbed-base/base.obb') as z:
 for r in records:
  original=z.read('assets/bin/Data/'+r['file']); env=UnityPy.load(original); objs=list(env.objects)
  if len(objs)!=1 or objs[0].type.name!='Texture2D': raise ValueError('unexpected texture file')
  d=objs[0].read(); s=sources[r['source_file']][r['source_id']].read()
  if d.m_TextureFormat==4: continue
  if d.m_TextureFormat!=45 or s.m_TextureFormat not in (4,10) or (d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_ColorSpace)!=(s.m_Name,s.m_Width,s.m_Height,s.m_MipCount,s.m_ColorSpace): raise ValueError('unexpected texture metadata')
  raw=s.get_image_data(); off=0; chunks=[]; ims=[]
  for lv in range(s.m_MipCount):
   w=max(1,s.m_Width>>lv);h=max(1,s.m_Height>>lv);n=w*h*4 if s.m_TextureFormat==4 else ((w+3)//4)*((h+3)//4)*8
   im=C.parse_image_data(raw[off:off+n],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA')
   if im.getchannel('A').getextrema()!=(255,255): raise ValueError('unexpected source alpha')
   data,actual=C.image_to_texture2d(im,4,d.object_reader.platform,d.m_PlatformBlob)
   if int(actual)!=4 or len(data)!=w*h*4: raise ValueError('bad RGBA encoding')
   chunks.append(data);ims.append(im);off+=n
  if off!=len(raw):raise ValueError('unexpected source length')
  d.m_TextureFormat=4;d.m_CompleteImageSize=sum(map(len,chunks));d.image_data=b''.join(chunks);d.save()
  result=env.file.save(); parsed=UnityPy.load(result).objects[0].read()
  if (parsed.m_Name,parsed.m_Width,parsed.m_Height,parsed.m_MipCount,parsed.m_TextureFormat,parsed.m_CompleteImageSize)!=(d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,4,len(d.image_data)): raise ValueError('serialized metadata mismatch')
  off=0
  for im in ims:
   w,h=im.size;n=w*h*4; got=C.parse_image_data(parsed.image_data[off:off+n],w,h,4,parsed.object_reader.version,parsed.object_reader.platform).convert('RGBA')
   if ImageChops.difference(im,got).getbbox() is not None or im.tobytes()!=got.tobytes():raise ValueError('pixel mismatch')
   off+=n
  if off!=len(parsed.image_data):raise ValueError('serialized payload mismatch')
  (out/r['file']).write_bytes(result)
  report.append({'file':r['file'],'name':d.m_Name,'mips':len(ims),'original_bytes':len(original),'rgba_bytes':len(result),'sha256':hashlib.sha256(result).hexdigest()})
if len(report)!=10:raise ValueError('unexpected count')
(out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2));print('delta bytes',sum(x['rgba_bytes']-x['original_bytes'] for x in report))
