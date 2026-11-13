#!/usr/bin/env python3
"""Derive the last two sharedassets0 texture conversions from Steam."""
import hashlib,struct,zipfile
from pathlib import Path
import UnityPy

ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'shared0-metadata-android-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=[B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip'];OUT=B/'shared0-final-textures-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='b3fd081f4d14cebc8fbadfaf0984802780a8f6e98895078d02c69b8b0bcefbfb':raise ValueError('incorrect base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets0.assets.split{i}') for i in range(126))
 targets={o.path_id:o for o in UnityPy.load(target).objects};data=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:data.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets0.assets','sharedassets0.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(data[-1][0]).objects})
 result=bytearray(base);rows=[]
 for tid,sid,name,w,mips,fmt in [(55,77,'Fox_Default',256,9,4),(64,74,'Emission',128,8,10)]:
  dst=targets[tid];d=dst.read();raw=dst.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels)
  if d.m_Name!=name or (d.m_Width,d.m_Height,d.m_MipCount,d.m_TextureFormat)!=(w,w,mips,45) or head<80 or raw.count(pixels)!=1 or raw[head+len(pixels):]!=bytes(16) or any(base[dst.byte_start:dst.byte_start+len(raw)]):raise ValueError('target mismatch')
  refs=[];headers=[];images=[]
  for files,index in zip(data,sources):
   obj=index[sid];s=obj.read();sr=obj.get_raw_data();stream=s.m_StreamData;img=files[1][stream.offset:stream.offset+stream.size];hdr=sr[:head]
   if s.m_Name!=name or (s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat)!=(w,w,mips,fmt) or stream.path!='sharedassets0.assets.resS' or not img:raise ValueError('source mismatch')
   refs.append((obj.byte_start,stream.offset));headers.append(hdr);images.append(img)
  if headers[0]!=headers[1] or images[0]!=images[1]:raise ValueError('platform source mismatch')
  h=bytearray(headers[0]);end=4+((len(name.encode())+3)&~3);struct.pack_into('<I',h,end+12,len(pixels));struct.pack_into('<I',h,end+20,45);struct.pack_into('<I',h,head-4,len(pixels))
  if bytes(h)!=raw[:head]:raise ValueError('target header mismatch')
  result[dst.byte_start:dst.byte_start+len(raw)]=raw
  rows.append((dst.byte_start,head,len(pixels),w,mips,fmt,refs[0][0],refs[1][0],refs[0][1],refs[1][1],len(images[0]),sha(headers[0]),sha(images[0]),sha(raw)))
 if result!=target:raise ValueError('shared0 target still differs')
 out=bytearray(b'OBS0T001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(result))
 for platform in data:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:out.extend(struct.pack('>IIIIIIQQQQI',*r[:11])+b''.join(r[11:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'complete asset',sha(result).hex())
if __name__=='__main__':main()
