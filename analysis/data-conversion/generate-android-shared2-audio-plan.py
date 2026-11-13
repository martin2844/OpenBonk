#!/usr/bin/env python3
"""Generate offset/hash metadata for the second Steam audio resource."""
import collections,hashlib,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip')
PINS=(('e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa'),('cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa'))
OUTPUT=B/'shared2-audio-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 with TARGET.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest()!='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691':raise ValueError('target OBB differs')
 with zipfile.ZipFile(TARGET) as z:
  prefix='assets/bin/Data/sharedassets2.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
  if len(parts)!=22:raise ValueError('unexpected asset splits')
  asset=b''.join(z.read(n) for n in parts);target=z.read('assets/bin/Data/sharedassets2.resource')
 clips=[o.read() for o in UnityPy.load(asset).objects if o.type.name=='AudioClip']
 if len(clips)!=124 or len({c.m_Name for c in clips})!=124:raise ValueError('target inventory mismatch')
 sources=[];resources=[]
 for path,pin in zip(ZIPS,PINS):
  with zipfile.ZipFile(path) as z:asset=z.read('Megabonk_Data/sharedassets2.assets');resource=z.read('Megabonk_Data/sharedassets2.resource')
  if (sha(asset).hex(),sha(resource).hex())!=pin:raise ValueError('Steam mismatch')
  index=collections.defaultdict(list)
  for obj in UnityPy.load(asset).objects:
   if obj.type.name=='AudioClip':
    clip=obj.read();index[clip.m_Name].append(clip)
  sources.append(index);resources.append(resource)
 attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex')
 rows=[];out=bytearray(len(target));end=0
 for clip in sorted(clips,key=lambda c:c.m_Resource.m_Offset):
  ref=clip.m_Resource;dst,size=ref.m_Offset,ref.m_Size
  if ref.m_Source!='sharedassets2.resource' or size<=0 or dst<end or dst-end>=32 or dst+size>len(target):raise ValueError('invalid target range')
  refs=[];values=[]
  for idx,data in zip(sources,resources):
   matches=idx[clip.m_Name]
   if len(matches)!=1 or any(getattr(clip,k,None)!=getattr(matches[0],k,None) for k in attrs):raise ValueError('incompatible source clip')
   src=matches[0].m_Resource
   if src.m_Source!='sharedassets2.resource' or src.m_Size!=size or src.m_Offset<0 or src.m_Offset+size>len(data):raise ValueError('invalid source range')
   refs.append(src.m_Offset);values.append(data[src.m_Offset:src.m_Offset+size])
  if refs[0]!=refs[1] or values[0]!=values[1] or not values[0].startswith(b'FSB5') or values[0]!=target[dst:dst+size]:raise ValueError('audio differs')
  out[dst:dst+size]=values[0];rows.append((dst,refs[0],size,sha(values[0])));end=dst+size
 if end!=len(target) or out!=target:raise ValueError('reconstruction differs')
 plan=bytearray(b'OBSSB001'+struct.pack('>I',len(rows)))
 for pair in PINS:
  for h in pair:plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>I',len(target)));plan.extend(sha(target))
 for dst,src,size,h in rows:plan.extend(struct.pack('>QQI',dst,src,size));plan.extend(h)
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('124 audio clips,',len(target),'bytes; output SHA',sha(target).hex(),'plan',len(plan),'sha256',sha(plan).hex())
if __name__=='__main__':main()
