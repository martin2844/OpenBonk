#!/usr/bin/env python3
"""Map two font atlases and the gamepad texture to pinned Steam data."""
import hashlib,importlib.util,struct,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
spec=importlib.util.spec_from_file_location('exact',Path(__file__).with_name('generate-android-texture-copy-plan.py'));exact=importlib.util.module_from_spec(spec);spec.loader.exec_module(exact)
BASE=B/'shared2-animation-android-plan/partial.assets';OUTPUT=B/'shared2-final-textures-plan/plan.bin'
PAIRS=((167,'sharedassets2.assets',110),(171,'sharedassets1.assets',989),(179,'sharedassets2.assets',188))
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='fd01cab3e17c0367d57d1319b884082ad70b0df5646d456885e2483d6f400a13':raise ValueError('base differs')
 if exact.file_sha(exact.TARGET)!=exact.TARGET_SHA:raise ValueError('target differs')
 with zipfile.ZipFile(exact.TARGET) as z:target=b''.join(z.read('assets/bin/Data/sharedassets2.assets.split'+str(i)) for i in range(22))
 objects={o.path_id:o for o in UnityPy.load(target).objects};before={o.path_id:o for o in UnityPy.load(base).objects};files=[];sources=[]
 for path,pins in ((exact.LINUX,exact.LINUX_SHA),(exact.WINDOWS,exact.WINDOWS_SHA)):
  with zipfile.ZipFile(path) as z:data=[z.read('Megabonk_Data/'+n) for n in exact.NAMES]
  if tuple(sha(b).hex() for b in data)!=pins:raise ValueError('Steam differs')
  files.append(data);sources.append({n:{o.path_id:o for o in UnityPy.load(data[exact.NAMES.index(n)]).objects} for n in ('sharedassets1.assets','sharedassets2.assets')})
 rows=[];work=bytearray(base)
 for tid,file,sid in PAIRS:
  dst=objects[tid];old=before[tid];d=dst.read();raw=dst.get_raw_data();original=old.get_raw_data();pixels=bytes(d.image_data);header=raw.find(pixels)
  if dst.type.name!='Texture2D' or dst.byte_start!=old.byte_start or len(raw)!=len(original) or any(x and x!=y for x,y in zip(original,raw)) or not pixels or raw.count(pixels)!=1 or raw[header+len(pixels):]!=bytes(16):raise ValueError('invalid target')
  refs=[];headers=[];images=[]
  for data,index in zip(files,sources):
   obj=index[file][sid];s=obj.read();stream=s.m_StreamData
   if not stream or stream.path not in exact.NAMES:raise ValueError('invalid texture stream')
   fid=exact.NAMES.index(stream.path);image=data[fid][stream.offset:stream.offset+stream.size];headerData=obj.get_raw_data()[:header]
   if s.m_Name!=d.m_Name or (s.m_Width,s.m_Height,s.m_MipCount)!=(d.m_Width,d.m_Height,d.m_MipCount) or len(headerData)!=header or d.m_TextureFormat!=47:raise ValueError('metadata mismatch')
   refs.append((exact.NAMES.index(file),obj.byte_start,fid,stream.offset));headers.append(headerData);images.append(image)
  if headers[0]!=headers[1] or images[0]!=images[1] or refs[0][0]!=refs[1][0] or refs[0][2]!=refs[1][2]:raise ValueError('platform mismatch')
  nameEnd=4+((len(d.m_Name.encode())+3)&~3);h=bytearray(headers[0]);struct.pack_into('<I',h,nameEnd+12,len(pixels));struct.pack_into('<I',h,nameEnd+20,d.m_TextureFormat);h[nameEnd+30]=int(d.m_IgnoreMipmapLimit);struct.pack_into('<I',h,nameEnd+76,d.m_LightmapFormat);struct.pack_into('<I',h,header-4,len(pixels))
  if h!=raw[:header] or s.m_TextureFormat not in (1,12):raise ValueError('derived header differs')
  chunks=[];off=0
  for lv in range(d.m_MipCount):
   w=max(1,d.m_Width>>lv);height=max(1,d.m_Height>>lv);n=w*height if s.m_TextureFormat==1 else ((w+3)//4)*((height+3)//4)*16
   image=C.parse_image_data(images[0][off:off+n],w,height,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA');value,fmt=C.image_to_texture2d(image,47,d.object_reader.platform,d.m_PlatformBlob)
   if int(fmt)!=47:raise ValueError('bad converted format')
   chunks.append(value);off+=n
  if off!=len(images[0]) or b''.join(chunks)!=pixels:raise ValueError('texture pixels differ')
  work[dst.byte_start:dst.byte_start+len(raw)]=raw
  rows.append((dst.byte_start,header,len(pixels),d.m_Width,d.m_Height,d.m_MipCount,s.m_TextureFormat,d.m_TextureFormat,d.m_LightmapFormat,int(d.m_IgnoreMipmapLimit),refs[0][0],refs[0][1],refs[1][1],refs[0][2],refs[0][3],refs[1][3],len(images[0]),sha(headers[0]),sha(images[0]),sha(raw)))
 if bytes(work)!=target:raise ValueError('shared2 asset still differs: '+str(sum(x!=y for x,y in zip(work,target))))
 plan=bytearray(b'OBSF2001'+struct.pack('>I',len(rows)))
 for h in (*exact.LINUX_SHA,*exact.WINDOWS_SHA):plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>I',len(base)));plan.extend(sha(base));plan.extend(sha(work))
 for r in rows:plan.extend(struct.pack('>IIIIIIIIIIIQQIQQI',*r[:17]));plan.extend(b''.join(r[17:]))
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('3 final textures; COMPLETE shared2 sha',sha(work).hex(),'plan sha',sha(plan).hex())
if __name__=='__main__':main()
