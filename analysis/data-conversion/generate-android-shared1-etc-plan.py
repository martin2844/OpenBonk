#!/usr/bin/env python3
"""Map Steam mip textures to the experimental ETC reference assets."""
import hashlib,json,struct,sys,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-rgba-converted-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');MANIFESTS=(B/'local-research-manifests/etc-mip-template/manifest.json',B/'local-research-manifests/bc-etc-template/manifest.json');OUT=B/'shared1-etc-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 rgba=len(sys.argv)==2 and sys.argv[1]=='rgba';matched=len(sys.argv)==2 and sys.argv[1]=='matched';alpha=len(sys.argv)==2 and sys.argv[1]=='alpha';large=len(sys.argv)==2 and sys.argv[1]=='large';renamed=len(sys.argv)==2 and sys.argv[1]=='renamed';last=len(sys.argv)==2 and sys.argv[1]=='last';alias=last or renamed or large or alpha or matched or len(sys.argv)==2 and sys.argv[1]=='alias'
 if len(sys.argv)>2 or len(sys.argv)==2 and not (rgba or alias):raise ValueError('usage: generator [rgba|alias|matched|alpha]')
 global BASE,MANIFESTS,OUT
 if rgba:BASE=B/'shared1-etc-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/bc-rgba-template/manifest.json',);OUT=B/'shared1-bc-rgba-plan/plan.bin'
 if alias:BASE=B/'shared1-bc-rgba-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/shared1-last-textures-template/textures-manifest.json',);OUT=B/'shared1-alias-textures-plan/plan.bin'
 if matched:BASE=B/'shared1-alias-textures-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/matched-template/matched.json',);OUT=B/'shared1-matched-textures-plan/plan.bin'
 if alpha:BASE=B/'shared1-matched-textures-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/alpha8-template/manifest.json',);OUT=B/'shared1-alpha-textures-plan/plan.bin'
 if large:BASE=B/'shared1-alpha-textures-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/large-template/manifest.json',);OUT=B/'shared1-large-textures-plan/plan.bin'
 if renamed:BASE=B/'shared1-large-textures-plan/linux.assets';MANIFESTS=(B/'local-research-manifests/full-exact-stripped/exact-renamed.json',);OUT=B/'shared1-renamed-textures-plan/plan.bin'
 if last:BASE=B/'shared1-renamed-textures-plan/linux.assets';MANIFESTS=(B/'share-source-experiment/shared1-four-textures-template/textures-manifest.json',);OUT=B/'shared1-three-textures-plan/plan.bin'
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!=('571f134d24d874a6537b2648580ee1e6eb77bed5f2dfac43f09584a4caf97117' if last else 'c7e867a69354c18c908bf7d0f4ab26a2740786cd3eabf2c9055a8be714b38b57' if renamed else 'b68de113a869c7c0ce009fcde2702d6a9fb4c5ce68007461bca6898f630fb091' if large else '28370f83ecd06bdab5ce2209883e1e34f61c0a9c1f980ae14a57d5373926cb0b' if alpha else 'eeec5fd78e2f86a2f61062fc8aff2d689b98a54aad2f0048e80904e0a27f226e' if matched else 'cfb61e4dca3918522e44b62f03ff6924987e0a2a0eafb7fb7b1b262e28dcf2bd' if alias else '48eb60ddb23455cb0fb7cadaeab8135940c055ccdc18d9ac719e39bed53a4bcf' if rgba else '72792c774df00852df4d36bfdaba74c13597e35ea4b8814cc03ec421424408fd'):raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets1.assets','sharedassets1.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(files[-1][0]).objects})
 recs=[r for path in MANIFESTS for r in json.loads(path.read_text())['textures']];work=bytearray(base);rows=[]
 if last:recs=[r for r in recs if r['target_id']!=1072]
 expected=3 if last else 6 if renamed else 2 if large else 6 if alpha else 26 if matched else 59 if alias else 17 if rgba else 268
 if len(recs)!=expected or len({r['target_id'] for r in recs})!=expected:raise ValueError('invalid texture inventory')
 for rec in recs:
  obj=objects[rec['target_id']];d=obj.read();raw=obj.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels)
  targetName=rec['target_name'] if (matched or renamed) else rec['name']
  if d.m_Name!=targetName or d.m_TextureFormat not in ((4,45,47) if alias else (4,) if rgba else (45,47)) or head<80 or raw.count(pixels)!=1 or raw[head+len(pixels):]!=bytes(16) or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target differs '+targetName)
  srcName=rec['source_name'] if alias and not (alpha or large) else d.m_Name
  if d.m_Name==srcName:suffix=0
  elif d.m_Name in [srcName+'_'+str(i) for i in range(3)]:suffix=int(d.m_Name[-1])+1
  elif d.m_Name==srcName+'_asset':suffix=4
  else:raise ValueError('unsupported texture rename')
  dstPad=(len(d.m_Name.encode())+3)&~3;srcPad=(len(srcName.encode())+3)&~3;sourceHead=head+srcPad-dstPad
  refs=[];heads=[];imgs=[]
  for dat,index in zip(files,sources):
   src=index[rec['source_id']];s=src.read();sr=src.get_raw_data();stream=s.m_StreamData;header=sr[:sourceHead]
   if stream.path=='sharedassets1.assets.resS':fid=1;off=stream.offset;img=dat[1][off:off+stream.size]
   elif not stream.path:
    img=bytes(s.image_data)
    if not img or sr.count(img)!=1:raise ValueError('ambiguous inline texture')
    fid=0;off=src.byte_start+sr.index(img)
   else:raise ValueError('unknown image stream')
   if s.m_Name!=srcName or s.m_TextureFormat not in (1,3,4,10,12,25) or (s.m_Width,s.m_Height,s.m_MipCount)!=(d.m_Width,d.m_Height,d.m_MipCount) or sha(img).hex()!=rec['sha256' if renamed else 'source_sha256' if alias else 'pc_raw_sha256']:raise ValueError('Steam texture metadata differs '+d.m_Name)
   refs.append((src.byte_start,fid,off));heads.append(header);imgs.append(img)
  if heads[0]!=heads[1] or imgs[0]!=imgs[1]:raise ValueError('platform source differs')
  name=d.m_Name.encode();h=bytearray(struct.pack('<I',len(name))+name+bytes(dstPad-len(name))+heads[0][4+srcPad:]);end=4+dstPad;struct.pack_into('<I',h,end+12,len(pixels));struct.pack_into('<I',h,end+20,d.m_TextureFormat);h[end+30]=int(d.m_IgnoreMipmapLimit);struct.pack_into('<I',h,end+76,d.m_LightmapFormat);struct.pack_into('<I',h,head-4,len(pixels))
  if large and s.m_TextureFormat==25:
   if struct.unpack_from('<I',h,end)[0]!=1:raise ValueError('unexpected BC7 fallback')
   struct.pack_into('<I',h,end,0)
  if bytes(h)!=raw[:head]:raise ValueError('derived header differs: '+d.m_Name)
  chunks=[];off=0
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);height=max(1,d.m_Height>>level);n=w*height*s.m_TextureFormat if s.m_TextureFormat in (1,3,4) else ((w+3)//4)*((height+3)//4)*(8 if s.m_TextureFormat==10 else 16)
   image=C.parse_image_data(imgs[0][off:off+n],w,height,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA');encoded,fmt=C.image_to_texture2d(image,d.m_TextureFormat,d.object_reader.platform,d.m_PlatformBlob)
   if int(fmt)!=d.m_TextureFormat:raise ValueError('unexpected encoded format')
   chunks.append(encoded);off+=n
  if off!=len(imgs[0]) or b''.join(chunks)!=pixels:raise ValueError('texture conversion differs '+d.m_Name)
  work[obj.byte_start:obj.byte_start+len(raw)]=raw
  rows.append((obj.byte_start,head,len(pixels),d.m_Width,d.m_Height,d.m_MipCount,s.m_TextureFormat,d.m_TextureFormat,d.m_LightmapFormat,int(d.m_IgnoreMipmapLimit),sourceHead,suffix,refs[0][0],refs[1][0],refs[0][1],refs[0][2],refs[1][1],refs[1][2],len(imgs[0]),sha(heads[0]),sha(imgs[0]),sha(raw)))
 rows.sort();out=bytearray((b'OBS1D001' if last else b'OBS1C001' if renamed else b'OBS1B001' if large else b'OBS1A001' if alpha else b'OBS1Z001' if matched else b'OBS1Y001' if alias else b'OBS1X001' if rgba else b'OBS1W001')+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:
  if alias:out.extend(struct.pack('>IIIIIIIIIIIIQQIQIQI',*r[:19])+b''.join(r[19:]))
  else:out.extend(struct.pack('>IIIIIIIIIIQQIQIQI',*(r[:10]+r[12:19]))+b''.join(r[19:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'textures',len(rows),'source bytes',sum(r[18] for r in rows))
if __name__=='__main__':main()
