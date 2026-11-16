#!/usr/bin/env python3
"""Derive the remaining generated-name mesh and video metadata privately."""
import argparse,hashlib,struct,zipfile
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
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--base',type=Path,default=BASE);p.add_argument('--linux-zip',type=Path,default=ZIPS[0]);p.add_argument('--windows-zip',type=Path,default=ZIPS[1]);p.add_argument('--audio-plan',type=Path);p.add_argument('--output',type=Path,default=OUT)
 targets=p.add_mutually_exclusive_group();targets.add_argument('--target-asset',type=Path);targets.add_argument('--target-obb',type=Path);args=p.parse_args();output=args.output
 if output.exists() or output.is_symlink():raise ValueError('output exists')
 base=args.base.read_bytes()
 if sha(base).hex()!='1d55906e02aa2d2f1123082b80e408cf4e3c596fad86632b691f50f6074dd8ce':raise ValueError('invalid base')
 if args.target_asset:
  target=bytearray(args.target_asset.read_bytes())
  if sha(target).hex()!='b6b1d5fd2d261c1163608dfcdf354eca87afbec514840b963a9b5c50ce7fd33e':raise ValueError('reconstructed target asset differs')
  if not args.audio_plan:raise ValueError('reconstructed target requires audio plan')
  audio=args.audio_plan.read_bytes()
  if sha(audio).hex()!='57a8feb4f98d9eb6692c208dd32d9f92556d52b41cb682936b03324945bd9883' or audio[:8]!=b'OBSSA001':raise ValueError('audio plan differs')
  video_offset=struct.unpack_from('>I',audio,144)[0]
  obj=next(o for o in UnityPy.load(bytes(target)).objects if o.path_id==1937);tree=obj.read_typetree();raw=obj.get_raw_data();ref=tree['m_ExternalResources']
  if tree['m_Name']!='Buffering3' or ref['m_Source']!='sharedassets1.resource' or ref['m_Offset']!=28321376 or ref['m_Size']!=8740 or video_offset!=50839968:raise ValueError('video resource differs')
  old=struct.pack('<Q',ref['m_Offset']);pos=raw.find(old)
  if pos<0 or raw.count(old)!=1:raise ValueError('video offset location differs')
  struct.pack_into('<Q',target,obj.byte_start+pos,video_offset)
  if sha(target).hex()!='fc3172d095c9a900af40d7a4a9edd4529743c1556ad0c267fb8bb675f0eb1e84':raise ValueError('rebuilt final target differs')
  target=bytes(target)
 else:
  with zipfile.ZipFile(args.target_obb or TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objs={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in (args.linux_zip,args.windows_zip):
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
 output.parent.mkdir(parents=True,exist_ok=True)
 with output.open('xb') as f:f.write(out)
 print('PRIVATE plan',sha(out).hex(),'complete shared1',sha(work).hex(),'objects',len(rows),'platform edits',*[sum(len(r[-1][i]) for r in rows) for i in range(2)])
if __name__=='__main__':main()
