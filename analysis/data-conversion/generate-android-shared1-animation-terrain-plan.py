#!/usr/bin/env python3
"""Build private terrain/animation patches; Android-derived bytes remain restricted."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-shader-pack/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');MANIFEST=B/'local-research-manifests/shared1-terrain-animation-template/objects-manifest.json';OUT=B/'shared1-animation-terrain-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='575780f50d87cb9183d398161dabcdb5566c87f3d4f5bb22922716a550862715':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append(z.read('Megabonk_Data/sharedassets1.assets'))
  sources.append({o.path_id:o for o in UnityPy.load(files[-1]).objects})
 records=json.loads(MANIFEST.read_text())['objects'];work=bytearray(base);rows=[]
 if len(records)!=13:raise ValueError('unexpected object inventory')
 for r in records:
  obj=objects[r['target_id']];wanted=obj.get_raw_data();offset=obj.byte_start
  if obj.type.name!=r['type'] or len(wanted)!=r['size'] or sha(wanted).hex()!=r['target_sha256'] or any(work[offset:offset+len(wanted)]):raise ValueError('target object differs')
  srcOffsets=[];hashes=[];edits=[]
  for data,index in zip(files,sources):
   source=index[r['source_id']];raw=source.get_raw_data();start=source.byte_start
   if source.type.name!=r['type'] or len(raw)!=len(wanted) or data[start:start+len(raw)]!=raw:raise ValueError('source object differs')
   patch=[(i,x) for i,(x,y) in enumerate(zip(wanted,raw)) if x!=y]
   if len(patch)>12000:raise ValueError('too many edits')
   srcOffsets.append(start);hashes.append(sha(raw));edits.append(patch)
  work[offset:offset+len(wanted)]=wanted;rows.append((offset,len(wanted),srcOffsets,hashes,sha(wanted),edits))
 rows.sort();out=bytearray(b'OBS1R001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for f in files:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,offsets,hashes,targetHash,edits in rows:
  out.extend(struct.pack('>IIQQ',dst,size,*offsets)+b''.join(hashes)+targetHash)
  for patch in edits:
   out.extend(struct.pack('>I',len(patch)))
   for pos,value in patch:out.extend(struct.pack('>IB',pos,value))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('PRIVATE plan',sha(out).hex(),'partial',sha(work).hex(),'objects',len(rows),'platform edits',*[sum(len(r[-1][i]) for r in rows) for i in range(2)])
if __name__=='__main__':main()
