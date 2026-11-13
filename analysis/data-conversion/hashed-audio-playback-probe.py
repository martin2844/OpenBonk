#!/usr/bin/env python3
"""Redirect Forest2_Intro to a candidate Steam FSB for an isolated audio test.

The resulting asset and resource contain game data, not distributable. This
changes a track only in the temporary diagnostic, not the game project.
"""
import hashlib,sys
from pathlib import Path
import UnityPy

def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def req(p,d):
 if sha(p)!=d:raise ValueError('incompatible input '+str(p))
if len(sys.argv)!=5:raise SystemExit('usage: hashed-audio-playback-probe.py ASSET RESOURCE CANDIDATE OUTPUT_DIR')
a,r,c,out=map(Path,sys.argv[1:])
if out.exists() or not out.parent.is_dir():raise ValueError('invalid output')
req(a,'b10fd4de8a0da336f39dff3dddbab994499b229fa5b3f443ab8adbcc16b2082e');req(r,'93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6');req(c,'7fd10cf09133ad4fdf2650406f8d86d1dc41d1ee7a58469d6943baf95552f9cf')
e=UnityPy.load(str(a));matches=[o.read() for o in e.objects if o.type.name=='AudioClip' and o.read().m_Name=='Forest2_Intro']
if len(matches)!=1:raise ValueError('unexpected Forest2 intro')
d=matches[0];data=r.read_bytes();candidate=c.read_bytes();pad=bytes(-len(data)%32);offset=len(data)+len(pad)
if not candidate.startswith(b'FSB5') or d.m_Frequency!=44100 or d.m_Channels!=2:raise ValueError('unexpected format')
d.m_Length=7.00877571105957;d.m_Resource.m_Source='sharedassets1.resource';d.m_Resource.m_Offset=offset;d.m_Resource.m_Size=len(candidate);d.save();out.mkdir();(out/'sharedassets1.assets').write_bytes(e.file.save());(out/'sharedassets1.resource').write_bytes(data+pad+candidate)
check=UnityPy.load(str(out/'sharedassets1.assets'));x=[o.read() for o in check.objects if o.type.name=='AudioClip' and o.read().m_Name=='Forest2_Intro']
if len(x)!=1 or x[0].m_Resource.m_Offset!=offset or x[0].m_Resource.m_Size!=len(candidate):raise ValueError('output verification failed')
print('Forest2_Intro references exact Desert1_Intro Steam stream, offset',offset,'size',len(candidate),'sha256',sha(c))
