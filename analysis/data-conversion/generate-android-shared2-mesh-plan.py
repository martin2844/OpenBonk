#!/usr/bin/env python3
"""Map exact mesh buffers into the experimental second-map asset."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'localization-shared-scrubbed-base/base.obb';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip')
NAMES=('sharedassets2.assets','sharedassets2.assets.resS','level1','level1.resS','level2','level2.resS')
PINS=(('e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6','1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51'),('cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6','853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6','544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51'))
MANIFEST=B/'shared2-mesh-object-delta/mesh-manifest.json';OUTPUT=B/'shared2-mesh-android-plan/plan-v7.bin'
def sha(b):return hashlib.sha256(b).digest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 for p,expected in ((BASE,'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'),(TARGET,'e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691')):
  with p.open('rb') as f:
   if hashlib.file_digest(f,'sha256').hexdigest()!=expected:raise ValueError('OBB pin differs')
 def asset(z):return b''.join(z.read('assets/bin/Data/sharedassets2.assets.split'+str(i)) for i in range(22))
 with zipfile.ZipFile(BASE) as z:base=asset(z)
 with zipfile.ZipFile(TARGET) as z:target=asset(z)
 if len(base)!=len(target):raise ValueError('asset lengths differ')
 tobj={o.path_id:o for o in UnityPy.load(target).objects};bobj={o.path_id:o for o in UnityPy.load(base).objects};sourceData=[];sourceObjects=[]
 for path,pins in zip(ZIPS,PINS):
  with zipfile.ZipFile(path) as z:files=[z.read('Megabonk_Data/'+n) for n in NAMES]
  if tuple(sha(x).hex() for x in files)!=pins:raise ValueError('Steam source pin differs')
  sourceData.append(files);sourceObjects.append({n:{o.path_id:o for o in UnityPy.load(files[NAMES.index(n)]).objects} for n in ('sharedassets2.assets','level1','level2')})
 recs=json.loads(MANIFEST.read_text())['meshes']
 if len(recs)!=251 or len({r['target_id'] for r in recs})!=251:raise ValueError('invalid mesh inventory')
 work=bytearray(base);ranges=[]
 for rec in recs:
  dst=tobj[rec['target_id']];old=bobj[rec['target_id']]
  if dst.type.name!='Mesh' or dst.byte_start!=old.byte_start:raise ValueError('target mesh layout differs')
  obj=dst.read();raw=dst.get_raw_data()
  for field in ('vertices','indices'):
   v=bytes(obj.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(obj.m_IndexBuffer or [])
   if len(v)!=rec[field+'_size'] or sha(v).hex()!=rec[field+'_sha256'] or not v or raw.count(v)!=1:raise ValueError('target mesh payload mismatch')
   offset=dst.byte_start+raw.index(v);refs=[];values=[]
   for files,index in zip(sourceData,sourceObjects):
    src=index[rec['source_file']][rec['source_id']];m=src.read();objRaw=src.get_raw_data()
    payload=bytes(m.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(m.m_IndexBuffer or [])
    if field=='vertices' and not payload:
     s=m.m_StreamData
     if not s or s.path not in NAMES:raise ValueError('unsupported mesh stream')
     fid=NAMES.index(s.path);start=s.offset;payload=files[fid][start:start+s.size]
    else:
     if not payload or objRaw.count(payload)!=1:raise ValueError('ambiguous inline mesh')
     fid=NAMES.index(rec['source_file']);start=src.byte_start+objRaw.index(payload)
    if files[fid][start:start+len(payload)]!=payload:raise ValueError('source range differs')
    refs.append((fid,start));values.append(payload)
   if values[0]!=v or values[1]!=v or any(work[offset:offset+len(v)]):raise ValueError('mesh template or source differs')
   work[offset:offset+len(v)]=v;ranges.append((offset,len(v),refs,sha(v)))
 materials=json.loads((B/'localization-shared-scrubbed-base/material-extra-2/manifest.json').read_text())['objects']
 if len(materials)!=102:raise ValueError('invalid material inventory')
 for rec in materials:
  dst=tobj[rec['target_id']];old=bobj[rec['target_id']];n=rec['length'];raw=dst.get_raw_data();offset=dst.byte_start+len(raw)-n
  if dst.type.name!='Material' or dst.byte_start!=old.byte_start or n<=0 or sha(raw[-n:]).hex()!=rec['value_sha256'] or any(work[offset:offset+n]):raise ValueError('material target mismatch')
  refs=[];values=[]
  for files,index in zip(sourceData,sourceObjects):
   src=index['sharedassets2.assets'][rec['steam_id']];sourceRaw=src.get_raw_data();start=src.byte_start+len(sourceRaw)-n;value=sourceRaw[-n:]
   if sha(value).hex()!=rec['value_sha256'] or files[0][start:start+n]!=value:raise ValueError('material source differs')
   values.append(value);refs.append((0,start))
  if values[0]!=raw[-n:] or values[1]!=values[0]:raise ValueError('material payload differs')
  work[offset:offset+n]=values[0];ranges.append((offset,n,refs,sha(values[0])))
 completeMeshes=0;completed=set()
 for rec in recs:
  dst=tobj[rec['target_id']];mesh=dst.read();raw=dst.get_raw_data();v=bytes(mesh.m_VertexData.m_DataSize);idx=bytes(mesh.m_IndexBuffer)
  intervals=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))))
  spans=((0,intervals[0][0]),(intervals[0][1],intervals[1][0]),(intervals[1][1],len(raw)));pieces=[raw[x:y] for x,y in spans]
  source=[index[rec['source_file']][rec['source_id']] for index in sourceObjects]
  if not all(all(not any(piece) or obj.get_raw_data().count(piece)==1 for piece in pieces) for obj in source):continue
  for (lo,hi),piece in zip(spans,pieces):
   if not any(piece):continue
   dstOffset=dst.byte_start+lo;refs=[]
   for files,obj in zip(sourceData,source):
    fid=NAMES.index(rec['source_file']);sourceOffset=obj.byte_start+obj.get_raw_data().index(piece)
    if files[fid][sourceOffset:sourceOffset+len(piece)]!=piece:raise ValueError('mesh metadata source differs')
    refs.append((fid,sourceOffset))
   if any(work[dstOffset:dstOffset+len(piece)]):raise ValueError('mesh metadata target not blank')
   work[dstOffset:dstOffset+len(piece)]=piece;ranges.append((dstOffset,len(piece),refs,sha(piece)))
  if work[dst.byte_start:dst.byte_start+len(raw)]!=raw:raise ValueError('complete mesh metadata differs')
  completeMeshes+=1;completed.add(rec['target_id'])
 if completeMeshes!=31:raise ValueError('unexpected direct mesh metadata count: '+str(completeMeshes))
 nameIndexes=[]
 for index in sourceObjects:
  names={}
  for file,byId in index.items():
   for obj in byId.values():
    if obj.type.name=='Mesh':names.setdefault(obj.read().m_Name,[]).append((file,obj))
  nameIndexes.append(names)
 flags=[]
 for rec in recs:
  if rec['target_id'] in completed:continue
  dst=tobj[rec['target_id']];mesh=dst.read();raw=dst.get_raw_data();v=bytes(mesh.m_VertexData.m_DataSize);idx=bytes(mesh.m_IndexBuffer)
  intervals=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))))
  spans=((0,intervals[0][0]),(intervals[0][1],intervals[1][0]),(intervals[1][1],len(raw)));pieces=[raw[x:y] for x,y in spans];prefix=pieces[0]
  name=mesh.m_Name.encode('utf-8');nameLen=int.from_bytes(prefix[:4],'little')
  if nameLen!=len(name) or prefix[4:4+nameLen]!=name or not name:continue
  source=[index[rec['source_file']][rec['source_id']] for index in sourceObjects];srcRaw=[obj.get_raw_data() for obj in source]
  if any(len(s)<len(prefix) or int.from_bytes(s[:4],'little')!=nameLen for s in srcRaw):continue
  differences=set();valid=True
  for src in srcRaw:
   diff=[j for j in range(len(prefix)) if src[j]!=prefix[j] and not 4<=j<4+nameLen]
   if len(diff)>2 or any(src[j]!=0 or prefix[j]!=1 for j in diff):valid=False;break
   differences.update(diff)
  if not valid or len(differences)>2 or any(any(piece) and src.count(piece)!=1 for src in srcRaw for piece in pieces[1:]):continue
  nameRefs=[]
  for files,names in zip(sourceData,nameIndexes):
   candidates=names.get(mesh.m_Name,[])
   if not candidates:break
   nameFile,nameObj=candidates[0];fid=NAMES.index(nameFile);offset=nameObj.byte_start+4
   if files[fid][offset:offset+len(name)]!=name:raise ValueError('name location differs')
   nameRefs.append((fid,offset))
  if len(nameRefs)!=2:continue
  segments=[];blocked=set(range(4,4+nameLen))|differences;cursor=0
  for j in range(len(prefix)+1):
   if j==len(prefix) or j in blocked:
    if cursor<j and any(prefix[cursor:j]):segments.append((cursor,j,[(NAMES.index(rec['source_file']),obj.byte_start+cursor) for obj in source]))
    cursor=j+1
  segments.append((4,4+nameLen,nameRefs))
  for (lo,hi),piece in zip(spans[1:],pieces[1:]):
   if any(piece):segments.append((lo,hi,[(NAMES.index(rec['source_file']),obj.byte_start+obj.get_raw_data().index(piece)) for obj in source]))
  for lo,hi,refs in segments:
   value=raw[lo:hi];offset=dst.byte_start+lo
   if any(work[offset:offset+len(value)]) or any(files[fid][start:start+len(value)]!=value for files,(fid,start) in zip(sourceData,refs)):raise ValueError('derived mesh metadata mismatch')
   work[offset:offset+len(value)]=value;ranges.append((offset,len(value),refs,sha(value)))
  for pos in sorted(differences):
   where=dst.byte_start+pos
   if work[where]!=0 or raw[pos]!=1:raise ValueError('mesh boolean mismatch')
   work[where]=1;flags.append(where)
  if work[dst.byte_start:dst.byte_start+len(raw)]!=raw:raise ValueError('derived mesh differs')
  completeMeshes+=1;completed.add(rec['target_id'])
 def commit(offset,value,refs):
  if not value or not any(value):return
  if any(work[offset:offset+len(value)]) or any(files[fid][start:start+len(value)]!=value for files,(fid,start) in zip(sourceData,refs)):raise ValueError('metadata copy mismatch')
  work[offset:offset+len(value)]=value;ranges.append((offset,len(value),refs,sha(value)))
 def commitPrefix(dst,raw,prefix,selected):
  sourceRaw=[obj.get_raw_data() for _,obj in selected];diffs=set()
  for src in sourceRaw:
   if len(src)<len(prefix):return False
   mismatch=[j for j in range(len(prefix)) if src[j]!=prefix[j]]
   if len(mismatch)>2 or any(src[j]!=0 or prefix[j]!=1 for j in mismatch):return False
   diffs.update(mismatch)
  if len(diffs)>2:return False
  cursor=0
  for j in sorted(diffs)+[len(prefix)]:
   if cursor<j:
    refs=[(NAMES.index(f),obj.byte_start+cursor) for f,obj in selected]
    commit(dst.byte_start+cursor,prefix[cursor:j],refs)
   cursor=j+1
  for j in sorted(diffs):
   where=dst.byte_start+j
   if work[where]!=0 or raw[j]!=1:raise ValueError('mesh boolean mismatch')
   work[where]=1;flags.append(where)
  return True
 namedCount=0
 for rec in recs:
  if rec['target_id'] in completed:continue
  dst=tobj[rec['target_id']];mesh=dst.read();raw=dst.get_raw_data();v=bytes(mesh.m_VertexData.m_DataSize);idx=bytes(mesh.m_IndexBuffer)
  iv=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))))
  spans=((0,iv[0][0]),(iv[0][1],iv[1][0]),(iv[1][1],len(raw)));pieces=[raw[x:y] for x,y in spans];prefix=pieces[0];chosen=[]
  for names in nameIndexes:
   found=None
   for file,obj in names.get(mesh.m_Name,[]):
    sr=obj.get_raw_data()
    if len(sr)<len(prefix):continue
    diff=[j for j in range(len(prefix)) if sr[j]!=prefix[j]]
    if len(diff)>2 or any(sr[j]!=0 or prefix[j]!=1 for j in diff):continue
    if any(any(piece) and sr.count(piece)!=1 for piece in pieces[1:]):continue
    found=(file,obj);break
   if found is None:break
   chosen.append(found)
  if len(chosen)!=2:continue
  if not commitPrefix(dst,raw,prefix,chosen):continue
  for (lo,hi),piece in zip(spans[1:],pieces[1:]):
   if any(piece):commit(dst.byte_start+lo,piece,[(NAMES.index(file),obj.byte_start+obj.get_raw_data().index(piece)) for file,obj in chosen])
  if work[dst.byte_start:dst.byte_start+len(raw)]!=raw:raise ValueError('named mesh metadata differs')
  completeMeshes+=1;namedCount+=1;completed.add(rec['target_id'])
 sizes=[];streamedCount=0
 for rec in recs:
  if rec['target_id'] in completed:continue
  dst=tobj[rec['target_id']];mesh=dst.read();raw=dst.get_raw_data();v=bytes(mesh.m_VertexData.m_DataSize);idx=bytes(mesh.m_IndexBuffer);ipos=raw.index(idx);vpos=raw.index(v)
  if ipos>=vpos:continue
  prefix=raw[:ipos];mid=raw[ipos+len(idx):vpos];tail=raw[vpos+len(v):]
  if len(mid)<4 or len(tail)<16 or tail[-16:]!=bytes(16) or int.from_bytes(mid[-4:],'little')!=len(v):continue
  chosen=[(rec['source_file'],s[rec['source_file']][rec['source_id']]) for s in sourceObjects];positions=[];valid=True
  for file,obj in chosen:
   source=obj.read();sr=obj.get_raw_data();ib=bytes(source.m_IndexBuffer or []);ip=sr.find(ib)
   if not source.m_StreamData or not source.m_StreamData.path or source.m_VertexData.m_DataSize or ip<0 or source.m_Name!=mesh.m_Name or len(sr)<len(prefix):valid=False;break
   diff=[j for j in range(len(prefix)) if sr[j]!=prefix[j]]
   if len(diff)>2 or any(sr[j]!=0 or prefix[j]!=1 for j in diff):valid=False;break
   midStart=ip+len(ib);tailStart=midStart+len(mid)
   if sr[midStart:midStart+len(mid)-4]!=mid[:-4] or sr[midStart+len(mid)-4:tailStart]!=bytes(4) or sr[tailStart:tailStart+len(tail)-16]!=tail[:-16]:valid=False;break
   positions.append((midStart,tailStart))
  if not valid:continue
  if not commitPrefix(dst,raw,prefix,chosen):continue
  commit(dst.byte_start+ipos+len(idx),mid[:-4],[(NAMES.index(file),obj.byte_start+positions[i][0]) for i,(file,obj) in enumerate(chosen)])
  where=dst.byte_start+vpos-4
  if any(work[where:where+4]):raise ValueError('mesh vertex length not blank')
  struct.pack_into('<I',work,where,len(v));sizes.append((where,len(v)))
  commit(dst.byte_start+vpos+len(v),tail[:-16],[(NAMES.index(file),obj.byte_start+positions[i][1]) for i,(file,obj) in enumerate(chosen)])
  if work[dst.byte_start:dst.byte_start+len(raw)]!=raw:raise ValueError('streamed mesh metadata differs')
  completeMeshes+=1;streamedCount+=1;completed.add(rec['target_id'])
 generatedNames=[]
 for rec in recs:
  if rec['target_id'] in completed:continue
  dst=tobj[rec['target_id']];mesh=dst.read();raw=dst.get_raw_data();name=mesh.m_Name
  if not name.startswith('pb_Mesh'):raise ValueError('unexpected generated mesh name')
  try:identifier=int(name[len('pb_Mesh'):])
  except ValueError:raise ValueError('unsupported generated mesh name')
  if name!='pb_Mesh'+str(identifier) or not -(2**31)<=identifier<2**31:raise ValueError('invalid generated mesh identifier')
  v=bytes(mesh.m_VertexData.m_DataSize);idx=bytes(mesh.m_IndexBuffer);intervals=sorted(((raw.index(v),raw.index(v)+len(v)),(raw.index(idx),raw.index(idx)+len(idx))));spans=((0,intervals[0][0]),(intervals[0][1],intervals[1][0]),(intervals[1][1],len(raw)));pieces=[raw[x:y] for x,y in spans];prefix=pieces[0];nameBytes=name.encode('utf-8');pad=(len(nameBytes)+3)&~3;nameEnd=4+pad
  if prefix[:4]!=struct.pack('<I',len(nameBytes)) or prefix[4:4+len(nameBytes)]!=nameBytes or any(prefix[4+len(nameBytes):nameEnd]):raise ValueError('generated name layout differs')
  chosen=None
  for file,linuxObj in nameIndexes[0].get(name,[]):
   winObj=sourceObjects[1][file].get(linuxObj.path_id)
   if winObj is None or winObj.type.name!='Mesh':continue
   pair=((file,linuxObj),(file,winObj));deltas=[];flagPos=set();valid=True
   for _,obj in pair:
    sr=obj.get_raw_data();srcLen=int.from_bytes(sr[:4],'little');srcPad=(srcLen+3)&~3;delta=srcPad-pad
    if len(sr)<len(prefix)+delta:valid=False;break
    for j in range(nameEnd,len(prefix)):
     x=sr[j+delta];y=prefix[j]
     if x!=y:
      if x!=0 or y!=1:valid=False;break
      flagPos.add(j)
    if not valid or any(any(piece) and sr.count(piece)!=1 for piece in pieces[1:]):valid=False;break
    deltas.append(delta)
   if valid and len(flagPos)<=2:chosen=(pair,deltas,sorted(flagPos));break
  if chosen is None:raise ValueError('cannot derive generated mesh metadata: '+name)
  pair,deltas,diffs=chosen;cursor=nameEnd
  for j in diffs+[len(prefix)]:
   if cursor<j:commit(dst.byte_start+cursor,prefix[cursor:j],[(NAMES.index(file),obj.byte_start+cursor+deltas[i]) for i,(file,obj) in enumerate(pair)])
   cursor=j+1
  for j in diffs:
   where=dst.byte_start+j
   if work[where]!=0 or raw[j]!=1:raise ValueError('mesh boolean mismatch')
   work[where]=1;flags.append(where)
  for (lo,hi),piece in zip(spans[1:],pieces[1:]):
   if any(piece):commit(dst.byte_start+lo,piece,[(NAMES.index(file),obj.byte_start+obj.get_raw_data().index(piece)) for file,obj in pair])
  where=dst.byte_start
  if any(work[where:where+nameEnd]):raise ValueError('mesh generated name not blank')
  struct.pack_into('<I',work,where,len(nameBytes));work[where+4:where+4+len(nameBytes)]=nameBytes;generatedNames.append((where,identifier))
  if work[where:where+len(raw)]!=raw:raise ValueError('generated mesh differs')
  completeMeshes+=1;completed.add(rec['target_id'])
 if completeMeshes!=251 or len(generatedNames)!=42:raise ValueError('incomplete generated mesh inventory')
 audio=[o for o in tobj.values() if o.type.name=='AudioClip']
 if len(audio)!=124:raise ValueError('invalid audio inventory')
 indexes=[]
 for objs in sourceObjects:
  clips={}
  for o in objs['sharedassets2.assets'].values():
   if o.type.name=='AudioClip':
    name=o.read().m_Name
    if name in clips:raise ValueError('ambiguous audio name')
    clips[name]=o
  indexes.append(clips)
 audioRows=[]
 for dst in audio:
  final=dst.get_raw_data();clip=dst.read();before=bobj[dst.path_id]
  if dst.byte_start!=before.byte_start or len(final)!=before.byte_size or any(before.get_raw_data()) or len(final)<80:raise ValueError('audio template mismatch')
  vals=[];refs=[]
  for files,index in zip(sourceData,indexes):
   src=index[clip.m_Name];value=src.get_raw_data();start=src.byte_start
   if len(value)!=len(final) or files[0][start:start+len(value)]!=value:raise ValueError('audio source mismatch')
   vals.append(value);refs.append((0,start))
  if vals[0]!=vals[1] or vals[0][-50]!=1 or final[-50]!=0:raise ValueError('audio platform metadata differs')
  oldOffset,oldSize=struct.unpack_from('<QQ',vals[0],len(final)-20);newOffset,newSize=struct.unpack_from('<QQ',final,len(final)-20)
  if newSize<=0 or newOffset+newSize>5365152:raise ValueError('invalid audio target reference')
  patched=bytearray(vals[0]);patched[-50]=0;struct.pack_into('<QQ',patched,len(patched)-20,newOffset,newSize)
  if patched!=final:raise ValueError('audio metadata differs beyond derived fields')
  start=dst.byte_start;work[start:start+len(final)]=patched;ranges.append((start,len(final),refs,sha(vals[0])))
  audioRows.append((start,len(final),oldOffset,oldSize,newOffset,newSize))
 ranges.sort(key=lambda x:x[0])
 if any(a[0]+a[1]>b[0] for a,b in zip(ranges,ranges[1:])):raise ValueError('overlapping object ranges')
 plan=bytearray(b'OBSM2007'+struct.pack('>I',len(ranges)))
 for pins in PINS:
  for h in pins:plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>I',len(base)));plan.extend(sha(base));plan.extend(sha(work));plan.extend(sha(target))
 for dst,size,refs,h in ranges:plan.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1]));plan.extend(h)
 plan.extend(struct.pack('>I',len(audioRows)))
 for row in audioRows:plan.extend(struct.pack('>IIQQQQ',*row))
 plan.extend(struct.pack('>I',len(flags)))
 for offset in flags:plan.extend(struct.pack('>I',offset))
 plan.extend(struct.pack('>I',len(sizes)))
 for offset,size in sizes:plan.extend(struct.pack('>II',offset,size))
 plan.extend(struct.pack('>I',len(generatedNames)))
 for offset,identifier in generatedNames:plan.extend(struct.pack('>Ii',offset,identifier))
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('251 complete meshes (named',namedCount,'streamed',streamedCount,'generated IDs',len(generatedNames),'), 102 materials and 124 audio clips,',len(ranges),'ranges,',sum(x[1] for x in ranges),'bytes; remaining differing bytes',sum(a!=b for a,b in zip(work,target)),'plan sha256',sha(plan).hex(),'partial asset sha256',sha(work).hex())
if __name__=='__main__':main()
