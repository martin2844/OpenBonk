#!/usr/bin/env python3
"""Map 72 second-map texture objects to Steam headers and texture streams."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'shared2-mesh-android-plan/partial-v7.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip')
PINS=(('e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'),('cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84'))
MANIFEST=B/'local-research-manifests/shared2-matched72-template/textures-manifest.json';OUTPUT=B/'shared2-texture-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='58e904b3b8a3d6f1c80d374d588708184b1b3fb75029d496fbec1edafec650c4':raise ValueError('base differs')
 with TARGET.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest()!='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691':raise ValueError('target OBB differs')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read('assets/bin/Data/sharedassets2.assets.split'+str(i)) for i in range(22))
 objs={o.path_id:o for o in UnityPy.load(target).objects};baseObjs={o.path_id:o for o in UnityPy.load(base).objects};sources=[];files=[]
 for zpath,pins in zip(ZIPS,PINS):
  with zipfile.ZipFile(zpath) as z:data=[z.read('Megabonk_Data/'+n) for n in ('sharedassets2.assets','sharedassets2.assets.resS')]
  if tuple(sha(x).hex() for x in data)!=pins:raise ValueError('Steam source differs')
  files.append(data);sources.append({o.path_id:o for o in UnityPy.load(data[0]).objects})
 records=json.loads(MANIFEST.read_text())['textures']
 if len(records)!=72 or len({r['target_id'] for r in records})!=72:raise ValueError('invalid texture inventory')
 work=bytearray(base);rows=[]
 for rec in records:
  dst=objs[rec['target_id']];blank=baseObjs[rec['target_id']];d=dst.read();raw=dst.get_raw_data();pixels=bytes(d.image_data);header=raw.find(pixels)
  if dst.type.name!='Texture2D' or dst.byte_start!=blank.byte_start or any(blank.get_raw_data()) or not pixels or raw.count(pixels)!=1 or raw[header+len(pixels):]!=bytes(16):raise ValueError('invalid target texture layout')
  refs=[];images=[];headers=[]
  for data,index in zip(files,sources):
   obj=index[rec['source_id']];s=obj.read();stream=s.m_StreamData;image=data[1][stream.offset:stream.offset+stream.size];objRaw=obj.get_raw_data();hdr=objRaw[:header]
   if s.m_Name!=d.m_Name or s.m_TextureFormat!=rec['source_format'] or not stream or stream.path!='sharedassets2.assets.resS' or sha(image).hex()!=rec['source_sha256'] or len(hdr)!=header:raise ValueError('invalid Steam texture')
   if (s.m_Width,s.m_Height,s.m_MipCount)!=(d.m_Width,d.m_Height,d.m_MipCount) or data[0][obj.byte_start:obj.byte_start+header]!=hdr:raise ValueError('texture metadata differs')
   refs.append((obj.byte_start,stream.offset));images.append(image);headers.append(hdr)
  if images[0]!=images[1] or headers[0]!=headers[1]:raise ValueError('platform texture differs')
  hdr=bytearray(headers[0]);nameEnd=4+((len(d.m_Name.encode('utf8'))+3)&~3)
  if nameEnd+80>header or d.m_LightmapFormat not in (0,6):raise ValueError('unexpected texture metadata')
  struct.pack_into('<I',hdr,nameEnd+12,len(pixels));struct.pack_into('<I',hdr,nameEnd+20,d.m_TextureFormat);struct.pack_into('<I',hdr,nameEnd+76,d.m_LightmapFormat);struct.pack_into('<I',hdr,header-4,len(pixels))
  if bytes(hdr)!=raw[:header]:raise ValueError('derived texture header differs')
  off=0;chunks=[]
  for lv in range(d.m_MipCount):
   w=max(1,d.m_Width>>lv);h=max(1,d.m_Height>>lv);fmt=rec['source_format'];n=w*h*(3 if fmt==3 else 4) if fmt in (3,4) else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
   im=C.parse_image_data(images[0][off:off+n],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');out,actual=C.image_to_texture2d(im,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(actual)!=d.m_TextureFormat:raise ValueError('converted format differs')
   chunks.append(out);off+=n
  if off!=len(images[0]) or b''.join(chunks)!=pixels:raise ValueError('converted pixels differ')
  start=dst.byte_start;work[start:start+len(raw)]=raw
  rows.append((start,header,len(pixels),d.m_Width,d.m_Height,d.m_MipCount,rec['source_format'],d.m_TextureFormat,d.m_LightmapFormat,refs[0][0],refs[1][0],refs[0][1],refs[1][1],len(images[0]),sha(headers[0]),sha(images[0]),sha(raw)))
 rows.sort(key=lambda r:r[0]);plan=bytearray(b'OBST2001'+struct.pack('>I',len(rows)))
 for pins in PINS:
  for h in pins:plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>I',len(base)));plan.extend(sha(base));plan.extend(sha(work));plan.extend(sha(target))
 for row in rows:plan.extend(struct.pack('>IIIIIIIIIQQQQI',*row[:14]));plan.extend(b''.join(row[14:]))
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('72 textures; differing target bytes',sum(a!=b for a,b in zip(work,target)),'intermediate sha',sha(work).hex(),'plan sha',sha(plan).hex())
if __name__=='__main__':main()
