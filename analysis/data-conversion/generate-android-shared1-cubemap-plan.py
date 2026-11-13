#!/usr/bin/env python3
"""Map seven cubemaps to Steam data for a private Android conversion."""
import hashlib,struct,subprocess,tempfile,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-animation-terrain-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');DECODER=ROOT/'build/tooling/unpack-unity-crunch-android-plan';OUT=B/'shared1-cubemap-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='e0b079e987a1928b2207ae9fcb4e4bae29220928cd42c90188a2d2f63db70646':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets1.assets','sharedassets1.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(files[-1][0]).objects})
 work=bytearray(base);rows=[]
 with tempfile.TemporaryDirectory(prefix='openbonk-cubemap-plan-') as tmp:
  srcPath=Path(tmp)/'input.crn';outPath=Path(tmp)/'output.dxt5'
  for tid,sid,name,fmt in ((1902,2284,'Desert1',29),(1903,2285,'Desert2',29),(1904,2286,'Forest3',29),(1905,2287,'Forest2',29),(1906,2289,'Desert3',29),(1907,2290,'_Graveyard',12),(1908,2288,'Forest1',29)):
   obj=objects[tid];d=obj.read();raw=obj.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels);tail=raw[head+len(pixels):]
   expectedTail=bytearray(92);struct.pack_into('<I',expectedTail,16,6)
   if d.m_Name!=name or (d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_TextureFormat)!=(512,512,1,6,45) or len(pixels)!=786432 or head<80 or raw.count(pixels)!=1 or tail!=expectedTail or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target cubemap differs')
   refs=[];headers=[];images=[]
   for dat,index in zip(files,sources):
    src=index[sid];s=src.read();sr=src.get_raw_data();stream=s.m_StreamData;img=dat[1][stream.offset:stream.offset+stream.size]
    if s.m_Name!=name or (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount,s.m_TextureFormat)!=(512,512,1,6,fmt) or stream.path!='sharedassets1.assets.resS':raise ValueError('Steam cubemap differs')
    refs.append((src.byte_start,stream.offset));headers.append(sr[:head]);images.append(img)
   if headers[0]!=headers[1] or images[0]!=images[1]:raise ValueError('platform cubemap differs')
   hdr=bytearray(headers[0]);end=4+((len(name.encode())+3)&~3)
   if struct.unpack_from('<I',hdr,end)[0]!=0:raise ValueError('unexpected cubemap source flag')
   struct.pack_into('<I',hdr,end,1);struct.pack_into('<I',hdr,end+12,131072);struct.pack_into('<I',hdr,end+20,45);hdr[end+30]=int(d.m_IgnoreMipmapLimit);struct.pack_into('<I',hdr,end+76,d.m_LightmapFormat);struct.pack_into('<I',hdr,head-4,len(pixels))
   if bytes(hdr)!=raw[:head]:raise ValueError('cubemap header differs')
   if fmt==29:
    srcPath.write_bytes(images[0]);outPath.unlink(missing_ok=True);subprocess.run([str(DECODER),str(srcPath),str(outPath)],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL);decoded=outPath.read_bytes()
   else:decoded=images[0]
   if len(decoded)!=1572864:raise ValueError('decoded cubemap size differs')
   faces=[]
   for i in range(6):
    image=C.parse_image_data(decoded[i*262144:(i+1)*262144],512,512,12,s.object_reader.version,s.object_reader.platform).convert('RGBA');encoded,actual=C.image_to_texture2d(image,45,d.object_reader.platform,d.m_PlatformBlob)
    if int(actual)!=45:raise ValueError('cubemap format differs')
    faces.append(encoded)
   if b''.join(faces)!=pixels:raise ValueError('cubemap pixels differ')
   work[obj.byte_start:obj.byte_start+len(raw)]=raw;rows.append((obj.byte_start,head,fmt,d.m_LightmapFormat,int(d.m_IgnoreMipmapLimit),refs[0][0],refs[1][0],refs[0][1],refs[1][1],len(images[0]),sha(headers[0]),sha(images[0]),sha(decoded),sha(raw)))
 rows.sort();out=bytearray(b'OBS1CUB1'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:out.extend(struct.pack('>IIIIIQQQQI',*r[:10])+b''.join(r[10:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'cubemaps',len(rows))
if __name__=='__main__':main()
