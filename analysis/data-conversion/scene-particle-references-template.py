#!/usr/bin/env python3
"""Rebuild 39 scene particle bodies while preserving Android PPtr fields.

The Android GameObject and sub-emitter references are retained. Matching data,
names or byte-perfect restoration do not establish distribution rights.
"""
import argparse,hashlib,json,shutil,struct,zipfile
from pathlib import Path
import UnityPy
PINS={'level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'}
OBB='bada442aa38b729666292968dac5b7771eeb620e045782356d7a1cf3ea9b7176';INVENTORY='1edba7913cc76f97ab6c16bed35f60e6f52792b12de1530ed8604628a963fa63';EXPECTED={'level1':7,'level2':10,'level4':22}
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
 if not a.inventory:p.error('template requires --inventory')
 require(a.inventory,INVENTORY);rows=json.loads(a.inventory.read_text())['results'];plans={n:[x for x in rows if x['scene']==n] for n in PINS}
else:
 manifest=json.loads((a.input/'manifest.json').read_text());plans={x['scene']:x for x in manifest['scenes']}
 if manifest['version']!=1 or manifest['base_obb_sha256']!=OBB or set(plans)!=set(PINS):raise ValueError('invalid manifest')
a.output.mkdir();result=[]
try:
 z=zipfile.ZipFile(a.input) if a.action=='template' else None
 try:
  for scene in PINS:
   if z:
    prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
    if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid scene fragments')
    original=b''.join(z.read(n) for n in parts);env=UnityPy.load(original);targets={o.path_id:o for o in env.objects};work=bytearray(original);records=[]
    if len(plans[scene])!=EXPECTED[scene]:raise ValueError('unexpected record count')
    for r in plans[scene]:
     if not r['candidates']:raise ValueError('unmatched particle')
     candidate=min(r['candidates'],key=lambda x:x['source_id']);o=targets[r['target_id']];src=sources[scene][candidate['source_id']]
     if o.type.name!='ParticleSystem' or src.type.name!='ParticleSystem':raise ValueError('unexpected object type')
     d=o.read();s=src.read();old=o.get_raw_data();body=src.get_raw_data();off=o.byte_start
     if d.m_GameObject.read().m_Name!=r['name'] or s.m_GameObject.read().m_Name!=r['name'] or len(old)!=r['size'] or len(body)!=len(old) or old[:12]!=struct.pack('<iq',d.m_GameObject.m_FileID,d.m_GameObject.m_PathID) or original[off:off+len(old)]!=old:raise ValueError('invalid target')
     refs=sorted(candidate['references'],key=lambda x:x['offset']);candidate_body=bytearray(body);segments=[]
     for ref in refs:
      pos=ref['offset'];n=ref['size']
      if n!=12 or pos<12 or pos+n>len(old):raise ValueError('invalid pointer range')
      expected_target=struct.pack('<iq',ref['android_file_id'],ref['android_path_id']);expected_source=struct.pack('<iq',ref['pc_file_id'],ref['pc_path_id'])
      if old[pos:pos+n]!=expected_target or body[pos:pos+n]!=expected_source:raise ValueError('pointer mismatch')
      candidate_body[pos:pos+n]=expected_target;segments.append({'offset':pos,'size':n,'pointer_sha256':digest(expected_target)})
     if any(x['offset']+x['size']>y['offset'] for x,y in zip(segments,segments[1:])) or candidate_body[12:]!=old[12:]:raise ValueError('non-pointer body difference')
     blank=bytearray(len(old)-12)
     for x in segments:pos=x['offset'];blank[pos-12:pos]=old[pos:pos+12]
     work[off+12:off+len(old)]=blank
     records.append({'target_id':o.path_id,'source_id':src.path_id,'offset':off+12,'size':len(old)-12,'source_sha256':digest(body),'body_sha256':digest(old[12:]),'references':segments})
    ordered=sorted(records,key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping objects')
    blank=bytes(work);(a.output/scene).write_bytes(blank);result.append({'scene':scene,'size':len(blank),'original_sha256':digest(original),'template_sha256':digest(blank),'objects':records})
   else:
    r=plans[scene];require(a.input/scene,r['template_sha256']);work=bytearray((a.input/scene).read_bytes());env=UnityPy.load(bytes(work));targets={o.path_id:o for o in env.objects};records=r['objects']
    if len(work)!=r['size'] or len(records)!=EXPECTED[scene]:raise ValueError('invalid template')
    ordered=sorted(records,key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(ordered,ordered[1:])):raise ValueError('overlapping objects')
    for x in records:
     off=x['offset'];size=x['size'];o=targets[x['target_id']];src=sources[scene][x['source_id']];source=src.get_raw_data()
     if o.type.name!='ParticleSystem' or src.type.name!='ParticleSystem' or off!=o.byte_start+12 or o.byte_size!=size+12 or len(source)!=size+12 or digest(source)!=x['source_sha256']:raise ValueError('source/target mismatch')
     template=bytes(work[off:off+size]);expected=bytearray(size);body=bytearray(source[12:]);segments=sorted(x['references'],key=lambda t:t['offset'])
     if any(y['offset']+y['size']>q['offset'] for y,q in zip(segments,segments[1:])):raise ValueError('overlapping references')
     for ref in segments:
      pos=ref['offset']-12;n=ref['size']
      if n!=12 or pos<0 or pos+n>size:raise ValueError('invalid reference')
      ptr=template[pos:pos+n]
      if digest(ptr)!=ref['pointer_sha256']:raise ValueError('retained pointer mismatch')
      expected[pos:pos+n]=ptr;body[pos:pos+n]=ptr
     if template!=expected or digest(body)!=x['body_sha256']:raise ValueError('restored particle mismatch')
     work[off:off+size]=body
    if digest(work)!=r['original_sha256']:raise ValueError('restored scene mismatch')
    (a.output/scene).write_bytes(work);result.append(r)
 finally:
  if z:z.close()
 if z is not None:(a.output/'manifest.json').write_text(json.dumps({'version':1,'base_obb_sha256':OBB,'scenes':result},indent=2)+'\n')
 print(a.action,sum(len(x['objects']) for x in result),'particles; preserved Android pointers; other game data remains, not distributable')
except Exception:shutil.rmtree(a.output);raise
