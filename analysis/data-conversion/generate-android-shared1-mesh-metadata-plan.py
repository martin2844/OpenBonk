#!/usr/bin/env python3
"""Derive exact mesh metadata ranges after the shared1 vertex/index stage."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-mesh-android-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','sharedassets1.assets.resS','level1','level1.resS');MANIFEST=B/'share-source-experiment/full-bc-template/mesh-manifest.json';OUT=B/'shared1-mesh-metadata-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='26df1db6a89a775557ddc9d29ac6bddb10f7f47f7370bd6064dce08cec0b1119':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in NAMES])
  sources.append({n:{o.path_id:o for o in UnityPy.load(files[-1][NAMES.index(n)]).objects} for n in ('sharedassets1.assets','level1')})
 recs=json.loads(MANIFEST.read_text())['meshes'];work=bytearray(base);ranges=[];completed=0
 for rec in recs:
  dst=objects[rec['target_id']];m=dst.read();raw=dst.get_raw_data();v=bytes(m.m_VertexData.m_DataSize);idx=bytes(m.m_IndexBuffer)
  if not v or not idx or raw.count(v)!=1 or raw.count(idx)!=1:raise ValueError('ambiguous mesh buffers')
  intervals=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))))
  if intervals[0][1]>intervals[1][0]:raise ValueError('overlapping mesh buffers')
  spans=((0,intervals[0][0]),(intervals[0][1],intervals[1][0]),(intervals[1][1],len(raw)));src=[s[rec['source_file']][rec['source_id']] for s in sources];raws=[x.get_raw_data() for x in src]
  for lo,hi in spans:
   value=raw[lo:hi]
   if not any(value) or any(r.count(value)!=1 for r in raws):continue
   refs=[];fid=NAMES.index(rec['source_file'])
   for dat,obj,sr in zip(files,src,raws):
    start=obj.byte_start+sr.index(value)
    if dat[fid][start:start+len(value)]!=value:raise ValueError('mesh metadata source mismatch')
    refs.append((fid,start))
   pos=dst.byte_start+lo
   if any(work[pos:pos+len(value)]):raise ValueError('mesh metadata not blank')
   work[pos:pos+len(value)]=value;ranges.append((pos,len(value),refs,sha(value)))
  if work[dst.byte_start:dst.byte_start+len(raw)]==raw:completed+=1
 ranges.sort(key=lambda r:r[0]);out=bytearray(b'OBS1H001'+struct.pack('>II',len(base),len(ranges))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,refs,h in ranges:out.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1])+h)
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'complete objects',completed,'ranges',len(ranges),'bytes',sum(r[1] for r in ranges))
if __name__=='__main__':main()
