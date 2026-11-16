#!/usr/bin/env python3
"""Build an offset/hash plan for the main Steam audio resource and video."""
import argparse,collections,hashlib,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
TARGET_SHA='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip')
PINS=(('d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'),('64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d'))
OUTPUT=B/'shared-audio-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 p=argparse.ArgumentParser(description=__doc__)
 source=p.add_mutually_exclusive_group()
 source.add_argument('--target-obb',type=Path,help='pinned historical target OBB')
 source.add_argument('--rebuilt-assets',type=Path,help='private rebuilt sharedassets1 files; reconstruct the video from Steam')
 p.add_argument('--linux-zip',type=Path,default=ZIPS[0]);p.add_argument('--windows-zip',type=Path,default=ZIPS[1]);p.add_argument('--output',type=Path,default=OUTPUT)
 a=p.parse_args();output=a.output.absolute()
 if a.output==OUTPUT:output.parent.mkdir(parents=True,exist_ok=True)
 if output.exists() or output.is_symlink() or not output.parent.is_dir():p.error('output must be new and its parent must exist')
 paths=[a.linux_zip.resolve(strict=True),a.windows_zip.resolve(strict=True)]
 source_path=(a.rebuilt_assets or a.target_obb or TARGET).resolve(strict=True)
 output=output.parent.resolve()/output.name
 if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (source_path,*paths)):p.error('output must be separate from inputs')
 if a.rebuilt_assets:
  if not source_path.is_dir():p.error('rebuilt assets must be a directory')
  asset=(source_path/'sharedassets1.assets').read_bytes();audio=(source_path/'sharedassets1.resource').read_bytes()
  if sha(asset).hex()!='b10fd4de8a0da336f39dff3dddbab994499b229fa5b3f443ab8adbcc16b2082e' or sha(audio).hex()!='93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6':raise ValueError('rebuilt asset differs')
  with zipfile.ZipFile(paths[0]) as z:resource=z.read('Megabonk_Data/sharedassets1.resource')
  if sha(resource).hex()!=PINS[0][1]:raise ValueError('Steam resource differs')
  target=audio+resource[60836960:60845700]
  if sha(target).hex()!='28f275bd17593fbffc218edb0b66e1458b13d887456c200074a70b2927c2d9f6':raise ValueError('reconstructed audio/video resource differs')
 else:
  with source_path.open('rb') as f:
   if hashlib.file_digest(f,'sha256').hexdigest()!=TARGET_SHA:raise ValueError('target OBB differs')
  with zipfile.ZipFile(source_path) as z:
   prefix='assets/bin/Data/sharedassets1.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
   if len(parts)!=117:raise ValueError('unexpected shared asset splits')
   asset=b''.join(z.read(n) for n in parts);target=z.read('assets/bin/Data/sharedassets1.resource')
 objects=UnityPy.load(asset).objects;targets=[o.read() for o in objects if o.type.name=='AudioClip']
 if len(targets)!=118 or len({c.m_Name for c in targets})!=118:raise ValueError('target clip inventory mismatch')
 sources=[];resources=[]
 for path,pin in zip(paths,PINS):
  with zipfile.ZipFile(path) as z:asset=z.read('Megabonk_Data/sharedassets1.assets');resource=z.read('Megabonk_Data/sharedassets1.resource')
  if (sha(asset).hex(),sha(resource).hex())!=pin:raise ValueError('Steam source mismatch')
  index=collections.defaultdict(list)
  for obj in UnityPy.load(asset).objects:
   if obj.type.name=='AudioClip':
    c=obj.read();index[c.m_Name].append(c)
  sources.append(index);resources.append(resource)
 attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex')
 rows=[];rebuilt=bytearray(len(target));end=0
 for clip in sorted(targets,key=lambda c:c.m_Resource.m_Offset):
  ref=clip.m_Resource;dst,size=ref.m_Offset,ref.m_Size
  if ref.m_Source!='sharedassets1.resource' or size<=0 or dst<end or dst-end>=32 or dst+size>len(target):raise ValueError('bad target range')
  offsets=[];values=[]
  for index,data in zip(sources,resources):
   matches=index[clip.m_Name]
   if len(matches)!=1 or any(getattr(clip,k,None)!=getattr(matches[0],k,None) for k in attrs):raise ValueError('audio metadata mismatch')
   src=matches[0].m_Resource
   if src.m_Source!='sharedassets1.resource' or src.m_Size!=size or src.m_Offset<0 or src.m_Offset+size>len(data):raise ValueError('bad Steam audio range')
   offsets.append(src.m_Offset);values.append(data[src.m_Offset:src.m_Offset+size])
  if values[0]!=values[1] or not values[0].startswith(b'FSB5') or values[0]!=target[dst:dst+size]:raise ValueError('audio payload differs')
  rebuilt[dst:dst+size]=values[0];rows.append((dst,offsets[0],offsets[1],size,sha(values[0])));end=dst+size
 video=bytearray(resources[0][60836960:60845700]);windowsVideo=bytearray(resources[1][60836960:60845700])
 if windowsVideo[252:259]!=b'\xb4'*7:raise ValueError('unexpected Windows video')
 windowsVideo[252:259]=b'\x70'*7
 if video!=windowsVideo or bytes(video)!=target[end:] or len(video)!=8740:raise ValueError('video or end layout differs')
 rebuilt[end:]=video
 if rebuilt!=target:raise ValueError('resource reconstruction differs')
 plan=bytearray(b'OBSSA001'+struct.pack('>I',len(rows)))
 for pair in PINS:
  for h in pair:plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>II',len(target),end));plan.extend(sha(target));plan.extend(sha(video))
 for dst,linux,windows,size,h in rows:plan.extend(struct.pack('>QQQI',dst,linux,windows,size));plan.extend(h)
 with output.open('xb') as f:f.write(plan)
 print('118 Steam clips + video,',len(target),'bytes; SHA256',sha(target).hex(),'plan',len(plan),'sha256',sha(plan).hex(),'offsets and hashes only')
if __name__=='__main__':main()
