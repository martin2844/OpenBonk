#!/usr/bin/env python3
"""Local RGBA32 conversion study of 13 PC texture candidates.

Inputs and outputs retain game data. This is not a distribution package.
Candidates and filenames are tied to one exact PC build and local Android base.
"""
import hashlib,json,shutil,sys,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PINS={'resources.assets':'a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366','resources.assets.resS':'58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50','sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'}
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for block in iter(lambda:f.read(1048576),b''):h.update(block)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError('incompatible input '+str(p))
if len(sys.argv)!=5:raise SystemExit('usage: hashed-extra-candidates-prototype.py PC_DATA OBB STUDY_JSON OUTPUT_DIR')
pc,obb,study,out=map(Path,sys.argv[1:])
if out.exists() or not out.parent.is_dir():raise SystemExit('output must not exist and parent must exist')
for n,h in PINS.items():require(pc/n,h)
require(obb,'baeb4b96ce7e96a0f1584605a5d501419db3d91a7364c55affbe2f5d7edd4f16');require(study,'2d88b1e912c887c7d7bbbe4b354d5eb1c4d537808bbde929d3363ff156833b79')
records=[r for r in json.loads(study.read_text()) if r['file']!='d7f61bd2281833a4e91517c4c2f54dae']
if len(records)!=13 or len({r['file'] for r in records})!=13:raise ValueError('wrong candidates')
envs={f:UnityPy.load(str(pc/f)) for f in ('resources.assets','sharedassets1.assets')};sources={f:{o.path_id:o for o in e.objects} for f,e in envs.items()};out.mkdir();result=[]
try:
 with zipfile.ZipFile(obb) as z:
  for r in records:
   raw=z.read('assets/bin/Data/'+r['file']);env=UnityPy.load(raw);objects=list(env.objects)
   if len(objects)!=1 or objects[0].type.name!='Texture2D':raise ValueError('unexpected target')
   d=objects[0].read();s=sources[r['source_file']][r['source_id']].read();source=s.get_image_data()
   if hashlib.sha256(source).hexdigest()!=r['source_payload_sha256'] or hashlib.sha256(d.image_data).hexdigest()!=r['target_payload_sha256']:raise ValueError('payload hash mismatch')
   if (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_ColorSpace)!=(d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace):raise ValueError('metadata mismatch')
   chunks=[];images=[];offset=0
   for lv in range(s.m_MipCount):
    w=max(1,s.m_Width>>lv);h=max(1,s.m_Height>>lv);fmt=int(s.m_TextureFormat)
    size=w*h if fmt==1 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16) if fmt in (10,12) else 0
    if not size:raise ValueError('unsupported source format')
    im=C.parse_image_data(source[offset:offset+size],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');images.append(im);offset+=size
    data,actual=C.image_to_texture2d(im,4,d.object_reader.platform,d.m_PlatformBlob)
    if int(actual)!=4 or len(data)!=w*h*4:raise ValueError('RGBA encode failed')
    chunks.append(data)
   if offset!=len(source):raise ValueError('mip sizes differ')
   if d.m_TextureFormat==1 and s.m_TextureFormat==1 and source==d.image_data:
    output=raw;output_format=1
   else:
    d.m_TextureFormat=4;d.m_CompleteImageSize=sum(map(len,chunks));d.image_data=b''.join(chunks);d.save();output=env.file.save();output_format=4
    check=UnityPy.load(output).objects[0].read();offset=0
    if (check.m_Width,check.m_Height,check.m_MipCount,check.m_TextureFormat)!=(s.m_Width,s.m_Height,s.m_MipCount,4):raise ValueError('serialized metadata differs')
    for im in images:
     w,h=im.size;n=w*h*4;recovered=C.parse_image_data(check.image_data[offset:offset+n],w,h,4,check.object_reader.version,check.object_reader.platform).convert('RGBA')
     if recovered.tobytes()!=im.tobytes():raise ValueError('pixels changed')
     offset+=n
    if offset!=len(check.image_data):raise ValueError('serialized mip size mismatch')
   (out/r['file']).write_bytes(output)
   result.append({'file':r['file'],'name':r['name'],'source_file':r['source_file'],'source_id':r['source_id'],'format':output_format,'old_size':len(raw),'new_size':len(output),'sha256':hashlib.sha256(output).hexdigest()})
 (out/'report.json').write_text(json.dumps(result,indent=2)+'\n')
 print('converted',len(result),'candidates; added bytes',sum(x['new_size']-x['old_size'] for x in result))
except Exception:shutil.rmtree(out);raise
