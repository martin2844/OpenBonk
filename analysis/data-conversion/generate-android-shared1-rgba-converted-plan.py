#!/usr/bin/env python3
"""Map 110 alpha/BC3 textures for RGBA conversion on Android."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-rgba-exact-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');MANIFEST=B/'local-research-manifests/rgba-template/manifest.json';OUT=B/'shared1-rgba-converted-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='41bbce3f1884566a5ff844be5a0f448fa7032364c6e2dd3cd3286ee6ec201202':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets1.assets','sharedassets1.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(files[-1][0]).objects})
 recs=json.loads(MANIFEST.read_text())['textures'];work=bytearray(base);rows=[]
 for rec in recs:
  obj=objects[rec['target_id']];d=obj.read();raw=obj.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels);sourceFmt=sources[0][rec['source_id']].read().m_TextureFormat
  if sourceFmt==4:continue
  if sourceFmt not in (1,12) or d.m_Name!=rec['name'] or d.m_TextureFormat!=4 or d.m_MipCount!=1 or head<80 or raw.count(pixels)!=1 or raw[head+len(pixels):]!=bytes(16) or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target differs')
  refs=[];heads=[];imgs=[]
  for dat,index in zip(files,sources):
   src=index[rec['source_id']];s=src.read();sr=src.get_raw_data();stream=s.m_StreamData;header=sr[:head]
   if stream.path=='sharedassets1.assets.resS':fid=1;off=stream.offset;img=dat[1][off:off+stream.size]
   elif not stream.path:
    img=bytes(s.image_data)
    if not img or sr.count(img)!=1:raise ValueError('ambiguous inline image')
    fid=0;off=src.byte_start+sr.index(img)
   else:raise ValueError('unknown image stream')
   if s.m_Name!=d.m_Name or (s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat)!=(d.m_Width,d.m_Height,1,sourceFmt):raise ValueError('Steam texture metadata differs')
   refs.append((src.byte_start,fid,off));heads.append(header);imgs.append(img)
  if heads[0]!=heads[1] or imgs[0]!=imgs[1]:raise ValueError('platform source differs')
  image=C.parse_image_data(imgs[0],d.m_Width,d.m_Height,sourceFmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');encoded,fmt=C.image_to_texture2d(image,4,d.object_reader.platform,d.m_PlatformBlob)
  if int(fmt)!=4 or encoded!=pixels:raise ValueError('RGBA conversion differs')
  h=bytearray(heads[0]);end=4+((len(d.m_Name.encode())+3)&~3);struct.pack_into('<I',h,end+12,len(pixels));struct.pack_into('<I',h,end+20,4);h[end+30]=int(d.m_IgnoreMipmapLimit);struct.pack_into('<I',h,end+76,d.m_LightmapFormat);struct.pack_into('<I',h,head-4,len(pixels))
  if bytes(h)!=raw[:head]:raise ValueError('derived header differs: '+d.m_Name)
  work[obj.byte_start:obj.byte_start+len(raw)]=raw
  rows.append((obj.byte_start,head,len(pixels),d.m_Width,d.m_Height,sourceFmt,d.m_LightmapFormat,int(d.m_IgnoreMipmapLimit),refs[0][0],refs[1][0],refs[0][1],refs[0][2],refs[1][1],refs[1][2],len(imgs[0]),sha(heads[0]),sha(imgs[0]),sha(raw)))
 if len(rows)!=110:raise ValueError('converted count differs')
 rows.sort();out=bytearray(b'OBS1V001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:out.extend(struct.pack('>IIIIIIIIQQIQIQI',*r[:15])+b''.join(r[15:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'converted',len(rows))
if __name__=='__main__':main()
