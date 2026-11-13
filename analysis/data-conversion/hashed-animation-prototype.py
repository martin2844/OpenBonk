#!/usr/bin/env python3
"""Prototype exact reconstruction of 108 hashed animation assets from Steam.

Difference patches contain bytes derived from the Android assets. The patches,
metadata and other game data remain unreviewed and are NOT redistributable.
"""
import argparse,base64,collections,hashlib,json,re,shutil,struct,zipfile
from pathlib import Path
from windows_sources import WINDOWS_PINS
import UnityPy
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';OBB='bad289bad4f106d314937a6a0a4e5790a0b0d608aece4f00416ac42168012e54'
HYBRID='716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
def digest(x):return hashlib.sha256(x).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path,help='base OBB for template; template directory for restore');p.add_argument('output_dir',type=Path);p.add_argument('--hybrid-manifest',type=Path,help='local experimental mixed animation manifest');p.add_argument('--windows-source',action='store_true',help='use pinned Windows inputs for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.hybrid_manifest and a.action!='restore':p.error('--hybrid-manifest requires restore')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.pc_data/'sharedassets1.assets',WINDOWS_PINS['sharedassets1.assets'] if a.windows_source else PC)
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));sources={o.path_id:o for o in pc.objects};index=collections.defaultdict(list)
if a.action=='template':
 require(a.input,OBB)
 for o in pc.objects:
  if o.type.name=='AnimationClip':
   d=o.read();raw=o.get_raw_data();index[(d.m_Name,len(raw))].append((o.path_id,raw))
else:
 if a.hybrid_manifest:require(a.hybrid_manifest,HYBRID)
 m=json.loads((a.hybrid_manifest if a.hybrid_manifest else a.input/'manifest.json').read_text());records=m['animations']
 if m['version']!=1 or m['base_obb_sha256']!=OBB or len(records)!=108 or len({r['file'] for r in records})!=108:raise ValueError('invalid manifest')
 if a.hybrid_manifest and (m.get('selected_count')!=25 or m.get('removed_patch_bytes')!=40928 or sum(r.get('mode')=='steam-stream' for r in records)!=25 or any(r.get('mode') not in ('steam-stream','legacy-exact') for r in records)):raise ValueError('invalid hybrid inventory')
a.output_dir.mkdir();out_records=[]
try:
 if a.action=='template':
  with zipfile.ZipFile(a.input) as z:
   for name in z.namelist():
    if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',name):continue
    raw=z.read(name);env=UnityPy.load(raw);objs=list(env.objects)
    if len(objs)!=1 or objs[0].type.name!='AnimationClip':continue
    obj=objs[0];d=obj.read();target=obj.get_raw_data();candidates=index[(d.m_Name,len(target))]
    if not candidates or not target or raw.count(target)!=1:raise ValueError(f'animation candidate missing or offset ambiguous: {name}')
    ranks=sorted((sum(x!=y for x,y in zip(target,src)),sid,src) for sid,src in candidates)
    ties=[r for r in ranks if r[0]==ranks[0][0]]
    if len({digest(r[2]) for r in ties})!=1:raise ValueError(f'ambiguous non-identical sources: {name}')
    _,sid,source=ranks[0];edits=[(i,b) for i,(b,x) in enumerate(zip(target,source)) if b!=x];patch=b''.join(struct.pack('<IB',i,b) for i,b in edits)
    offset=raw.find(target);blank=bytearray(raw);blank[offset:offset+len(target)]=bytes(len(target));blank=bytes(blank)
    filename=Path(name).name;rec={'file':filename,'name':d.m_Name,'source_id':sid,'offset':offset,'size':len(target),'source_sha256':digest(source),'object_sha256':digest(target),'original_sha256':digest(raw),'template_sha256':digest(blank),'patch_count':len(edits),'patch_base64':base64.b64encode(patch).decode('ascii')}
    out_records.append(rec);(a.output_dir/filename).write_bytes(blank)
  if len(out_records)!=108:raise ValueError(f'unexpected animation count {len(out_records)}')
  (a.output_dir/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'animations':out_records},separators=(',',':'))+'\n')
 else:
  for rec in records:
   filename=rec['file'];sid=rec['source_id']
   if not re.fullmatch(r'[0-9a-f]{32}',filename) or sid not in sources or sources[sid].type.name!='AnimationClip':raise ValueError('invalid source record')
   path=a.input/filename;require(path,rec['template_sha256']);blank=path.read_bytes();source=sources[sid].get_raw_data()
   if sources[sid].read().m_Name!=rec['name'] or len(source)!=rec['size'] or digest(source)!=rec['source_sha256']:raise ValueError('source mismatch')
   off=rec['offset'];size=rec['size']
   if not isinstance(off,int) or off<0 or off+size>len(blank) or any(blank[off:off+size]):raise ValueError('invalid template offset')
   value=bytearray(source)
   if a.hybrid_manifest and rec['mode']=='steam-stream':
    if 'patch_base64' in rec or rec['max_sampled_curve_difference']>m['threshold']:raise ValueError('invalid Steam stream entry')
   else:
    patch=base64.b64decode(rec['patch_base64'],validate=True)
    if len(patch)!=rec['patch_count']*5 or rec['patch_count']>len(source):raise ValueError('invalid patch length')
    last=-1
    for i in range(0,len(patch),5):
     pos,byte=struct.unpack_from('<IB',patch,i)
     if pos<=last or pos>=size or value[pos]==byte:raise ValueError('invalid patch entry')
     value[pos]=byte;last=pos
    if digest(value)!=rec['object_sha256']:raise ValueError('object digest mismatch')
   out=bytearray(blank);out[off:off+size]=value
   if digest(out)!=(rec['converted_sha256'] if a.hybrid_manifest and rec['mode']=='steam-stream' else rec['original_sha256']):raise ValueError('asset digest mismatch')
   (a.output_dir/filename).write_bytes(out);out_records.append(rec)
 print(a.action,len(out_records),'animations,',sum(r['size'] for r in out_records),'object bytes;',sum(r['patch_count'] for r in out_records if r.get('mode')!='steam-stream'),'patched bytes;',sum(r.get('mode')=='steam-stream' for r in out_records),'Steam-stream clips; patches and remaining data are NOT redistributable')
except Exception:shutil.rmtree(a.output_dir);raise
