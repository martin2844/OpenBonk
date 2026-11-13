#!/usr/bin/env python3
"""Rebuild exact scene particle object bodies from pinned Steam scene data.

The 12-byte Unity GameObject pointer stays in the Android template. Other scene
objects and metadata remain; neither templates nor outputs are distributable.
"""
import argparse,hashlib,json,shutil,struct,zipfile
from pathlib import Path
import UnityPy
PINS={'level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'}
OBB='bafbe7ba6be507da28e65e87f0ff41f603e76a8badc7d14d1ad3b48a5ec4ce7a';INVENTORY='11f168189f9accefaece4ce1120043e487f854b8577095a48fdb9ca955f8f7a6';EXPECTED={'level1':46,'level2':176,'level4':175}
def digest(x):return hashlib.sha256(x).hexdigest()
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
envs={n:UnityPy.load(str(a.pc_data/n)) for n in PINS};sources={n:{o.path_id:o for o in env.objects} for n,env in envs.items()}
if a.action=='template':
 require(a.input,OBB)
 if not a.inventory:p.error('template requires --inventory')
 require(a.inventory,INVENTORY);inventory=json.loads(a.inventory.read_text());plans={r['scene']:r['rows'] for r in inventory['scenes']}
 if inventory['errors'] or set(plans)!=set(PINS):raise ValueError('invalid inventory')
else:
 m=json.loads((a.input/'manifest.json').read_text());plans={r['scene']:r for r in m['scenes']}
 if m['version']!=1 or m['base_obb_sha256']!=OBB or set(plans)!=set(PINS):raise ValueError('invalid manifest')
a.output.mkdir();out=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for scene in PINS:
   if z:
    prefix='assets/bin/Data/'+scene+'.split';names=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
    if not names or [int(n[len(prefix):]) for n in names]!=list(range(len(names))):raise ValueError('scene fragments missing')
    original=b''.join(z.read(n) for n in names);env=UnityPy.load(original);targets={o.path_id:o for o in env.objects};work=bytearray(original);records=[]
    for r in plans[scene]:
     matches=[m for m in r['matches'] if m['different_bytes_after_12']==0]
     if not matches:continue
     candidate=min(matches,key=lambda m:m['id']);o=targets[r['id']];src=sources[scene][candidate['id']]
     if o.type.name!='ParticleSystem' or src.type.name!='ParticleSystem':raise ValueError('invalid object type')
     d=o.read();sd=src.read();old=o.get_raw_data();source=src.get_raw_data();off=o.byte_start
     if d.m_GameObject.read().m_Name!=r['name'] or sd.m_GameObject.read().m_Name!=r['name'] or len(old)!=r['size'] or len(old)!=len(source) or old[:12]!=struct.pack('<iq',d.m_GameObject.m_FileID,d.m_GameObject.m_PathID) or old[12:]!=source[12:] or digest(old)!=r['sha256'] or digest(source)!=candidate['sha256'] or original[off:off+len(old)]!=old:raise ValueError('particle mismatch')
     work[off+12:off+len(old)]=bytes(len(old)-12)
     records.append({'target_id':o.path_id,'source_id':src.path_id,'name':r['name'],'offset':off+12,'size':len(old)-12,'body_sha256':digest(source[12:])})
    if len(records)!=EXPECTED[scene]:raise ValueError('unexpected matched particle count')
    ordered=sorted(records,key=lambda r:r['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping object ranges')
    blank=bytes(work);(a.output/scene).write_bytes(blank);out.append({'scene':scene,'original_sha256':digest(original),'template_sha256':digest(blank),'size':len(blank),'objects':records})
   else:
    r=plans[scene];require(a.input/scene,r['template_sha256']);work=bytearray((a.input/scene).read_bytes());records=r['objects']
    if len(records)!=EXPECTED[scene] or len(work)!=r['size']:raise ValueError('invalid template')
    env=UnityPy.load(bytes(work));targets={o.path_id:o for o in env.objects};ordered=sorted(records,key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping object ranges')
    for x in records:
     off=x['offset'];size=x['size'];o=targets[x['target_id']];src=sources[scene][x['source_id']];source=src.get_raw_data()
     if o.type.name!='ParticleSystem' or src.type.name!='ParticleSystem' or o.byte_start+12!=off or o.byte_size!=size+12 or src.read().m_GameObject.read().m_Name!=x['name'] or len(source)!=size+12 or digest(source[12:])!=x['body_sha256'] or any(work[off:off+size]):raise ValueError('template/source mismatch')
     work[off:off+size]=source[12:]
    if digest(work)!=r['original_sha256']:raise ValueError('restored scene mismatch')
    (a.output/scene).write_bytes(work);out.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'scenes':out},indent=2)+'\n')
 print(a.action,sum(len(r['objects']) for r in out),'particle bodies;',sum(sum(x['size'] for x in r['objects']) for r in out),'bytes; other content remains, not distributable')
except Exception:shutil.rmtree(a.output);raise
