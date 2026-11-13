#!/usr/bin/env python3
"""Map three DXT5nm textures to the experimental Android reference."""
import hashlib,struct,sys,zipfile
from pathlib import Path
import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-three-textures-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');OUT=B/'shared1-normal-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 water=len(sys.argv)==2 and sys.argv[1]=='water'
 if len(sys.argv)>2 or len(sys.argv)==2 and not water:raise ValueError('usage: generator [water]')
 global OUT
 if water:OUT=B/'shared1-water-plan/plan.bin'
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='407cc3a2b9644960eff549169c15d8515d5a59da615fca50e7479b717f1c7305':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets1.assets','sharedassets1.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(files[-1][0]).objects})
 work=bytearray(base);rows=[];lut=bytes(round(max(0,1-(x/127.5-1)**2-(y/127.5-1)**2)**.5*127.5+127.5) for x in range(256) for y in range(256))
 pairs=((1072,963,'IL3DN_WaterNormal'),) if water else ((802,1044,'Tiles1_Normal'),(870,764,'Tiles4_Normal'),(1072,963,'IL3DN_WaterNormal'))
 for tid,sid,name in pairs:
  obj=objects[tid];d=obj.read();raw=obj.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels)
  if d.m_Name!=name or d.m_TextureFormat not in (4,34) or head<80 or raw.count(pixels)!=1 or raw[head+len(pixels):]!=bytes(16) or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target differs '+name)
  refs=[];heads=[];imgs=[]
  for dat,index in zip(files,sources):
   src=index[sid];s=src.read();sr=src.get_raw_data();stream=s.m_StreamData;img=dat[1][stream.offset:stream.offset+stream.size]
   if s.m_Name!=name or s.m_TextureFormat!=12 or (s.m_Width,s.m_Height,s.m_MipCount)!=(d.m_Width,d.m_Height,d.m_MipCount) or stream.path!='sharedassets1.assets.resS':raise ValueError('Steam normal differs')
   refs.append((src.byte_start,1,stream.offset));heads.append(sr[:head]);imgs.append(img)
  if heads[0]!=heads[1] or imgs[0]!=imgs[1]:raise ValueError('platform source differs')
  h=bytearray(heads[0]);end=4+((len(name.encode())+3)&~3)
  if struct.unpack_from('<I',h,end)[0]!=0:raise ValueError('unexpected source normal fallback')
  struct.pack_into('<I',h,end,1);struct.pack_into('<I',h,end+12,len(pixels));struct.pack_into('<I',h,end+20,d.m_TextureFormat);h[end+30]=int(d.m_IgnoreMipmapLimit);struct.pack_into('<I',h,end+76,d.m_LightmapFormat);struct.pack_into('<I',h,head-4,len(pixels))
  if bytes(h)!=raw[:head]:raise ValueError('header differs '+name)
  chunks=[];off=0
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);height=max(1,d.m_Height>>level);n=((w+3)//4)*((height+3)//4)*16
   im=C.parse_image_data(imgs[0][off:off+n],w,height,12,s.object_reader.version,s.object_reader.platform).convert('RGBA');x=im.getchannel('A');y=im.getchannel('G');z=Image.frombytes('L',(w,height),bytes(lut[(i<<8)|j] for i,j in zip(x.tobytes(),y.tobytes())));normal=Image.merge('RGBA',(x,y,z,Image.new('L',(w,height),255)));enc,fmt=C.image_to_texture2d(normal,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(fmt)!=d.m_TextureFormat:raise ValueError('normal format differs')
   chunks.append(enc);off+=n
  if off!=len(imgs[0]) or b''.join(chunks)!=pixels:raise ValueError('normal conversion differs '+name)
  work[obj.byte_start:obj.byte_start+len(raw)]=raw
  rows.append((obj.byte_start,head,len(pixels),d.m_Width,d.m_Height,d.m_MipCount,12,d.m_TextureFormat,d.m_LightmapFormat,int(d.m_IgnoreMipmapLimit),head,0,refs[0][0],refs[1][0],1,refs[0][2],1,refs[1][2],len(imgs[0]),sha(heads[0]),sha(imgs[0]),sha(raw)))
 rows.sort();out=bytearray((b'OBS1P001' if water else b'OBS1N001')+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:out.extend(struct.pack('>IIIIIIIIIIIIQQIQIQI',*r[:19])+b''.join(r[19:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'normal textures',len(rows))
if __name__=='__main__':main()
