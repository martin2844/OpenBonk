#!/usr/bin/env python3
"""Map exact shared1 texture pixels and derived Unity headers to Steam."""
import hashlib,json,struct,sys,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-mesh-stream-plan/linux.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');MANIFEST=B/'local-research-manifests/direct-texture-template/manifest.json';OUT=B/'shared1-exact-textures-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 rgba=len(sys.argv)==2 and sys.argv[1]=='rgba'
 if len(sys.argv)>2 or len(sys.argv)==2 and not rgba:raise ValueError('usage: generator [rgba]')
 global BASE,MANIFEST,OUT
 if rgba:BASE=B/'shared1-exact-textures-plan/linux.assets';MANIFEST=B/'local-research-manifests/rgba-template/manifest.json';OUT=B/'shared1-rgba-exact-plan/plan.bin'
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!=('cde5b19279b3c5ddf3c72b973e96a93e99cb9ec4490b58178a6f47c1ea61564c' if rgba else 'e609d9c11f63ce8b42a582780dd23848bae7d06efc5936ac5d3db4d81a9b106f'):raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 objects={o.path_id:o for o in UnityPy.load(target).objects};files=[];sources=[]
 for path in ZIPS:
  with zipfile.ZipFile(path) as z:files.append([z.read('Megabonk_Data/'+n) for n in ('sharedassets1.assets','sharedassets1.assets.resS')])
  sources.append({o.path_id:o for o in UnityPy.load(files[-1][0]).objects})
 recs=json.loads(MANIFEST.read_text())['textures'];work=bytearray(base);rows=[]
 if len(recs)!=(289 if rgba else 168):raise ValueError('unexpected texture inventory')
 for rec in recs:
  obj=objects[rec['target_id']];d=obj.read();raw=obj.get_raw_data();pixels=bytes(d.image_data);head=raw.find(pixels)
  if rgba and sources[0][rec['source_id']].read().m_TextureFormat!=4:continue
  if d.m_Name!=rec['name'] or (not rgba and (len(pixels)!=rec['size'] or sha(pixels).hex()!=rec['sha256'])) or head<80 or raw.count(pixels)!=1 or raw[head+len(pixels):]!=bytes(16) or any(work[obj.byte_start:obj.byte_start+len(raw)]):raise ValueError('target texture differs: '+str((rec['target_id'],d.m_Name)))
  refs=[];heads=[];imgs=[]
  for dat,index in zip(files,sources):
   src=index[rec['source_id']];s=src.read();sr=src.get_raw_data();stream=s.m_StreamData;header=sr[:head]
   if stream.path=='sharedassets1.assets.resS':fid=1;off=stream.offset;img=dat[1][off:off+stream.size]
   elif not stream.path and sr.count(pixels)==1:fid=0;off=src.byte_start+sr.index(pixels);img=dat[0][off:off+len(pixels)]
   else:raise ValueError('unsupported Steam texture source')
   if s.m_Name!=d.m_Name or (s.m_Width,s.m_Height,s.m_MipCount,s.m_TextureFormat)!=(d.m_Width,d.m_Height,d.m_MipCount,d.m_TextureFormat) or img!=pixels:raise ValueError('Steam texture differs: '+repr((rec['target_id'],d.m_Name,s.m_Name)))
   refs.append((src.byte_start,fid,off));heads.append(header);imgs.append(img)
  if heads[0]!=heads[1] or imgs[0]!=imgs[1]:raise ValueError('platform source differs')
  h=bytearray(heads[0]);end=4+((len(d.m_Name.encode())+3)&~3);struct.pack_into('<I',h,end+76,d.m_LightmapFormat);struct.pack_into('<I',h,head-4,len(pixels))
  if bytes(h)!=raw[:head]:raise ValueError('derived header differs')
  work[obj.byte_start:obj.byte_start+len(raw)]=raw
  rows.append((obj.byte_start,head,len(pixels),d.m_LightmapFormat,refs[0][0],refs[1][0],refs[0][1],refs[0][2],refs[1][1],refs[1][2],sha(heads[0]),sha(pixels),sha(raw)))
 if len(rows)!=(179 if rgba else 168):raise ValueError('selected texture count differs')
 rows.sort();out=bytearray((b'OBS1U001' if rgba else b'OBS1T001')+struct.pack('>II',len(base),len(rows))+sha(base)+sha(work))
 for platform in files:
  for f in platform:out.extend(struct.pack('>Q',len(f))+sha(f))
 for r in rows:out.extend(struct.pack('>IIIIQQIQIQ',*r[:10])+b''.join(r[10:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'partial',sha(work).hex(),'textures',len(rows),'bytes',sum(r[2] for r in rows))
if __name__=='__main__':main()
