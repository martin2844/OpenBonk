#!/usr/bin/env python3
"""Derive exact material values and verified audio references for shared1."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android';BASE=B/'shared1-layout-android-plan/result.assets';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';ZIPS=[B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip'];OUT=B/'shared1-material-audio-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='98793ea6706fda2e576257fe677daf5bbdcfd12488a31b61c2e4c47b0bcb9a98':raise ValueError('invalid base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets1.assets.split{i}') for i in range(117))
 a={o.path_id:o for o in UnityPy.load(base).objects};b={o.path_id:o for o in UnityPy.load(target).objects};data=[];sources=[]
 for p in ZIPS:
  with zipfile.ZipFile(p) as z:data.append(z.read('Megabonk_Data/sharedassets1.assets'))
  sources.append({o.path_id:o for o in UnityPy.load(data[-1]).objects})
 records=json.loads((B/'localization-shared-scrubbed-base/material-manifest.json').read_text())['objects']
 if len(records)!=397:raise ValueError('material inventory differs')
 result=bytearray(base);ranges=[]
 for r in records:
  dst=b[r['target_id']];raw=dst.get_raw_data();n=r['length'];pos=dst.byte_start+len(raw)-n;value=raw[-n:];refs=[]
  if dst.type.name!='Material' or sha(value).hex()!=r['value_sha256'] or any(result[pos:pos+n]):raise ValueError('material target differs')
  for f,index in zip(data,sources):
   src=index[r['steam_id']];sr=src.get_raw_data();off=src.byte_start+len(sr)-n
   if src.type.name!='Material' or sr[-n:]!=value or f[off:off+n]!=value:raise ValueError('material source differs')
   refs.append(off)
  result[pos:pos+n]=value;ranges.append((pos,n,*refs,sha(value)))
 audioPlan=(B/'shared-audio-android-plan/plan.bin').read_bytes()
 if sha(audioPlan).hex()!='57a8feb4f98d9eb6692c208dd32d9f92556d52b41cb682936b03324945bd9883':raise ValueError('audio plan differs')
 audioRanges=set();p=8+4+128+8+64
 for i in range(118):
  targetOffset,linux,windows,size=struct.unpack_from('>QQQI',audioPlan,p);p+=28+32;audioRanges.add((targetOffset,size))
 audios=[]
 sourceAudio=[]
 for index in sources:
  table={}
  for obj in index.values():
   if obj.type.name=='AudioClip':
    name=obj.read().m_Name
    if name in table:raise ValueError('duplicate Steam audio name')
    table[name]=obj
  sourceAudio.append(table)
 for dst in sorted((o for o in b.values() if o.type.name=='AudioClip'),key=lambda o:o.path_id):
  clip=dst.read();new=clip.m_Resource;raw=dst.get_raw_data();where=dst.byte_start;refs=[];hashes=[]
  if new.m_Source!='sharedassets1.resource' or (new.m_Offset,new.m_Size) not in audioRanges or any(result[where:where+len(raw)]):raise ValueError('audio reference differs')
  for table in sourceAudio:
   src=table[clip.m_Name];v=src.get_raw_data();rebuilt=bytearray(v);nameEnd=4+((len(clip.m_Name.encode())+3)&~3)
   if len(v)!=len(raw) or rebuilt[nameEnd+30]!=1:raise ValueError('audio source header differs')
   rebuilt[nameEnd+30]=0;struct.pack_into('<QQ',rebuilt,len(v)-20,new.m_Offset,new.m_Size)
   if rebuilt!=raw:raise ValueError('audio metadata differs')
   refs.append(src.byte_start);hashes.append(sha(v))
  result[where:where+len(raw)]=raw;audios.append((where,len(raw),refs[0],refs[1],new.m_Offset,new.m_Size,*hashes,sha(raw)))
 if len(audios)!=118:raise ValueError('audio count differs')
 ranges.sort();out=bytearray(b'OBS1M001'+struct.pack('>III',len(base),len(ranges),len(audios))+sha(base)+sha(result))
 for f in data:out.extend(struct.pack('>Q',len(f))+sha(f))
 for row in ranges:out.extend(struct.pack('>IIQQ',*row[:4])+row[4])
 for row in audios:out.extend(struct.pack('>IIQQQQ',*row[:6])+b''.join(row[6:]))
 OUT.parent.mkdir(parents=True,exist_ok=True)
 with OUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'intermediate',sha(result).hex(),'materials',len(ranges),'audio',len(audios))
if __name__=='__main__':main()
