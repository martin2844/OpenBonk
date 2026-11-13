#!/usr/bin/env python3
"""Locally reconstruct seven serialized font metadata objects from a pinned PC build.

The byte patches adapt platform references observed in this build. The template
still contains game data and neither it nor its outputs are for distribution.
"""
import argparse,hashlib,json,shutil
from pathlib import Path
import UnityPy
PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
BASE='5726ab27dfa1bad53a6e77f856d0379294b79640e5ec9b1848875f8b48179cc1'
PAIRS=list(zip(range(131,138),range(133,140)))
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'missing or incompatible input: {p}')
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('action',choices=['template','restore']);p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--manifest',type=Path);p.add_argument('--converted-textures',action='store_true')
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets0.assets',PC)
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.manifest:p.error('restore requires --manifest')
 m=json.loads(a.manifest.read_text());records=m['objects']
 if m['version']!=1 or m['base_sha256']!=BASE or [(x['target_id'],x['source_id']) for x in records]!=PAIRS:raise ValueError('invalid manifest')
 require(a.input_asset,'7158ed15e1e8d134e836ff0e6c60cdf5309a4a6cb7a14d6e2e7777fceb4c62b7' if a.converted_textures else m['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets0.assets'));env=UnityPy.load(str(a.input_asset))
sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects};out_records=[]
for idx,(tid,sid) in enumerate(PAIRS):
 t=targets[tid];s=sources[sid]
 if t.type.name!='MonoBehaviour' or s.type.name!='MonoBehaviour':raise ValueError('wrong object type')
 source=s.get_raw_data();old=t.get_raw_data()
 if len(source)!=len(old):raise ValueError('object length mismatch')
 if a.action=='template':
  edits=[[i,b] for i,(b,c) in enumerate(zip(old,source)) if b!=c]
  if not 1<=len(edits)<=32:raise ValueError('unexpected patch size')
  out_records.append({'target_id':tid,'source_id':sid,'size':len(old),'source_sha256':digest(source),'target_sha256':digest(old),'edits':edits})
  t.set_raw_data(bytes(len(old)))
 else:
  rec=records[idx]
  if rec['size']!=len(source) or rec['source_sha256']!=digest(source) or any(old):raise ValueError('metadata source/template mismatch')
  result=bytearray(source);seen=set()
  for offset,value in rec['edits']:
   if not isinstance(offset,int) or not 0<=offset<len(result) or offset in seen or not isinstance(value,int) or not 0<=value<=255:raise ValueError('invalid patch')
   seen.add(offset);result[offset]=value
  if not 1<=len(seen)<=32 or digest(result)!=rec['target_sha256']:raise ValueError('metadata patch mismatch')
  t.set_raw_data(bytes(result))
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets0.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.action=='template':(a.output_dir/'metadata-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'objects':out_records},indent=2)+'\n')
 elif h!=('8c875f7f1b90cc9509eb8dd2790caa2b5de33b5cc5c11d5135ae687941bae418' if a.converted_textures else BASE):raise ValueError('restored asset differs from expected output')
 print(a.action,len(PAIRS),'objects; asset SHA256',h,'; template contains other game data, not distributable')
except Exception:shutil.rmtree(a.output_dir);raise
