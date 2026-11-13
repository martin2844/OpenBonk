#!/usr/bin/env python3
"""Build a private pack of locally compiled Unity builtin shaders for diagnostics."""
import hashlib,importlib.util,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';CACHE=B/'pinned-shader-cache-test';INPUT=B/'animation-diagnostic-obb/linux.obb';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';OUT=B/'builtin-shader-android-plan';ENTRY='assets/bin/Data/0000000000000000f000000000000000'
def sha(b):return hashlib.sha256(b).digest()
def main():
 spec=importlib.util.spec_from_file_location('pins',ROOT/'tools/pinned-shader-bundles.py');p=importlib.util.module_from_spec(spec);spec.loader.exec_module(p);p.verify(CACHE)
 with zipfile.ZipFile(INPUT) as z:base=z.read(ENTRY)
 with zipfile.ZipFile(TARGET) as z:target=z.read(ENTRY)
 source={}
 for o in UnityPy.load(str(CACHE/'builtin-probe')).objects:
  if o.type.name=='Shader':source[o.read_typetree()['m_ParsedForm']['m_Name']]=o.get_raw_data()
 expected={1:'Standard',2:'Legacy Shaders/Particles/Alpha Blended',3:'Unlit/Texture'};work=bytearray(base);rows=[]
 for obj in UnityPy.load(target).objects:
  if obj.path_id not in expected:continue
  value=source[expected[obj.path_id]];off=obj.byte_start;end=off+obj.byte_size
  if obj.type.name!='Shader' or len(value)!=obj.byte_size or value!=target[off:end] or any(base[off:end]):raise ValueError('shader mismatch')
  work[off:end]=value;rows.append((obj.path_id,off,len(value),sha(value),value))
 rows.sort()
 if len(rows)!=3 or work!=target:raise ValueError('asset reconstruction differs')
 OUT.mkdir(parents=True,exist_ok=True);pack=OUT/'shader-pack.zip';plan=OUT/'plan.bin'
 if pack.exists() or plan.exists():raise ValueError('output exists')
 with zipfile.ZipFile(pack,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
  for i,_,_,_,v in rows:
   info=zipfile.ZipInfo(f'shader-{i}.bin',(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;z.writestr(info,v)
 pack_hash=sha(pack.read_bytes());data=bytearray(b'OBBSH001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(target)+pack_hash)
 for i,off,n,h,v in rows:data+=struct.pack('>III',i,off,n)+h
 plan.write_bytes(data)
 print('PASS: 3 Unity builtin shaders,',sum(x[2] for x in rows),'bytes; plan',sha(data).hex(),'pack',pack_hash.hex())
if __name__=='__main__':main()
