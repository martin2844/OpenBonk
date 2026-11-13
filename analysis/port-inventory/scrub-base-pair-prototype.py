#!/usr/bin/env python3
"""Make local zero-filled APK/OBB research inputs for data rebuilt from Steam.

Only sharedassets0, sharedassets1, sharedassets2 and their two resource files
are scrubbed. Numerous other assets and code remain; outputs are unsigned,
not installable, not vetted for rights, and not redistribution packages.
"""
import argparse,copy,hashlib,json,os,re,shutil,struct,tempfile,zipfile,zlib
from pathlib import Path
APK='980c03b141a4d8dda5ff230d1bc880c4dbd2d5cad074899471813f6f30487581';OBB='baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d'
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'incompatible input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('input_apk',type=Path);p.add_argument('input_obb',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--hashed-mesh-template',type=Path);p.add_argument('--hashed-animation-template',type=Path);p.add_argument('--hashed-texture-template',type=Path);p.add_argument('--hashed-matched-texture-template',type=Path);p.add_argument('--hashed-rgba-template',type=Path);p.add_argument('--hashed-extra-template',type=Path);p.add_argument('--hashed-final-template',type=Path);p.add_argument('--hashed-audio-template',type=Path);p.add_argument('--scene-particle-template',type=Path);p.add_argument('--scene-particle-references-template',type=Path);p.add_argument('--scene-monobehaviour-template',type=Path);a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
require(a.input_apk,APK);require(a.input_obb,OBB)
with tempfile.TemporaryDirectory(prefix='.openbonk-scrub-pair-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp);outobb=tmp/'base.obb';outapk=tmp/'base.apk'
 with zipfile.ZipFile(a.input_apk) as az,zipfile.ZipFile(a.input_obb) as oz:
  an=az.namelist();on=oz.namelist()
  if len(an)!=len(set(an)) or len(on)!=len(set(on)):raise ValueError('duplicate archive member')
  dex=az.read('classes.dex');old=OBB.encode();count,offset=struct.unpack_from('<II',dex,56);offsets=struct.unpack_from(f'<{count}I',dex,offset)
  def dex_string(pos):
   while dex[pos]&0x80:pos+=1
   pos+=1;return dex[pos:dex.index(b'\0',pos)]
  indexes=[i for i,o in enumerate(offsets) if dex_string(o)==old]
  if len(indexes)!=1 or indexes[0] in (0,len(offsets)-1) or dex.count(old)!=1:raise ValueError('DEX OBB reference mismatch')
  x=indexes[0];previous,following=dex_string(offsets[x-1]),dex_string(offsets[x+1]);zero_names=[]
  for stem,expected in (('assets/bin/Data/sharedassets1.assets.split',108),('assets/bin/Data/sharedassets2.assets.split',22)):
   names=[n for n in on if n.startswith(stem)]
   try:ids=sorted(int(n[len(stem):]) for n in names)
   except ValueError:raise ValueError('invalid fragment name')
   if ids!=list(range(expected)):raise ValueError('unexpected fragment count')
   zero_names.extend(names)
  for n in ('assets/bin/Data/sharedassets1.resource','assets/bin/Data/sharedassets2.resource'):
   if on.count(n)!=1:raise ValueError('missing resource')
   zero_names.append(n)
  zero_names=set(zero_names);total=sum(oz.getinfo(n).file_size for n in zero_names)
  mesh_data={}
  if a.hashed_mesh_template:
   manifest=a.hashed_mesh_template/'manifest.json';require(manifest,'44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24')
   records=json.loads(manifest.read_text())['meshes']
   if len(records)!=50 or len({r['file'] for r in records})!=50:raise ValueError('invalid mesh manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid mesh filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1:raise ValueError('missing mesh entry')
    raw=oz.read(entry);path=a.hashed_mesh_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('mesh template mismatch')
    expected=bytearray(raw)
    for kind in ('vertex','index'):
     start,size=r[kind+'_offset'],r[kind+'_size']
     if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid mesh bounds')
     expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected mesh template changes')
    mesh_data[entry]=blank
  animation_data={}
  if a.hashed_animation_template:
   manifest=a.hashed_animation_template/'manifest.json';require(manifest,'d906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8')
   records=json.loads(manifest.read_text())['animations']
   if len(records)!=108 or len({r['file'] for r in records})!=108:raise ValueError('invalid animation manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid animation filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data:raise ValueError('missing or duplicate animation entry')
    raw=oz.read(entry);path=a.hashed_animation_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('animation template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid animation bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected animation template changes')
    animation_data[entry]=blank
  texture_data={}
  if a.hashed_texture_template:
   manifest=a.hashed_texture_template/'manifest.json';require(manifest,'72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9')
   records=json.loads(manifest.read_text())['textures']
   if len(records)!=56 or len({r['file'] for r in records})!=56:raise ValueError('invalid texture manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid texture filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data or entry in animation_data:raise ValueError('missing or duplicate texture entry')
    raw=oz.read(entry);path=a.hashed_texture_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('texture template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid texture bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected texture template changes')
    texture_data[entry]=blank
  matched_texture_data={}
  if a.hashed_matched_texture_template:
   manifest=a.hashed_matched_texture_template/'manifest.json';require(manifest,'7e4bd817c3138434082496ba797c5ef216798357b7f8ef7bc6ecf1fed02061a7')
   records=json.loads(manifest.read_text())['textures']
   if len(records)!=53 or len({r['file'] for r in records})!=53:raise ValueError('invalid matched texture manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid matched texture filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data or entry in animation_data or entry in texture_data:raise ValueError('missing or duplicate matched texture entry')
    raw=oz.read(entry);path=a.hashed_matched_texture_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('matched texture template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid matched texture bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected matched texture template changes')
    matched_texture_data[entry]=blank
  rgba_data={}
  if a.hashed_rgba_template:
   manifest=a.hashed_rgba_template/'manifest.json';require(manifest,'828fde47398ad39c9a0a42d2afba85111dd577f704f3fd5740c4ddcb5e79c76d')
   records=json.loads(manifest.read_text())['textures']
   if len(records)!=10 or len({r['file'] for r in records})!=10:raise ValueError('invalid RGBA texture manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid RGBA texture filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data or entry in animation_data or entry in texture_data or entry in matched_texture_data:raise ValueError('missing or duplicate RGBA entry')
    raw=oz.read(entry);path=a.hashed_rgba_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('RGBA template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid RGBA bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected RGBA template changes')
    rgba_data[entry]=blank
  extra_data={}
  if a.hashed_extra_template:
   manifest=a.hashed_extra_template/'manifest.json';require(manifest,'34e87d8502957407489faca56ae44db02c1ed7d7eed9b2363ffdb44f44504f44')
   records=json.loads(manifest.read_text())['textures']
   if len(records)!=13 or len({r['file'] for r in records})!=13:raise ValueError('invalid extra texture manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid extra texture filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data or entry in animation_data or entry in texture_data or entry in matched_texture_data or entry in rgba_data:raise ValueError('missing or duplicate extra texture entry')
    raw=oz.read(entry);path=a.hashed_extra_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('extra texture template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid extra texture bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected extra texture template changes')
    extra_data[entry]=blank
  final_data={}
  if a.hashed_final_template:
   manifest=a.hashed_final_template/'manifest.json';require(manifest,'e2d7f81ff5d42db9b8e698b063212e3abf0cffdcbe27d12737fa78739370e6cd')
   records=json.loads(manifest.read_text())['textures']
   if len(records)!=2 or len({r['file'] for r in records})!=2:raise ValueError('invalid final texture manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid final texture filename')
    entry='assets/bin/Data/'+name
    if on.count(entry)!=1 or entry in mesh_data or entry in animation_data or entry in texture_data or entry in matched_texture_data or entry in rgba_data or entry in extra_data:raise ValueError('missing or duplicate final texture entry')
    raw=oz.read(entry);path=a.hashed_final_template/name;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=r['original_sha256'] or len(raw)!=len(blank):raise ValueError('final texture template mismatch')
    start,size=r['offset'],r['size']
    if not isinstance(start,int) or not isinstance(size,int) or min(start,size)<0 or start+size>len(raw):raise ValueError('invalid final texture bounds')
    expected=bytearray(raw);expected[start:start+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected final texture template changes')
    final_data[entry]=blank
  hashed_audio_names=set()
  if a.hashed_audio_template:
   manifest=a.hashed_audio_template/'manifest.json';require(manifest,'6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f')
   records=json.loads(manifest.read_text())['clips']
   if len(records)!=63 or len({r['file'] for r in records})!=63:raise ValueError('invalid hashed audio manifest')
   for r in records:
    name=r['file']
    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid hashed audio filename')
    entry='assets/bin/Data/'+name;resource=entry+'.resource'
    if on.count(entry)!=1 or on.count(resource)!=1 or entry in mesh_data or entry in animation_data or entry in texture_data or entry in matched_texture_data or entry in rgba_data or entry in extra_data or entry in final_data or resource in zero_names:raise ValueError('missing or duplicated audio entry')
    require(a.hashed_audio_template/name,r['asset_template_sha256'])
    if hashlib.sha256(oz.read(entry)).hexdigest()!=r['asset_template_sha256']:raise ValueError('audio object template mismatch')
    raw=oz.read(resource)
    if len(raw)!=r['original_size'] or hashlib.sha256(raw).hexdigest()!=r['original_resource_sha256']:raise ValueError('audio resource mismatch')
    hashed_audio_names.add(resource)
  scene_data={}
  if a.scene_particle_template:
   manifest=a.scene_particle_template/'manifest.json';require(manifest,'733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be')
   records=json.loads(manifest.read_text())['scenes'];expected_counts={'level1':46,'level2':176,'level4':175}
   if {r['scene'] for r in records}!=set(expected_counts) or len(records)!=3:raise ValueError('invalid scene manifest')
   for r in records:
    scene=r['scene'];prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in on if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
    if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))) or len(r['objects'])!=expected_counts[scene]:raise ValueError('invalid scene fragments')
    original=b''.join(oz.read(n) for n in parts);path=a.scene_particle_template/scene;require(path,r['template_sha256']);blank=path.read_bytes()
    if hashlib.sha256(original).hexdigest()!=r['original_sha256'] or len(original)!=len(blank) or len(blank)!=r['size']:raise ValueError('scene template mismatch')
    expected=bytearray(original);objects=sorted(r['objects'],key=lambda x:x['offset'])
    for x in objects:
     off,size=x['offset'],x['size']
     if not isinstance(off,int) or not isinstance(size,int) or min(off,size)<0 or off+size>len(expected):raise ValueError('invalid scene range')
     expected[off:off+size]=bytes(size)
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(objects,objects[1:])) or expected!=blank:raise ValueError('unexpected scene changes')
    off=0
    for name in parts:
     size=oz.getinfo(name).file_size;scene_data[name]=blank[off:off+size];off+=size
    if off!=len(blank):raise ValueError('scene fragment length mismatch')
  reference_count=0
  if a.scene_particle_references_template:
   if not a.scene_particle_template:raise ValueError('particle references require particle body template')
   manifest=a.scene_particle_references_template/'manifest.json';require(manifest,'cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0')
   records=json.loads(manifest.read_text())['scenes'];counts={'level1':7,'level2':10,'level4':22}
   if len(records)!=3 or {r['scene'] for r in records}!=set(counts):raise ValueError('invalid particle references manifest')
   for r in records:
    scene=r['scene'];prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in scene_data if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]));original=b''.join(scene_data[n] for n in parts);path=a.scene_particle_references_template/scene;require(path,r['template_sha256']);blank=path.read_bytes()
    if len(r['objects'])!=counts[scene] or hashlib.sha256(original).hexdigest()!=r['original_sha256'] or len(original)!=len(blank) or len(blank)!=r['size']:raise ValueError('particle reference template mismatch')
    expected=bytearray(original);objects=sorted(r['objects'],key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(objects,objects[1:])):raise ValueError('overlapping particles')
    for x in objects:
     off,size=x['offset'],x['size']
     if not isinstance(off,int) or not isinstance(size,int) or min(off,size)<0 or off+size>len(original):raise ValueError('invalid particle range')
     body=bytearray(size)
     for ref in x['references']:
      pos=ref['offset']-12;n=ref['size']
      if n!=12 or pos<0 or pos+n>size:raise ValueError('invalid retained pointer')
      value=original[off+pos:off+pos+n]
      if hashlib.sha256(value).hexdigest()!=ref['pointer_sha256']:raise ValueError('retained pointer mismatch')
      body[pos:pos+n]=value
     expected[off:off+size]=body
    if expected!=blank:raise ValueError('unexpected particle reference template changes')
    off=0
    for name in parts:size=oz.getinfo(name).file_size;scene_data[name]=blank[off:off+size];off+=size
    if off!=len(blank):raise ValueError('scene fragment length mismatch')
    reference_count+=len(objects)
  mono_count=0
  if a.scene_monobehaviour_template:
   if not a.scene_particle_references_template:raise ValueError('MonoBehaviour templates require particle reference templates')
   manifest=a.scene_monobehaviour_template/'manifest.json';require(manifest,'b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4')
   records=json.loads(manifest.read_text())['scenes'];counts={'level1':1570,'level2':1194,'level4':1163}
   if len(records)!=3 or {r['scene'] for r in records}!=set(counts):raise ValueError('invalid MonoBehaviour manifest')
   for r in records:
    scene=r['scene'];prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in scene_data if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]));original=b''.join(scene_data[n] for n in parts);path=a.scene_monobehaviour_template/scene;require(path,r['template_sha256']);blank=path.read_bytes()
    if len(r['objects'])!=counts[scene] or hashlib.sha256(original).hexdigest()!=r['original_sha256'] or len(original)!=len(blank) or len(blank)!=r['size']:raise ValueError('MonoBehaviour template mismatch')
    expected=bytearray(original);objects=sorted(r['objects'],key=lambda x:x['offset'])
    if any(x['offset']+x['size']>y['offset'] for x,y in zip(objects,objects[1:])):raise ValueError('overlapping MonoBehaviour objects')
    for x in objects:
     off,size=x['offset'],x['size']
     if not isinstance(off,int) or not isinstance(size,int) or min(off,size)<0 or off+size>len(original):raise ValueError('invalid MonoBehaviour range')
     expected[off:off+size]=bytes(size)
    if expected!=blank:raise ValueError('unexpected MonoBehaviour template changes')
    off=0
    for name in parts:size=oz.getinfo(name).file_size;scene_data[name]=blank[off:off+size];off+=size
    if off!=len(blank):raise ValueError('scene fragment length mismatch')
    mono_count+=len(objects)
  zero_names.update(hashed_audio_names)
  total+=sum(oz.getinfo(n).file_size for n in hashed_audio_names)
  with zipfile.ZipFile(outobb,'w',allowZip64=True) as dest:
   for info in oz.infolist():dest.writestr(copy.copy(info),bytes(info.file_size) if info.filename in zero_names else mesh_data[info.filename] if info.filename in mesh_data else animation_data[info.filename] if info.filename in animation_data else texture_data[info.filename] if info.filename in texture_data else matched_texture_data[info.filename] if info.filename in matched_texture_data else rgba_data[info.filename] if info.filename in rgba_data else extra_data[info.filename] if info.filename in extra_data else final_data[info.filename] if info.filename in final_data else scene_data[info.filename] if info.filename in scene_data else oz.read(info))
   dest.comment=b'00000000'
  h=hashlib.sha256()
  with outobb.open('rb') as f:
   remaining=outobb.stat().st_size-8
   while remaining:
    b=f.read(min(1048576,remaining))
    if not b:raise ValueError('short OBB read')
    h.update(b);remaining-=len(b)
  for nonce in range(1_000_000):
   comment=f'{nonce:08x}'.encode();d=h.copy();d.update(comment);new=d.hexdigest().encode()
   if previous<new<following:break
  else:raise ValueError('could not preserve DEX string ordering')
  with outobb.open('r+b') as f:f.seek(-8,2);f.write(comment)
  if sha(outobb).encode()!=new:raise ValueError('OBB hash verification failed')
  dex=bytearray(dex.replace(old,new));dex[12:32]=hashlib.sha1(dex[32:]).digest();dex[8:12]=(zlib.adler32(dex[12:])&0xffffffff).to_bytes(4,'little')
  with zipfile.ZipFile(outapk,'w',allowZip64=True) as dest:
   for info in az.infolist():dest.writestr(copy.copy(info),dex if info.filename=='classes.dex' else az.read(info))
 with zipfile.ZipFile(outobb) as check:
  if len(check.namelist())!=len(set(check.namelist())) or any(any(check.read(n)) for n in zero_names) or check.testzip() is not None:raise ValueError('OBB verification failed')
 with zipfile.ZipFile(outapk) as check:
  if new not in check.read('classes.dex') or check.testzip() is not None:raise ValueError('APK verification failed')
 a.output_dir.mkdir()
 try:
  shutil.move(str(outapk),str(a.output_dir/'base.apk'));shutil.move(str(outobb),str(a.output_dir/'base.obb'))
 except Exception:shutil.rmtree(a.output_dir);raise
 print('Zeroed',len(zero_names),'OBB entries,',total,'bytes; original sharedassets0 remains zero in APK')
 print('Replaced',len(mesh_data),'hashed mesh entries with zero-buffer templates')
 print('Replaced',len(animation_data),'hashed animation entries with zero-object templates')
 print('Replaced',len(texture_data),'hashed texture entries with zero-payload templates')
 print('Replaced',len(matched_texture_data),'matched hashed texture entries with zero-payload templates')
 print('Replaced',len(rgba_data),'RGBA candidate entries with zero-payload templates')
 print('Replaced',len(extra_data),'extra candidate entries with zero-payload templates')
 print('Replaced',len(final_data),'final candidate entries with zero-payload templates')
 print('Zeroed',len(hashed_audio_names),'standalone hashed audio resources')
 print('Replaced',len(scene_data),'scene fragments with particle-body templates')
 print('Applied',reference_count,'particle templates retaining Android references')
 print('Applied',mono_count,'MonoBehaviour body templates retaining Android headers')
 print('base.apk',sha(a.output_dir/'base.apk'));print('base.obb',sha(a.output_dir/'base.obb'))
 print('Other data and code remain: not distributable.')
