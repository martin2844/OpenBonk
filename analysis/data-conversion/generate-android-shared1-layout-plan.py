#!/usr/bin/env python3
"""Derive the resized blank texture layout of the private shared1 base."""
import hashlib,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'localization-shared-scrubbed-base/base.obb';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';OUT=B/'shared1-layout-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def asset(path):
 with zipfile.ZipFile(path) as z:
  names=sorted((n for n in z.namelist() if n.startswith('assets/bin/Data/sharedassets1.assets.split')),key=lambda n:int(n.rsplit('split',1)[1]))
  return b''.join(z.read(n) for n in names)
def main():
 if OUT.exists():raise ValueError('output exists')
 original=asset(BASE);target=asset(TARGET);env=UnityPy.load(original);a={o.path_id:o for o in env.objects};b={o.path_id:o for o in UnityPy.load(target).objects}
 if len(original)!=112604540 or len(target)!=122379840 or len(a)!=6368 or a.keys()!=b.keys() or env.file.header.data_offset!=194384 or env.file.header.endian!='<':raise ValueError('unexpected asset structure')
 records=[]
 for k,o in sorted(a.items()):
  t=b[k];needle=struct.pack('<qqIi',k,o.byte_start-194384,o.byte_size,o.type_id)
  if original[:194384].count(needle)!=1:raise ValueError('ambiguous object table')
  if o.byte_size!=t.byte_size:
   if o.type.name!='Texture2D' or any(o.get_raw_data()):raise ValueError('nonblank resized object')
   records.append((k,o.byte_size,t.byte_size));o.set_raw_data(bytes(t.byte_size))
 if len(records)!=295:raise ValueError('unexpected resized inventory')
 result=env.file.save();new={o.path_id:o for o in UnityPy.load(result).objects}
 if len(result)!=len(target) or result[:194384]!=target[:194384] or any(new[k].byte_start!=b[k].byte_start or new[k].byte_size!=b[k].byte_size for k in b):raise ValueError('generated layout differs')
 plan=bytearray(b'OBS1L001'+struct.pack('>III',len(original),len(result),len(records))+sha(original)+sha(result))
 for row in records:plan.extend(struct.pack('>qII',*row))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(plan)
 print('plan',sha(plan).hex(),'base',sha(original).hex(),'skeleton',sha(result).hex(),'resized',len(records))
if __name__=='__main__':main()
