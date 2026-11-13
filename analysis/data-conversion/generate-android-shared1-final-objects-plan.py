#!/usr/bin/env python3
"""Derive the remaining generated-name mesh and video metadata privately."""
import hashlib,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-outline-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','level1');OUT=B/'shared1-final-objects-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def video(raw,obj):
 path=obj.read_typetree()['m_OriginalPath'].encode();new=b'Assets/VideoClip/'+path.rsplit(b'/',1)[-1];at=raw.find(path)-4
 if at<0 or raw.count(path)!=1 or struct.unpack_from('<I',raw,at)[0]!=len(path):raise ValueError('video path differs')
 end=at+4+((len(path)+3)&~3)
 return raw[:at]+struct.pack('<I',len(new))+new+bytes(-len(new)%4)+raw[end:]
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='1d55906e02aa2d2f1123082b80e408cf4e3c596fad86632b691f50f6074dd8ce':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objs={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in NAMES])
  sources.append([{o.path_id:o for o in UnityPy.load(f).objects} for f in files[-1]])
 rows=[];work=bytearray(base)
 for tid,fid,sid,kind in ((1655,1,15,0),(1937,0,3037,1)):
  dst=objs[tid];wanted=dst.get_raw_data();pos=dst.byte_start;refs=[];hashes=[];edits=[];sourceSize=None
  priorHash=sha(work[pos:pos+len(wanted)])
  for dat,index in zip(files,sources):
   src=index[fid][sid];raw=src.get_raw_data();candidate=video(raw,src) if kind==1 else raw
   if len(candidate)!=len(wanted):raise ValueError('object size differs')
   patch=[(i,y) for i,(x,y) in enumerate(zip(candidate,wanted)) if x!=y]
   if len(patch)>16:raise ValueError('too many final edits')
   refs.append(src.byte_start);hashes.append(sha(raw));edits.append(patch)
   if sourceSize is None:sourceSize=len(raw)
   elif sourceSize!=len(raw):raise ValueError('platform source size differs')
  work[pos:pos+len(wanted)]=wanted;rows.append((pos,len(wanted),sourceSize,fid,kind,refs,hashes,sha(wanted),priorHash,edits))
 if bytes(work)!=target:raise ValueError('shared1 still differs')
 rows.sort();out=bytearray(b'OBS1F001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,sourceSize,fid,kind,refs,hashes,targetHash,priorHash,edits in rows:
  out.extend(struct.pack('>IIIIIQQ',dst,size,sourceSize,fid,kind,*refs)+b''.join(hashes)+targetHash+priorHash)
  for patch in edits:
   out.extend(struct.pack('>I',len(patch)))
   for off,value in patch:out.extend(struct.pack('>IB',off,value))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('PRIVATE plan',sha(out).hex(),'complete shared1',sha(work).hex(),'objects',len(rows),'platform edits',*[sum(len(r[-1][i]) for r in rows) for i in range(2)])
if __name__=='__main__':main()
