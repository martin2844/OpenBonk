#!/usr/bin/env python3
"""Derive additional mesh metadata and Android readability flags."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-mesh-metadata-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','sharedassets1.assets.resS','level1','level1.resS');MANIFEST=B/'share-source-experiment/full-bc-template/mesh-manifest.json';OUT=B/'shared1-mesh-flags-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='76991a41618218070f29fe7f3dc273d6bc78dd777d690d9636ce55de70a28464':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in NAMES])
  sources.append({n:{o.path_id:o for o in UnityPy.load(files[-1][NAMES.index(n)]).objects} for n in ('sharedassets1.assets','level1')})
 recs=json.loads(MANIFEST.read_text())['meshes'];work=bytearray(base);ranges=[];flags=[];completed=0
 for rec in recs:
  dst=objects[rec['target_id']];raw=dst.get_raw_data();start=dst.byte_start
  if work[start:start+len(raw)]==raw:continue
  m=dst.read();v=bytes(m.m_VertexData.m_DataSize);idx=bytes(m.m_IndexBuffer);a=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))))
  spans=((0,a[0][0]),(a[0][1],a[1][0]),(a[1][1],len(raw)));selected=[s[rec['source_file']][rec['source_id']] for s in sources];srcRaw=[o.get_raw_data() for o in selected];prefix=raw[:spans[0][1]];diff=set();valid=True
  for sr in srcRaw:
   if len(sr)<len(prefix):valid=False;break
   d=[i for i in range(len(prefix)) if sr[i]!=prefix[i]]
   if len(d)>3 or any(sr[i]!=0 or prefix[i]!=1 for i in d):valid=False;break
   diff.update(d)
  if not valid or len(diff)>3:continue
  fid=NAMES.index(rec['source_file'])
  def commit(lo,hi,offsets):
   value=raw[lo:hi];pos=start+lo
   if not any(value) or work[pos:pos+len(value)]==value:return
   if any(work[pos:pos+len(value)]):raise ValueError('partial metadata range')
   refs=[]
   for data,obj,offset in zip(files,selected,offsets):
    sourceOffset=obj.byte_start+offset
    if data[fid][sourceOffset:sourceOffset+len(value)]!=value:raise ValueError('metadata source differs')
    refs.append((fid,sourceOffset))
   work[pos:pos+len(value)]=value;ranges.append((pos,len(value),refs,sha(value)))
  cursor=0
  for i in sorted(diff)+[len(prefix)]:
   if cursor<i:commit(cursor,i,[cursor,cursor])
   cursor=i+1
  for i in sorted(diff):
   if work[start+i]==0:work[start+i]=1;flags.append(start+i)
   elif work[start+i]!=1:raise ValueError('flag target differs')
  for lo,hi in spans[1:]:
   value=raw[lo:hi]
   if not any(value) or any(sr.count(value)!=1 for sr in srcRaw):continue
   commit(lo,hi,[sr.index(value) for sr in srcRaw])
  if work[start:start+len(raw)]==raw:completed+=1
 ranges.sort(key=lambda r:r[0]);flags.sort();out=bytearray(b'OBS1J001'+struct.pack('>II',len(base),len(ranges))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,refs,h in ranges:out.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1])+h)
 out.extend(struct.pack('>I',len(flags)))
 for off in flags:out.extend(struct.pack('>I',off))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'extra complete objects',completed,'ranges',len(ranges),'flags',len(flags))
if __name__=='__main__':main()
