#!/usr/bin/env python3
"""Generate private offset-only plans for Steam-derived preload component bodies."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
B=Path(__file__).resolve().parents[2]/'build/android';BASE=B/'builtin-shader-diagnostic-obb/result.obb';REF=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';MANIFEST=B/'localization-shared-scrubbed-base/preload-particle-manifest.json';ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip');NAMES=('sharedassets1.assets','sharedassets2.assets');OUT=B/'preload-body-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()
def p(*n):return struct.pack('>'+'I'*len(n),*n)
def main():
 doc=json.loads(MANIFEST.read_bytes());records=doc['files'];sources=[];data=[]
 if len(records)!=68 or sum(len(r['objects']) for r in records.values())!=635:raise ValueError('unexpected preload inventory')
 for zippath in ZIPS:
  with zipfile.ZipFile(zippath) as z: files=[z.read('Megabonk_Data/'+n) for n in NAMES]
  data.append(files);sources.append([{o.path_id:o for o in UnityPy.load(value).objects} for value in files])
 out=bytearray(b'OBPRE001'+p(len(records)))
 for platform in data:
  for value in platform:out.extend(struct.pack('>Q',len(value))+sha(value))
 count=0
 with zipfile.ZipFile(BASE) as base,zipfile.ZipFile(REF) as ref:
  for name,r in sorted(records.items()):
   old=base.read('assets/bin/Data/'+name);want=ref.read('assets/bin/Data/'+name)
   if sha(old).hex()!=r['template_sha256'] or sha(want).hex()!=r['original_sha256']:raise ValueError('preload template differs '+name)
   target={o.path_id:o for o in UnityPy.load(old).objects};rows=[];work=bytearray(old)
   for item in r['objects']:
    dst=target[item['target_id']];f=NAMES.index(item['steam_file']);values=[];offsets=[]
    if dst.type.name!=item['type'] or dst.byte_size!=item['body_size']+12:raise ValueError('target object differs')
    for source in sources:
     obj=source[f][item['steam_id']];body=obj.get_raw_data()[12:]
     if obj.type.name!=item['type'] or len(body)!=item['body_size'] or sha(body).hex()!=item['body_sha256']:raise ValueError('source body differs')
     offsets.append(obj.byte_start+12);values.append(body)
    if values[0]!=values[1]:raise ValueError('source platform differs')
    off=dst.byte_start+12;n=len(values[0]);
    if any(work[off:off+n]) or want[off:off+n]!=values[0]:raise ValueError('invalid target range')
    work[off:off+n]=values[0];rows.append((off,n,f,*offsets,sha(values[0])))
   rows.sort()
   if work!=want:raise ValueError('incomplete file '+name)
   out.extend(name.encode('ascii')+p(len(old),len(rows))+sha(old)+sha(want))
   for off,n,f,linux,windows,h in rows:out.extend(p(off,n,f)+struct.pack('>QQ',linux,windows)+h)
   count+=len(rows)
 OUT.parent.mkdir(parents=True,exist_ok=True)
 if OUT.exists():raise ValueError('output exists')
 OUT.write_bytes(out);print('PASS:',len(records),'files',count,'objects; plan bytes',len(out),'sha256',sha(out).hex())
if __name__=='__main__':main()
