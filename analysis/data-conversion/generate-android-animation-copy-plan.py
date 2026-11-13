#!/usr/bin/env python3
"""Generate offset-only plans for 25 Steam-stream animation assets."""
import argparse,base64,hashlib,importlib.util,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('textures',ROOT/'analysis/data-conversion/generate-android-texture-copy-plan.py');textures=importlib.util.module_from_spec(spec);spec.loader.exec_module(textures)
MANIFEST=ROOT/'build/android/no-rewired-hashed-animation-hybrid-manifest.json';MANIFEST_SHA='716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
OUTPUT=ROOT/'build/android/animation-android-copy-plan/animation-plan.bin'
def sha(b):return hashlib.sha256(b).hexdigest()
def file_sha(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--base',type=Path,default=textures.BASE);p.add_argument('--target',type=Path,default=textures.TARGET);p.add_argument('--manifest',type=Path,default=MANIFEST);p.add_argument('--linux-zip',type=Path,default=textures.LINUX);p.add_argument('--windows-zip',type=Path,default=textures.WINDOWS);p.add_argument('--output',type=Path,default=OUTPUT);p.add_argument('--include-private-patches',action='store_true',help='local research only: include Android-derived byte differences for 83 additional clips');a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink():raise ValueError('output exists')
 for path,expected in ((a.base,textures.BASE_SHA),(a.target,textures.TARGET_SHA),(a.manifest,MANIFEST_SHA)):
  if file_sha(path)!=expected:raise ValueError('input pin mismatch: '+str(path))
 doc=json.loads(a.manifest.read_text());records=doc['animations'] if a.include_private_patches else [r for r in doc['animations'] if r['mode']=='steam-stream'];expected_count=108 if a.include_private_patches else 25
 if len(records)!=expected_count or len({r['file'] for r in records})!=expected_count or doc['selected_count']!=25:raise ValueError('animation inventory mismatch')
 plan=bytearray((b'OBSTX003' if a.include_private_patches else b'OBSTX002')+struct.pack('>I',len(records)))
 for h in (*textures.LINUX_SHA,*textures.WINDOWS_SHA):plan.extend(bytes.fromhex(h))
 with zipfile.ZipFile(a.base) as base,zipfile.ZipFile(a.target) as target,zipfile.ZipFile(a.linux_zip) as linux,zipfile.ZipFile(a.windows_zip) as windows:
  assets=[];objects=[]
  for z,expected in ((linux,textures.LINUX_SHA[0]),(windows,textures.WINDOWS_SHA[0])):
   asset=z.read('Megabonk_Data/sharedassets1.assets')
   if sha(asset)!=expected:raise ValueError('Steam source mismatch')
   assets.append(asset);objects.append({o.path_id:o for o in UnityPy.load(asset).objects})
  for rec in records:
   name=rec['file'];entry='assets/bin/Data/'+name
   if len(name)!=32 or any(c not in '0123456789abcdef' for c in name):raise ValueError('invalid animation name')
   template=base.read(entry);expected=target.read(entry)
   expected_hash=rec['converted_sha256'] if rec['mode']=='steam-stream' else rec['original_sha256']
   if sha(template)!=rec['template_sha256'] or sha(expected)!=expected_hash:raise ValueError('target mismatch')
   offsets=[];values=[]
   for asset,index in zip(assets,objects):
    obj=index[rec['source_id']]
    if obj.type.name!='AnimationClip' or obj.read().m_Name!=rec['name']:raise ValueError('animation source mismatch')
    raw=obj.get_raw_data();offset=obj.byte_start
    if len(raw)!=rec['size'] or sha(raw)!=rec['source_sha256'] or asset[offset:offset+len(raw)]!=raw:raise ValueError('animation payload mismatch')
    offsets.append(offset);values.append(raw)
   if values[0]!=values[1]:raise ValueError('platform animations differ')
   patch=b'' if rec['mode']=='steam-stream' else base64.b64decode(rec['patch_base64'],validate=True)
   count=0 if rec['mode']=='steam-stream' else rec['patch_count']
   if len(patch)!=count*5 or count>rec['size']:raise ValueError('invalid private patch')
   value=bytearray(values[0]);last=-1
   for i in range(0,len(patch),5):
    where,byte=struct.unpack_from('<IB',patch,i)
    if where<=last or where>=len(value) or value[where]==byte:raise ValueError('invalid patch record')
    value[where]=byte;last=where
   if rec['mode']=='legacy-exact' and sha(value)!=rec['object_sha256']:raise ValueError('patched animation mismatch')
   pos=rec['offset'];size=rec['size'];rebuilt=bytearray(template)
   if pos<0 or pos+size>len(rebuilt) or any(rebuilt[pos:pos+size]):raise ValueError('animation target not blank')
   rebuilt[pos:pos+size]=value
   if bytes(rebuilt)!=expected:raise ValueError('animation reconstruction differs')
   plan.extend(name.encode('ascii'))
   for h in (rec['template_sha256'],expected_hash,rec['source_sha256']):plan.extend(bytes.fromhex(h))
   plan.extend(struct.pack('>IIIQIQ',pos,size,0,offsets[0],0,offsets[1]))
   if a.include_private_patches:plan.extend(struct.pack('>I',count));plan.extend(patch)
 out.parent.mkdir(parents=True,exist_ok=True)
 with out.open('xb') as f:f.write(plan)
 print(len(records),'animations;',sum(r['size'] for r in records),'payload bytes; plan',len(plan),'sha256',sha(plan),'; private Android-derived patches included' if a.include_private_patches else '; offsets and hashes only')
if __name__=='__main__':main()
