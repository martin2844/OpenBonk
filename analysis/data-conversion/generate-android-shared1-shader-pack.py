#!/usr/bin/env python3
"""Stage locally compiled Unity shader bodies for a private Android study.

The shader pack contains locally compiled assets with unresolved distribution
rights. It is not a release artifact.
"""
import hashlib,importlib.util,json,struct,zipfile
from collections import defaultdict
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-normal-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';BUNDLES=B/'pinned-shader-cache-test';OUT=B/'shared1-shader-pack'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 spec=importlib.util.spec_from_file_location('pinned_shaders',ROOT/'tools/pinned-shader-bundles.py');mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod);mod.verify(BUNDLES)
 base=BASE.read_bytes()
 if sha(base).hex()!='ae07560ac689959eb102e9dab569e0247b39d0ee7845e97d78639279ebedee03':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 refs={}
 for fn in ('reference-shader-manifest.json','instancing-reference-manifest.json'):
  for r in json.loads((B/'localization-shared-scrubbed-base'/fn).read_text())['objects']:refs[r['target_id']]=r
 index=defaultdict(list)
 for filename in mod.PINS:
  for o in UnityPy.load(str(BUNDLES/filename)).objects:
   if o.type.name=='Shader':name=o.read_typetree()['m_ParsedForm']['m_Name']
   elif o.type.name=='ComputeShader':name=o.read_typetree()['m_Name']
   else:continue
   index[(o.type.name,name)].append((filename,o.get_raw_data()))
 selected=[];work=bytearray(base)
 for o in UnityPy.load(target).objects:
  if o.type.name not in ('Shader','ComputeShader'):continue
  wanted=o.get_raw_data();old=base[o.byte_start:o.byte_start+o.byte_size]
  if old==wanted:continue
  name=o.read_typetree()['m_ParsedForm']['m_Name'] if o.type.name=='Shader' else o.read_typetree()['m_Name'];ref=refs.get(o.path_id);lo,hi=(ref['reference_start'],ref['reference_end']) if ref else (0,0)
  if any(old[:lo]) or any(old[hi:]):raise ValueError('shader placeholder differs')
  choices=[]
  for filename,value in index[(o.type.name,name)]:
   if len(value)!=len(wanted):continue
   if ref and sha(value).hex()!=ref['generated_sha256']:continue
   candidate=value[:lo]+old[lo:hi]+value[hi:]
   if candidate==wanted:choices.append((filename,value))
  if not choices:raise ValueError('missing generated shader '+name)
  filename,value=sorted(choices,key=lambda x:x[0])[0];selected.append((o.path_id,o.byte_start,len(value),lo,hi,sha(value),sha(wanted),sha(old[lo:hi]),filename,value));work[o.byte_start:o.byte_start+len(value)]=wanted
 if len(selected)!=55:raise ValueError('unexpected shader count')
 OUT.mkdir(parents=True)
 try:
  pack=OUT/'shader-pack.zip'
  with zipfile.ZipFile(pack,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as z:
   for idx,(_,_,_,_,_,_,_,_,_,value) in enumerate(selected):
    info=zipfile.ZipInfo(f'shader-{idx:03d}.bin',(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;z.writestr(info,value)
  packData=pack.read_bytes();plan=bytearray(b'OBS1S001'+struct.pack('>II',len(base),len(selected))+sha(base)+sha(work)+sha(packData))
  for _,dst,size,lo,hi,sourceHash,targetHash,refHash,_,_ in selected:plan.extend(struct.pack('>IIII',dst,size,lo,hi)+sourceHash+targetHash+refHash)
  (OUT/'plan.bin').write_bytes(plan)
  print('plan',sha(plan).hex(),'pack',sha(packData).hex(),'partial',sha(work).hex(),'shaders',len(selected),'bytes',sum(r[2] for r in selected),'references',sum(bool(r[4]) for r in selected))
 except BaseException:
  for p in OUT.iterdir():p.unlink()
  OUT.rmdir();raise
if __name__=='__main__':main()
