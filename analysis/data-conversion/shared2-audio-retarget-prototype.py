#!/usr/bin/env python3
"""Retarget 124 sharedassets2 audio clips to a pinned local Steam installation.

Local experiment: audio and other data remain copyrighted/unreviewed. Outputs
are not a release. Format compatibility must be tested on Android.
"""
import argparse,collections,hashlib,shutil
from pathlib import Path
import UnityPy
PC='e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da';AUDIO='79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa';BASE='b5d6ab07a5ae164453dee414bf892a525b68dd053c9aa08fd80b93f7a75b68cb'
WINDOWS_PC='cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--matched-textures',action='store_true');p.add_argument('--windows-source',action='store_true');a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets2.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets2.resource',AUDIO);require(a.input_asset,'a1b5c15ff723ca7477ee76503916813167d72361b6fd7db43cd71c9c8b02bbc4' if a.matched_textures else BASE)
pc=UnityPy.load(str(a.pc_data/'sharedassets2.assets'));env=UnityPy.load(str(a.input_asset));sources=collections.defaultdict(list)
for o in pc.objects:
 if o.type.name=='AudioClip':
  clip=o.read();sources[clip.m_Name].append(clip)
attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex');targets=[];source_audio=a.pc_data/'sharedassets2.resource'
for o in env.objects:
 if o.type.name!='AudioClip':continue
 d=o.read();matches=sources[d.m_Name]
 if len(matches)!=1:raise ValueError(f'missing or ambiguous clip {d.m_Name}')
 s=matches[0]
 if any(getattr(d,k,None)!=getattr(s,k,None) for k in attrs):raise ValueError(f'audio metadata mismatch {d.m_Name}')
 r=s.m_Resource
 if r.m_Source!='sharedassets2.resource' or r.m_Size<=0 or r.m_Offset+r.m_Size>source_audio.stat().st_size:raise ValueError(f'invalid stream {d.m_Name}')
 targets.append((d,r.m_Offset,r.m_Size))
if len(targets)!=124:raise ValueError('unexpected audio clip count')
a.output_dir.mkdir()
try:
 with source_audio.open('rb') as src,(a.output_dir/'sharedassets2.resource').open('xb') as dest:
  for d,offset,size in targets:
   dest.write(bytes(-dest.tell()%32));new_offset=dest.tell();src.seek(offset);data=src.read(size)
   if len(data)!=size or not data.startswith(b'FSB5'):raise ValueError(f'invalid FSB {d.m_Name}')
   dest.write(data);d.m_Resource.m_Offset=new_offset;d.m_Resource.m_Size=size;d.m_Resource.m_Source='sharedassets2.resource';d.save()
 out=a.output_dir/'sharedassets2.assets';out.write_bytes(env.file.save())
 for n in ('sharedassets2.assets','sharedassets2.resource'):
  x=a.output_dir/n;print(n,x.stat().st_size,sha(x))
 print('Retargeted 124 clips; outputs contain game data and are not redistributable.')
except Exception:shutil.rmtree(a.output_dir);raise
