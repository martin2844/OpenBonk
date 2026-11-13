#!/usr/bin/env python3
"""Generate an offset-only plan for two second-map animation objects."""
import hashlib,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'shared2-texture-android-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';OUTPUT=B/'shared2-animation-android-plan/plan.bin'
PINS=('e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9')
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='8835049aa6e69f1fe689150ab004f749d9a8fb7bcbe0ac1176352b2d1d33e32f':raise ValueError('base differs')
 with TARGET.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest()!='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691':raise ValueError('target OBB differs')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read('assets/bin/Data/sharedassets2.assets.split'+str(i)) for i in range(22))
 dest={o.path_id:o for o in UnityPy.load(target).objects};blank={o.path_id:o for o in UnityPy.load(base).objects};sources=[];data=[]
 for path,pin in zip((B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip'),PINS):
  with zipfile.ZipFile(path) as z:value=z.read('Megabonk_Data/sharedassets2.assets')
  if sha(value).hex()!=pin:raise ValueError('Steam asset differs')
  data.append(value);sources.append({o.path_id:o for o in UnityPy.load(value).objects})
 rows=[];work=bytearray(base)
 for tid,sid,name in ((455,247,'MonkeBreakdance'),(456,249,'MovingBushQuest')):
  obj=dest[tid];old=blank[tid];raw=obj.get_raw_data()
  prior=old.get_raw_data()
  if obj.type.name!='AnimationClip' or old.byte_start!=obj.byte_start or len(prior)!=len(raw) or any(x and x!=y for x,y in zip(prior,raw)) or obj.read().m_Name!=name:raise ValueError('invalid target')
  offsets=[]
  for b,index in zip(data,sources):
   src=index[sid]
   if src.type.name!='AnimationClip' or src.read().m_Name!=name or src.get_raw_data()!=raw or b[src.byte_start:src.byte_start+len(raw)]!=raw:raise ValueError('Steam animation differs')
   offsets.append(src.byte_start)
  work[obj.byte_start:obj.byte_start+len(raw)]=raw;rows.append((obj.byte_start,len(raw),*offsets,sha(raw)))
 plan=bytearray(b'OBSA2001'+struct.pack('>I',len(rows)))
 for pin in PINS:plan.extend(bytes.fromhex(pin))
 plan.extend(struct.pack('>I',len(base)));plan.extend(sha(base));plan.extend(sha(work));plan.extend(sha(target))
 for dst,size,linux,windows,h in rows:plan.extend(struct.pack('>IIQQ',dst,size,linux,windows));plan.extend(h)
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('2 animation objects,',sum(x[1] for x in rows),'bytes; remaining differing bytes',sum(x!=y for x,y in zip(work,target)),'intermediate sha',sha(work).hex(),'plan sha',sha(plan).hex())
if __name__=='__main__':main()
