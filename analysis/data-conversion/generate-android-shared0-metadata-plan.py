#!/usr/bin/env python3
"""Derive exact material/font ranges and Unity reference edits for a local study."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy

ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'shared0-atlas-android-plan/linux.assets'
TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=[B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip']
OUTPUT=B/'shared0-metadata-android-plan/plan.bin'
def sha(b):return hashlib.sha256(b).digest()

def main():
 if OUTPUT.exists():raise ValueError('output exists')
 base=BASE.read_bytes()
 if sha(base).hex()!='aa89dcccddb5c0f60d80b0e21c1aa67c6219f1c77b6bd4784ef1dc5c049bea9e':raise ValueError('incompatible base')
 with zipfile.ZipFile(TARGET) as z:target=b''.join(z.read(f'assets/bin/Data/sharedassets0.assets.split{i}') for i in range(126))
 objs={o.path_id:o for o in UnityPy.load(target).objects};data=[];sources=[]
 for p in ZIPS:
  with zipfile.ZipFile(p) as z:data.append(z.read('Megabonk_Data/sharedassets0.assets'))
  sources.append({o.path_id:o for o in UnityPy.load(data[-1]).objects})
 specs=[]
 for r in json.loads((B/'localization-shared-scrubbed-base/material-extra-0/manifest.json').read_text())['objects']:
  specs.append(('material',r['target_id'],r['steam_id'],r['length'],None))
 for r in json.loads((B/'local-research-manifests/shared0-font-template/font-manifest.json').read_text())['fonts']:
  specs.append(('font',r['target_id'],r['source_id'],None,None))
 for r in json.loads((ROOT/'analysis/data-conversion/no-rewired-font-reference-offsets.json').read_text())['objects']:
  specs.append(('metadata',r['target_id'],r['source_id'],None,r['reference_offsets']))
 if len(specs)!=29:raise ValueError('inventory differs')
 rows=[];result=bytearray(base)
 for kind,tid,sid,size,refs in specs:
  dst=objs[tid];raw=dst.get_raw_data();size=len(raw) if size is None else size;target_data=raw[-size:];dst_pos=dst.byte_start+len(raw)-size
  if any(x not in (0,y) for x,y in zip(base[dst_pos:dst_pos+size],target_data)):raise ValueError('unexpected retained bytes')
  source_offsets=[];source_hashes=[];patches=[]
  for f,idx in zip(data,sources):
   src=idx[sid];sr=src.get_raw_data();v=sr[-size:]
   if len(v)!=size:raise ValueError('source size differs')
   off=src.byte_start+len(sr)-size
   if f[off:off+size]!=v:raise ValueError('source range differs')
   edits=[(i,y) for i,(x,y) in enumerate(zip(v,target_data)) if x!=y]
   if kind=='material' and edits:raise ValueError('material values differ')
   if kind=='font' and (len(edits)!=2 or any(i>=128 for i,_ in edits)):raise ValueError('font references differ')
   if kind=='metadata' and any(not any(ref<=i<ref+12 for ref in refs) for i,_ in edits):raise ValueError('non-reference metadata difference')
   source_offsets.append(off);source_hashes.append(sha(v));patches.append(edits)
  result[dst_pos:dst_pos+size]=target_data
  rows.append((dst_pos,size,source_offsets,source_hashes,sha(target_data),patches))
 rows.sort(key=lambda r:r[0]);out=bytearray(b'OBS0M001'+struct.pack('>II',len(base),len(rows))+sha(base)+sha(result))
 for f in data:out.extend(struct.pack('>Q',len(f))+sha(f))
 for dst,size,offsets,hashes,target_hash,patches in rows:
  out.extend(struct.pack('>IIQQ',dst,size,*offsets)+b''.join(hashes)+target_hash)
  for edits in patches:
   out.extend(struct.pack('>I',len(edits)))
   for pos,value in edits:out.extend(struct.pack('>IB',pos,value))
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(out)
 print('plan',sha(out).hex(),'result',sha(result).hex(),'objects',len(rows),'edits',sum(len(e) for r in rows for e in r[-1]))
 print('remaining different asset splits',sum(result[i*1048576:(i+1)*1048576]!=target[i*1048576:(i+1)*1048576] for i in range(126)))
if __name__=='__main__':main()
