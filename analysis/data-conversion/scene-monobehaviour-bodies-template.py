#!/usr/bin/env python3
"""Rebuild exact MonoBehaviour bodies while retaining Android 28-byte headers.

The GameObject, enabled state and script references remain Android-derived.
Exact reconstruction does not establish provenance or distribution rights.
"""
import argparse,hashlib,json,shutil,zipfile
from pathlib import Path
import UnityPy
PINS={'level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'}
OBB='baf36d26d1ed49517c68ce02f92ceb12a29ea7971185ac3ac7db2182d8ade7fd';INVENTORY='70530989fec3e91773ccd33b17b49a5ba929756a3d866f00e8c78a65675eef94';EXPECTED={'level1':1570,'level2':1194,'level4':1163};PREFIX=28
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError('incompatible input '+str(p))
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path);p.add_argument('--inventory',type=Path);a=p.parse_args()
if a.output.exists() or not a.output.parent.is_dir():p.error('output must not exist and parent must exist')
for n,h in PINS.items():require(a.pc_data/n,h)
envs={n:UnityPy.load(str(a.pc_data/n)) for n in PINS};sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.inventory:p.error('inventory required')
 require(a.inventory,INVENTORY);m=json.loads(a.inventory.read_text());plans={x['scene']:x['rows'] for x in m['scenes']}
 if m['errors'] or set(plans)!=set(PINS):raise ValueError('invalid inventory')
else:
 m=json.loads((a.input/'manifest.json').read_text());plans={x['scene']:x for x in m['scenes']}
 if m['version']!=1 or m['base_obb_sha256']!=OBB or set(plans)!=set(PINS):raise ValueError('invalid manifest')
a.output.mkdir();out=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for scene in PINS:
   if z:
    prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
    if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('scene fragments missing')
    original=b''.join(z.read(n) for n in parts);env=UnityPy.load(original);targets={o.path_id:o for o in env.objects};work=bytearray(original);records=[]
    for r in plans[scene]:
     if r['type']!='MonoBehaviour':continue
     o=targets[r['target_id']];raw=o.get_raw_data()
     if not any(raw[PREFIX:]):continue
     sid=min(r['source_ids']);src=sources[scene][sid];pcraw=src.get_raw_data();off=o.byte_start
     if o.type.name!='MonoBehaviour' or src.type.name!='MonoBehaviour' or r['prefix']!=PREFIX or len(raw)!=r['size'] or len(raw)!=len(pcraw) or digest(raw)!=r['target_sha256'] or raw[PREFIX:]!=pcraw[PREFIX:] or original[off:off+len(raw)]!=raw:raise ValueError('component mismatch')
     work[off+PREFIX:off+len(raw)]=bytes(len(raw)-PREFIX)
     records.append({'target_id':o.path_id,'source_id':sid,'offset':off+PREFIX,'size':len(raw)-PREFIX,'body_sha256':digest(pcraw[PREFIX:])})
    if len(records)!=EXPECTED[scene]:raise ValueError('unexpected matched component count')
    ordered=sorted(records,key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping ranges')
    blank=bytes(work);(a.output/scene).write_bytes(blank);out.append({'scene':scene,'size':len(blank),'original_sha256':digest(original),'template_sha256':digest(blank),'objects':records})
   else:
    r=plans[scene];require(a.input/scene,r['template_sha256']);work=bytearray((a.input/scene).read_bytes());env=UnityPy.load(bytes(work));targets={o.path_id:o for o in env.objects};records=r['objects']
    if len(work)!=r['size'] or len(records)!=EXPECTED[scene]:raise ValueError('invalid template')
    ordered=sorted(records,key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping ranges')
    for x in records:
     off=x['offset'];size=x['size'];o=targets[x['target_id']];src=sources[scene][x['source_id']];data=src.get_raw_data()
     if o.type.name!='MonoBehaviour' or src.type.name!='MonoBehaviour' or o.byte_start+PREFIX!=off or o.byte_size!=size+PREFIX or len(data)!=size+PREFIX or digest(data[PREFIX:])!=x['body_sha256'] or any(work[off:off+size]):raise ValueError('template/source mismatch')
     work[off:off+size]=data[PREFIX:]
    if digest(work)!=r['original_sha256']:raise ValueError('restored scene mismatch')
    (a.output/scene).write_bytes(work);out.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'scenes':out},indent=2)+'\n')
 print(a.action,sum(len(x['objects']) for x in out),'MonoBehaviour bodies;',sum(sum(v['size'] for v in x['objects']) for x in out),'bytes; Android headers and other data remain, not distributable')
except Exception:shutil.rmtree(a.output);raise
