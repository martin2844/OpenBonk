#!/usr/bin/env python3
"""Map shared1 mesh vertex/index ranges back to the two Steam depots."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-material-audio-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','sharedassets1.assets.resS','level1','level1.resS');MANIFEST=B/'share-source-experiment/full-bc-template/mesh-manifest.json';OUT=B/'shared1-mesh-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='fa2ef744b54d702abf75835b03a161da3db635c7facfd03cde3dece61f5d3288':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in NAMES])
  sources.append({n:{o.path_id:o for o in UnityPy.load(files[-1][NAMES.index(n)]).objects} for n in ('sharedassets1.assets','level1')})
 recs=json.loads(MANIFEST.read_text())['meshes']
 if len(recs)!=420 or len({r['target_id'] for r in recs})!=420:raise ValueError('invalid inventory')
 work=bytearray(base);ranges=[]
 for rec in recs:
  dst=objects[rec['target_id']];obj=dst.read();raw=dst.get_raw_data()
  if dst.type.name!='Mesh':raise ValueError('target type differs')
  for field in ('vertices','indices'):
   value=bytes(obj.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(obj.m_IndexBuffer or [])
   if len(value)!=rec[field+'_size'] or sha(value).hex()!=rec[field+'_sha256'] or not value or raw.count(value)!=1:raise ValueError('target buffer mismatch')
   offset=dst.byte_start+raw.index(value);refs=[]
   for source,index in zip(files,sources):
    src=index[rec['source_file']][rec['source_id']];m=src.read();srcRaw=src.get_raw_data();payload=bytes(m.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(m.m_IndexBuffer or [])
    if field=='vertices' and not payload:
     stream=m.m_StreamData
     if not stream or stream.path not in NAMES:raise ValueError('unknown mesh stream')
     fid=NAMES.index(stream.path);start=stream.offset;payload=source[fid][start:start+stream.size]
    else:
     if not payload or srcRaw.count(payload)!=1:raise ValueError('ambiguous mesh source')
     fid=NAMES.index(rec['source_file']);start=src.byte_start+srcRaw.index(payload)
    if payload!=value or source[fid][start:start+len(value)]!=value:raise ValueError('Steam geometry differs')
    refs.append((fid,start))
   if any(work[offset:offset+len(value)]):raise ValueError('mesh range not blank')
   work[offset:offset+len(value)]=value;ranges.append((offset,len(value),refs,sha(value)))
 ranges.sort(key=lambda r:r[0]);out=bytearray(b'OBS1G001'+struct.pack('>II',len(base),len(ranges))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,refs,h in ranges:out.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1])+h)
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'meshes',len(recs),'ranges',len(ranges),'bytes',sum(r[1] for r in ranges))
if __name__=='__main__':main()
