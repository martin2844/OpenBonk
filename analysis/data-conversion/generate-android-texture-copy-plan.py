#!/usr/bin/env python3
"""Generate offset-only plans for 56 exact Steam texture payloads."""
import argparse,hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'build/android/localization-shared-scrubbed-base/base.obb';TARGET=ROOT/'build/android/launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
MANIFEST=ROOT/'build/android/local-research-manifests/hashed-exact-textures-template/manifest.json'
LINUX=ROOT/'build/android/linux-steam-regression.zip';WINDOWS=ROOT/'build/android/windows-steam-full-input.zip';OUTPUT=ROOT/'build/android/texture-android-copy-plan/texture-plan.bin'
BASE_SHA='ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d';TARGET_SHA='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691';MANIFEST_SHA='72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9'
NAMES=('sharedassets1.assets','sharedassets1.assets.resS','sharedassets2.assets','sharedassets2.assets.resS')
LINUX_SHA=('d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84')
WINDOWS_SHA=('64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a',LINUX_SHA[1],'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9',LINUX_SHA[3])
def sha(b):return hashlib.sha256(b).hexdigest()
def file_sha(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--base',type=Path,default=BASE);p.add_argument('--target',type=Path,default=TARGET);p.add_argument('--manifest',type=Path,default=MANIFEST);p.add_argument('--linux-zip',type=Path,default=LINUX);p.add_argument('--windows-zip',type=Path,default=WINDOWS);p.add_argument('--output',type=Path,default=OUTPUT);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink():raise ValueError('output exists')
 for path,expected in ((a.base,BASE_SHA),(a.target,TARGET_SHA),(a.manifest,MANIFEST_SHA)):
  if file_sha(path)!=expected:raise ValueError('input pin mismatch: '+str(path))
 records=json.loads(a.manifest.read_text())['textures']
 if len(records)!=56 or len({r['file'] for r in records})!=56:raise ValueError('texture inventory mismatch')
 plan=bytearray(b'OBSTX001'+struct.pack('>I',len(records)))
 for h in (*LINUX_SHA,*WINDOWS_SHA):plan.extend(bytes.fromhex(h))
 with zipfile.ZipFile(a.base) as base,zipfile.ZipFile(a.target) as target,zipfile.ZipFile(a.linux_zip) as linux,zipfile.ZipFile(a.windows_zip) as windows:
  sources=[];objects=[]
  for z,pins in ((linux,LINUX_SHA),(windows,WINDOWS_SHA)):
   data=[z.read('Megabonk_Data/'+n) for n in NAMES]
   if tuple(sha(b) for b in data)!=pins:raise ValueError('Steam source mismatch')
   sources.append(data);objects.append({name:{o.path_id:o for o in UnityPy.load(data[i]).objects} for name,i in ((NAMES[0],0),(NAMES[2],2))})
  for rec in records:
   name=rec['file'];entry='assets/bin/Data/'+name
   if len(name)!=32 or any(c not in '0123456789abcdef' for c in name):raise ValueError('invalid texture name')
   template=base.read(entry);expected=target.read(entry)
   if sha(template)!=rec['template_sha256'] or sha(expected)!=rec['original_sha256']:raise ValueError('target mismatch')
   refs=[]
   for data,index in zip(sources,objects):
    obj=index[rec['source_file']][rec['source_id']]
    if obj.type.name!='Texture2D':raise ValueError('texture source type mismatch')
    tex=obj.read()
    if tex.m_Name!=rec['name']:raise ValueError('texture name mismatch')
    raw=obj.get_raw_data();payload=bytes(tex.image_data or b'')
    if payload:
     at=raw.find(payload)
     if at<0:raise ValueError('inline texture data missing')
     file_id=NAMES.index(rec['source_file']);offset=obj.byte_start+at
    else:
     stream=tex.m_StreamData
     if not stream or stream.path not in NAMES or stream.offset<0 or stream.size<=0:raise ValueError('invalid texture stream')
     file_id=NAMES.index(stream.path);offset=stream.offset;payload=data[file_id][offset:offset+stream.size]
    if len(payload)!=rec['size'] or sha(payload)!=rec['payload_sha256'] or data[file_id][offset:offset+len(payload)]!=payload:raise ValueError('texture payload mismatch')
    refs.append((file_id,offset,payload))
   if refs[0][2]!=refs[1][2]:raise ValueError('platform textures differ')
   offset=rec['offset'];size=rec['size'];rebuilt=bytearray(template)
   if offset<0 or offset+size>len(rebuilt) or any(rebuilt[offset:offset+size]):raise ValueError('texture target not blank')
   rebuilt[offset:offset+size]=refs[0][2]
   if bytes(rebuilt)!=expected:raise ValueError('texture reconstruction differs')
   plan.extend(name.encode('ascii'))
   for key in ('template_sha256','original_sha256','payload_sha256'):plan.extend(bytes.fromhex(rec[key]))
   plan.extend(struct.pack('>IIIQIQ',offset,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1]))
 out.parent.mkdir(parents=True,exist_ok=True)
 with out.open('xb') as f:f.write(plan)
 print('56 textures;',sum(r['size'] for r in records),'payload bytes; plan',len(plan),'sha256',sha(plan),'offsets and hashes only')
if __name__=='__main__':main()
