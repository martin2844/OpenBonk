#!/usr/bin/env python3
"""Map exact mesh and particle bytes for the experimental final-map asset."""
import hashlib,json,struct,zipfile
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
BASE=B/'localization-shared-scrubbed-base/base.obb';TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
ZIPS=(B/'linux-steam-regression.zip',B/'windows-steam-full-input.zip')
NAMES=('sharedassets4.assets','sharedassets4.assets.resS','level4')
PINS=(('cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3','862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127','df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'),('896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9','862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127','d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'))
OUTPUT=B/'shared4-map-android-plan/plan-v2.bin'
def sha(b):return hashlib.sha256(b).digest()
def read_asset(z):return b''.join(z.read('assets/bin/Data/sharedassets4.assets.split'+str(i)) for i in range(2))
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 for p,expected in ((BASE,'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'),(TARGET,'e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691')):
  with p.open('rb') as f:
   if hashlib.file_digest(f,'sha256').hexdigest()!=expected:raise ValueError('OBB pin differs')
 with zipfile.ZipFile(BASE) as z:base=read_asset(z)
 with zipfile.ZipFile(TARGET) as z:target=read_asset(z)
 if len(base)!=len(target):raise ValueError('asset lengths differ')
 target_objects={o.path_id:o for o in UnityPy.load(target).objects};base_objects={o.path_id:o for o in UnityPy.load(base).objects}
 data=[];objects=[]
 for path,pins in zip(ZIPS,PINS):
  with zipfile.ZipFile(path) as z:files=[z.read('Megabonk_Data/'+n) for n in NAMES]
  if tuple(sha(x).hex() for x in files)!=pins:raise ValueError('Steam inputs differ')
  data.append(files);objects.append({name:{o.path_id:o for o in UnityPy.load(files[NAMES.index(name)]).objects} for name in ('sharedassets4.assets','level4')})
 work=bytearray(base);ranges=[]
 def add(dst,values,refs):
  if values[0]!=values[1] or dst<0 or dst+len(values[0])>len(work) or any(work[dst:dst+len(values[0])]) or target[dst:dst+len(values[0])]!=values[0]:raise ValueError('invalid copy range')
  if any(dst<r[0]+r[1] and r[0]<dst+len(values[0]) for r in ranges):raise ValueError('overlapping ranges')
  work[dst:dst+len(values[0])]=values[0];ranges.append((dst,len(values[0]),refs,sha(values[0])))
 manifest=B/'localization-shared-scrubbed-base'
 for rec in json.loads((manifest/'shared4-mesh-manifest.json').read_text())['meshes']:
  dst=target_objects[rec['target_id']];baseObj=base_objects[rec['target_id']]
  if dst.type.name!='Mesh' or dst.byte_start!=baseObj.byte_start:raise ValueError('target mesh mismatch')
  dstMesh=dst.read();targetRaw=dst.get_raw_data()
  for field in ('vertices','indices'):
   value=bytes(dstMesh.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(dstMesh.m_IndexBuffer or [])
   if len(value)!=rec[field+'_size'] or sha(value).hex()!=rec[field+'_sha256'] or targetRaw.count(value)!=1:raise ValueError('target mesh payload mismatch')
   dstOffset=dst.byte_start+targetRaw.index(value);values=[];refs=[]
   for files,index in zip(data,objects):
    srcObj=index[rec['source_file']][rec['source_id']];src=srcObj.read();raw=srcObj.get_raw_data()
    payload=bytes(src.m_VertexData.m_DataSize or b'') if field=='vertices' else bytes(src.m_IndexBuffer or [])
    if field=='vertices' and not payload:
     stream=src.m_StreamData
     if not stream or stream.path!=NAMES[1]:raise ValueError('unexpected mesh stream')
     fid=1;offset=stream.offset;payload=files[fid][offset:offset+stream.size]
    else:
     if raw.count(payload)!=1:raise ValueError('ambiguous inline mesh')
     fid=NAMES.index(rec['source_file']);offset=srcObj.byte_start+raw.index(payload)
    if files[fid][offset:offset+len(payload)]!=payload:raise ValueError('source offset mismatch')
    values.append(payload);refs.append((fid,offset))
   add(dstOffset,values,refs)
 particles=json.loads((manifest/'shared4-particle-manifest.json').read_text())['particles']
 refsManifest=json.loads((manifest/'shared4-particle-reference-manifest.json').read_text())['particles']
 for rec in particles+refsManifest:
  dst=target_objects[rec['target_id']];baseObj=base_objects[rec['target_id']]
  if dst.type.name!='ParticleSystem' or dst.byte_start!=baseObj.byte_start or dst.byte_start+12!=rec['offset']:raise ValueError('particle layout mismatch')
  spans=[(0,rec['size'])] if 'pointer_offset' not in rec else [(0,rec['pointer_offset']-12),(rec['pointer_offset'],rec['size']-rec['pointer_offset'])]
  for relative,size in spans:
   if size<=0:raise ValueError('invalid particle span')
   vals=[];offsets=[]
   for files,index in zip(data,objects):
    src=index['sharedassets4.assets'][rec['source_id']];start=src.byte_start+12+relative;value=files[0][start:start+size]
    if len(value)!=size:raise ValueError('source particle short')
    vals.append(value);offsets.append((0,start))
   add(rec['offset']+relative,vals,offsets)
 materials=json.loads((manifest/'material-extra-4/manifest.json').read_text())['objects']
 for rec in materials:
  dst=target_objects[rec['target_id']];baseObj=base_objects[rec['target_id']];length=rec['length'];targetRaw=dst.get_raw_data()
  if dst.type.name!='Material' or dst.byte_start!=baseObj.byte_start or sha(targetRaw[-length:]).hex()!=rec['value_sha256']:raise ValueError('material mismatch')
  vals=[];refs=[]
  for files,index in zip(data,objects):
   src=index['sharedassets4.assets'][rec['steam_id']];raw=src.get_raw_data();offset=src.byte_start+len(raw)-length;value=raw[-length:]
   if sha(value).hex()!=rec['value_sha256'] or files[0][offset:offset+length]!=value:raise ValueError('material source mismatch')
   vals.append(value);refs.append((0,offset))
  add(dst.byte_start+len(targetRaw)-length,vals,refs)
 textureRows=[]
 for rec in json.loads((manifest/'shared4-texture-manifest.json').read_text())['textures']:
  dst=target_objects[rec['target_id']];baseObj=base_objects[rec['target_id']];dstTex=dst.read();raw=dst.get_raw_data();targetPixels=bytes(dstTex.image_data)
  if dst.type.name!='Texture2D' or dst.byte_start!=baseObj.byte_start or raw.count(targetPixels)!=1 or dstTex.m_TextureFormat!=45:raise ValueError('texture target mismatch')
  dstOffset=dst.byte_start+raw.index(targetPixels);refs=[];values=[]
  for files,index in zip(data,objects):
   obj=index['sharedassets4.assets'][rec['source_id']];src=obj.read();stream=src.m_StreamData
   if src.m_TextureFormat!=10 or not stream or stream.path!=NAMES[1]:raise ValueError('texture stream mismatch')
   value=files[1][stream.offset:stream.offset+stream.size]
   if sha(value).hex()!=rec['source_sha256']:raise ValueError('texture source differs')
   refs.append(stream.offset);values.append(value)
  if values[0]!=values[1] or any(work[dstOffset:dstOffset+len(targetPixels)]):raise ValueError('texture source/template mismatch')
  offset=0;converted=[]
  for lv in range(rec['mips']):
   w=max(1,rec['width']>>lv);h=max(1,rec['height']>>lv);n=((w+3)//4)*((h+3)//4)*8
   im=C.parse_image_data(values[0][offset:offset+n],w,h,10,src.object_reader.version,src.object_reader.platform).convert('RGBA');encoded,fmt=C.image_to_texture2d(im,45,dstTex.object_reader.platform,dstTex.m_PlatformBlob)
   if int(fmt)!=45:raise ValueError('texture conversion format mismatch')
   converted.append(encoded);offset+=n
  pixels=b''.join(converted)
  if offset!=len(values[0]) or pixels!=targetPixels:raise ValueError('texture conversion differs')
  work[dstOffset:dstOffset+len(pixels)]=pixels
  textureRows.append((dstOffset,len(pixels),refs[0],refs[1],len(values[0]),rec['width'],rec['height'],rec['mips'],sha(values[0]),sha(pixels)))
 audioRows=[]
 for obj in sorted((o for o in target_objects.values() if o.type.name=='AudioClip'),key=lambda x:x.path_id):
  before=base_objects[obj.path_id];old=before.read().m_Resource;new=obj.read().m_Resource;where=obj.byte_start+obj.byte_size-20
  if old.m_Source!='sharedassets4.resource' or new.m_Source!=old.m_Source or struct.unpack_from('<QQ',work,where)!=(old.m_Offset,old.m_Size):raise ValueError('audio reference mismatch')
  struct.pack_into('<QQ',work,where,new.m_Offset,new.m_Size);audioRows.append((where,old.m_Offset,old.m_Size,new.m_Offset,new.m_Size))
 if len(textureRows)!=3 or len(audioRows)!=6 or bytes(work)!=target:raise ValueError('map reconstruction still differs: '+str(sum(a!=b for a,b in zip(work,target))))
 ranges.sort(key=lambda r:r[0]);plan=bytearray(b'OBSM4002'+struct.pack('>I',len(ranges)))
 for pins in PINS:
  for h in pins:plan.extend(bytes.fromhex(h))
 plan.extend(struct.pack('>I',len(base)));plan.extend(sha(base));plan.extend(sha(work));plan.extend(sha(target))
 for dst,size,refs,h in ranges:
  plan.extend(struct.pack('>IIIQIQ',dst,size,refs[0][0],refs[0][1],refs[1][0],refs[1][1]));plan.extend(h)
 plan.extend(struct.pack('>I',len(textureRows)))
 for dst,size,linux,windows,srcSize,w,h,mips,srcHash,outHash in textureRows:
  plan.extend(struct.pack('>IIQQIIII',dst,size,linux,windows,srcSize,w,h,mips));plan.extend(srcHash);plan.extend(outHash)
 plan.extend(struct.pack('>I',len(audioRows)))
 for row in audioRows:plan.extend(struct.pack('>IQQQQ',*row))
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('mapped',len(ranges),'exact ranges,',len(textureRows),'textures,',len(audioRows),'audio references; complete asset sha',sha(work).hex(),'plan sha',sha(plan).hex())
if __name__=='__main__':main()
