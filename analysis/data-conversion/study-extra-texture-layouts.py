#!/usr/bin/env python3
"""Study metadata edits needed for 25 additional Steam texture conversions.

Output contains hashes, offsets and Android-derived header bytes. It is a local
research artifact, not a reviewed distributable plan or a runtime converter.
"""
import hashlib
import json
import zipfile
from pathlib import Path
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
B=ROOT/'build/android'
MANIFESTS=('hashed-extra-candidates-template','hashed-final-candidates-template','hashed-excluded-rgba-template')
SOURCE=('sharedassets1.assets','sharedassets1.assets.resS','sharedassets2.assets','sharedassets2.assets.resS','resources.assets','resources.assets.resS')
PINS=(('d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366','58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50'),('64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','142439d97de6546370e03b3c09831c4401383e056816aee19966465ef861e0db','58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50'))
OUT=B/'extra-texture-layout-study/layouts.json'
def sha(b):return hashlib.sha256(b).hexdigest()

def main():
 if OUT.exists() or OUT.is_symlink():raise ValueError('output exists')
 records=[r for m in MANIFESTS for r in json.loads((B/'local-research-manifests'/m/'manifest.json').read_text())['textures']]
 if len(records)!=25 or len({r['file'] for r in records})!=25:raise ValueError('unexpected inventory')
 result=[]
 with zipfile.ZipFile(B/'localization-shared-scrubbed-base/base.obb') as base,zipfile.ZipFile(B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb') as final,zipfile.ZipFile(B/'linux-steam-regression.zip') as linux,zipfile.ZipFile(B/'windows-steam-full-input.zip') as windows:
  source_data=[];source_objects=[]
  for z,pins in ((linux,PINS[0]),(windows,PINS[1])):
   data=[z.read('Megabonk_Data/'+n) for n in SOURCE]
   if tuple(sha(v) for v in data)!=pins:raise ValueError('source pin mismatch')
   source_data.append(data);source_objects.append({n:{o.path_id:o for o in UnityPy.load(data[SOURCE.index(n)]).objects} for n in ('sharedassets1.assets','sharedassets2.assets','resources.assets')})
  for rec in records:
   name=rec['file']; old=base.read('assets/bin/Data/'+name);target=final.read('assets/bin/Data/'+name)
   if sha(old)!=rec['template_sha256'] or sha(target)!=rec['converted_sha256']:raise ValueError('asset mismatch')
   tex=UnityPy.load(target).objects[0].read(); pixels=bytes(tex.image_data);offset=target.find(pixels)
   if not pixels or offset<0:raise ValueError('target pixel data missing')
   if target.count(pixels)!=1:
    if len(pixels)!=1 or rec['offset']>=len(target) or target[rec['offset']]!=pixels[0]:raise ValueError('ambiguous pixel data')
    offset=rec['offset']
   suffix=len(target)-offset-len(pixels)
   if offset>len(old) or suffix>len(old) or (suffix and old[-suffix:]!=target[-suffix:]):raise ValueError('unsupported serialized layout')
   patch=[[i,b] for i,(a,b) in enumerate(zip(old[:offset],target[:offset])) if a!=b]
   header=bytearray(old[:offset])
   for i,v in patch:header[i]=v
   rebuilt=bytes(header)+pixels+(old[-suffix:] if suffix else b'')
   if rebuilt!=target:raise ValueError('structural reconstruction failed')
   refs=[];properties=[]
   for data,objects in zip(source_data,source_objects):
    obj=objects[rec['source_file']][rec['source_id']];source=obj.read();payload=bytes(source.image_data or b'');raw=obj.get_raw_data()
    if payload:
     at=raw.find(payload)
     if at<0:raise ValueError('missing inline payload')
     fid=SOURCE.index(rec['source_file']);start=obj.byte_start+at
    else:
     s=source.m_StreamData
     if not s or s.path not in SOURCE or s.offset<0 or s.size<=0:raise ValueError('bad stream')
     fid=SOURCE.index(s.path);start=s.offset;payload=data[fid][start:start+s.size]
    if sha(payload)!=rec['source_sha256'] or data[fid][start:start+len(payload)]!=payload:raise ValueError('source payload differs')
    refs.append({'file':SOURCE[fid],'offset':start,'size':len(payload),'sha256':sha(payload)})
    properties.append((source.m_Width,source.m_Height,source.m_MipCount,source.m_TextureFormat))
   if refs[0]['sha256']!=refs[1]['sha256'] or properties[0]!=properties[1] or properties[0][:3]!=(tex.m_Width,tex.m_Height,tex.m_MipCount):raise ValueError('platform or dimensions differ')
   result.append({'file':name,'base_sha256':sha(old),'target_sha256':sha(target),'width':tex.m_Width,'height':tex.m_Height,'mips':tex.m_MipCount,'source_format':properties[0][3],'target_format':tex.m_TextureFormat,'target_pixel_offset':offset,'target_pixel_size':len(pixels),'suffix_bytes':suffix,'header_patch':patch,'linux':refs[0],'windows':refs[1]})
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('x') as f:json.dump({'version':1,'private':True,'textures':result},f,indent=2);f.write('\n')
 print('verified',len(result),'layouts;',sum(len(x['header_patch']) for x in result),'Android-derived header bytes; output is private')

if __name__=='__main__':main()
