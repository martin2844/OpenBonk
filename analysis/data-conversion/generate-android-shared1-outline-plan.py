#!/usr/bin/env python3
"""Derive two outline meshes from Steam vertex data and computed normals."""
import collections,hashlib,math,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-cubemap-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');OUT=B/'shared1-outline-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def generate(raw,count):
 groups=collections.defaultdict(list)
 for i in range(count):groups[struct.unpack_from('<3f',raw,32*i)].append(i)
 smooth=[None]*count
 for indices in groups.values():
  result=struct.unpack_from('<3f',raw,32*indices[0]+12)
  if len(indices)>1:
   sums=[0.,0.,0.]
   for i in indices:
    n=struct.unpack_from('<3f',raw,32*i+12)
    for j in range(3):sums[j]=f32(sums[j]+n[j])
   length=f32(math.sqrt(f32(f32(f32(sums[0]*sums[0])+f32(sums[1]*sums[1]))+f32(sums[2]*sums[2]))))
   result=tuple(f32(x/length) for x in sums) if length>1e-5 else (0.,0.,0.)
  for i in indices:smooth[i]=result
 out=bytearray(count*44)
 for i in range(count):out[i*44:i*44+32]=raw[i*32:i*32+32];struct.pack_into('<3f',out,i*44+32,*smooth[i])
 return bytes(out)
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='fde2b6fe5f08ba2cdcd4edb120a29e4b7c2cf2f5ea5537d8c5dfda947481cf0c':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append(z.read('Megabonk_Data/sharedassets1.assets'))
  sources.append({o.path_id:o for o in UnityPy.load(files[-1]).objects})
 work=bytearray(base);rows=[]
 for tid,sid,name,count in ((1559,1482,'Cube',2600),(1641,1827,'lp.obj',493)):
  obj=objects[tid];m=obj.read();raw=obj.get_raw_data();vertex=bytes(m.m_VertexData.m_DataSize);vp=raw.index(vertex)
  if m.m_Name!=name or len(vertex)!=count*44 or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target mesh differs')
  refs=[];hashes=[];patches=[]
  for data,index in zip(files,sources):
   src=index[sid];sm=src.read();sr=src.get_raw_data();sv=bytes(sm.m_VertexData.m_DataSize);sp=sr.index(sv)
   if sm.m_Name!=name or len(sv)!=count*32 or sp!=vp or generate(sv,count)!=vertex:raise ValueError('source vertices differ')
   candidate=bytearray(sr[:sp]+vertex+sr[sp+len(sv):]);diff=[(i,x) for i,(x,y) in enumerate(zip(raw,candidate)) if x!=y]
   if len(candidate)!=len(raw) or len(diff)>8 or any(vp<=i<vp+len(vertex) for i,_ in diff):raise ValueError('unexpected outline differences')
   for i,x in diff:candidate[i]=x
   if candidate!=raw:raise ValueError('outline reconstruction differs')
   refs.append(src.byte_start);hashes.append(sha(sr));patches.append(diff)
  work[obj.byte_start:obj.byte_start+len(raw)]=raw;rows.append((obj.byte_start,len(raw),len(sr),vp,count,refs,hashes,sha(raw),patches))
 rows.sort();out=bytearray(b'OBS1O001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for f in files:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,sourceSize,vp,count,refs,hashes,targetHash,patches in rows:
  out.extend(struct.pack('>IIIIIQQ',dst,size,sourceSize,vp,count,*refs)+b''.join(hashes)+targetHash)
  for patch in patches:
   out.extend(struct.pack('>I',len(patch)))
   for pos,value in patch:out.extend(struct.pack('>IB',pos,value))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'outline meshes',len(rows))
if __name__=='__main__':main()
