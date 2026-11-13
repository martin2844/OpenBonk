#!/usr/bin/env python3
"""One-pass research reconstruction of selected PC game data for Android.

Pinned to one Steam build and local Unity template. Many other original assets
and metadata remain in the template; this is not a distributable installer.
"""
import argparse,collections,hashlib,json,math,os,shutil,struct,subprocess,sys,tempfile
from pathlib import Path
import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C
from UnityPy.helpers.ResourceReader import get_resource_data

PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35';STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea';LEVEL='fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551';LEVEL_STREAM='bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6';TEMPLATE='74691b1e689d7541993adb6b7a6800c153731047f1c6447b64503a6af20e6e3b'
WINDOWS_PC='64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
WINDOWS_LEVEL='9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c'
MANIFESTS={
'direct':('direct-texture-template/manifest.json','3bcf4a4ff496fcdac1520384bd30c49d671e9944b4ec5e7e23cc9e23ae5fe100'),
'rgba':('rgba-template/manifest.json','abbad059169083017f36b6c560faed2ad88c92a17d6b4bc8f76d9816d5327750'),
'etc':('etc-mip-template/manifest.json','64b2c54efaebfc6fafa4b3cdf19e880d0a0067ec32e25b034c22c38ad274c0e0'),
'bc':('bc-etc-template/manifest.json','779a342d550dd4d703b5700cd382d55bfd5e1e0a7db214e580f7ec0b9cab1c3f'),
'bc_rgba':('bc-rgba-template/manifest.json','c0b287bee5928fd5fbc4cc36c430f6c9d2cab43b99e0a9e2dc71906f7638280c'),
'alpha':('alpha8-template/manifest.json','c303ad6169e2fe7a9a459f19a485f4dc3992ab7b4911f8507c8309ec68020852'),
'exact':('full-exact-stripped/exact-renamed.json','37f2efb47b603148210cd66887b0a57f2c5ad12fd3049ea71d19642531b86984'),
'normal':('normal-template/manifest.json','9523e3d7e2a9aefc74d8c805acc27b861796b701b74926a227f0ae4279231720'),
'large':('large-template/manifest.json','084a8c562538e518bb9bb660d8a8632cc9a262d9a1bd0153b65363a80520ebd8'),
'cube':('all-cubemaps-template/cubemaps.json','c88c880339f06b269fad6b5606f1118b000321f2141040e685cadcf0f9d668d3'),
'matched':('matched-template/matched.json','bcf2bfa4012f9344fcd8e0a22753f6e0e5dcc014dbe2dcad2abe881233ecc075'),
'mesh':('full-cubemaps-stripped/mesh-manifest.json','bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3')}
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported file: {p}')
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def outline(raw,count):
 groups=collections.defaultdict(list)
 for i in range(count):groups[struct.unpack_from('<3f',raw,32*i)].append(i)
 smooth=[None]*count
 for group in groups.values():
  result=struct.unpack_from('<3f',raw,32*group[0]+12)
  if len(group)>1:
   sums=[0.,0.,0.]
   for i in group:
    n=struct.unpack_from('<3f',raw,32*i+12)
    for j in range(3):sums[j]=f32(sums[j]+n[j])
   length=f32(math.sqrt(f32(f32(f32(sums[0]*sums[0])+f32(sums[1]*sums[1]))+f32(sums[2]*sums[2]))))
   result=tuple(f32(x/length) for x in sums) if length>1e-5 else (0.,0.,0.)
  for i in group:smooth[i]=result
 out=bytearray(count*44)
 for i in range(count):out[i*44:i*44+32]=raw[i*32:i*32+32];struct.pack_into('<3f',out,i*44+32,*smooth[i])
 return bytes(out)
def geometry(m):return (m.m_Name,m.m_VertexData.m_VertexCount,tuple((x.stream,x.offset,x.format,x.dimension) for x in m.m_VertexData.m_Channels),m.m_IndexFormat)
def payload(m):
 v=m.m_VertexData.m_DataSize or b''
 if not v and m.m_StreamData and m.m_StreamData.path:
  if m.m_StreamData.path not in ('sharedassets1.assets.resS','level1.resS'):raise ValueError('unexpected mesh stream')
  v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
 return v,bytes(m.m_IndexBuffer or [])
def mip_encode(src,dst,raw,normal=False):
 chunks=[];off=0
 lut=bytes(round((max(0,1-(x/127.5-1)**2-(y/127.5-1)**2)**.5)*127.5+127.5) for x in range(256) for y in range(256)) if normal else None
 for level in range(dst.m_MipCount):
  w=max(1,dst.m_Width>>level);h=max(1,dst.m_Height>>level);fmt=src.m_TextureFormat
  size=w*h if fmt==1 else w*h*4 if fmt==4 else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16)
  im=C.parse_image_data(raw[off:off+size],w,h,fmt,src.object_reader.version,src.object_reader.platform).convert('RGBA')
  if normal:
   x=im.getchannel('A');y=im.getchannel('G');z=Image.frombytes('L',(w,h),bytes(lut[(i<<8)|j] for i,j in zip(x.tobytes(),y.tobytes())));im=Image.merge('RGBA',(x,y,z,Image.new('L',(w,h),255)))
  data,actual=C.image_to_texture2d(im,dst.m_TextureFormat,dst.object_reader.platform,dst.m_PlatformBlob)
  if int(actual)!=dst.m_TextureFormat:raise ValueError('texture output format mismatch')
  chunks.append(data);off+=size
 if off!=len(raw):raise ValueError('texture mip input mismatch')
 return b''.join(chunks)

p=argparse.ArgumentParser(description=__doc__)
for n in ('pc_data','template','manifest_root','decoder','output_dir'):p.add_argument(n,type=Path)
p.add_argument('--experimental-remaining-textures',action='store_true',help='accept template with 59 experimental converted textures')
p.add_argument('--experimental-all-textures',action='store_true',help='accept all 63 experimental converted textures')
p.add_argument('--windows-source',action='store_true',help='experimentally reconstruct this selected group from the pinned Windows build')
a=p.parse_args()
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and parent must exist')
for name,h in [('sharedassets1.assets',WINDOWS_PC if a.windows_source else PC),('sharedassets1.assets.resS',STREAM),('level1',WINDOWS_LEVEL if a.windows_source else LEVEL),('level1.resS',LEVEL_STREAM)]:require(a.pc_data/name,h)
require(a.template/'sharedassets1.assets','69252ea2b84f122bff60a0085d074e53b5f60400b13f67ef579fc469a91bd20a' if a.experimental_all_textures else '1e75e45736ffb5d433516feaa760aa1f1f0dd949163064e67936bfee01f56aec' if a.experimental_remaining_textures else TEMPLATE)
if not a.decoder.is_file():raise ValueError('missing Unity Crunch decoder')
sets={}
for name,(path,h) in MANIFESTS.items():
 f=a.manifest_root/path;require(f,h);sets[name]=json.loads(f.read_text())['cubemaps' if name=='cube' else 'meshes' if name=='mesh' else 'textures']
ids=[r['target_id'] for k,v in sets.items() if k not in ('cube','mesh') for r in v]
if len(ids)!=784 or len(set(ids))!=784 or len(sets['cube'])!=7 or len(sets['mesh'])!=420:raise ValueError('bad manifest counts')
source_envs={name:UnityPy.load(str(a.pc_data/name)) for name in ('sharedassets1.assets','level1')};sources={k:{o.path_id:o for o in v.objects} for k,v in source_envs.items()};src=sources['sharedassets1.assets'];env=UnityPy.load(str(a.template/'sharedassets1.assets'));dst={o.path_id:o for o in env.objects}
for mode in ('direct','rgba','etc','bc','bc_rgba','alpha','exact','normal','large','matched'):
 for rec in sets[mode]:
  sid,tid=rec['source_id'],rec['target_id']
  if sid not in src or tid not in dst or src[sid].type.name!='Texture2D' or dst[tid].type.name!='Texture2D':raise ValueError('missing texture')
  s=src[sid].read();d=dst[tid].read();expected_name=rec.get('target_name',rec.get('name'));source_name=rec.get('source_name',rec.get('name'))
  if d.m_Name!=expected_name or s.m_Name!=source_name or (s.m_Width,s.m_Height,s.m_ColorSpace,s.m_ImageCount)!=(d.m_Width,d.m_Height,d.m_ColorSpace,d.m_ImageCount) or d.m_ImageCount!=1 or len(d.image_data)!=rec['size'] or any(d.image_data):raise ValueError(f'texture metadata mismatch {tid}')
  if mode in ('rgba','alpha'):
   im=s.image.convert('RGBA');key='source_rgba_sha256' if mode=='rgba' else 'rgba_sha256'
   if digest(im.tobytes())!=rec[key] or d.m_TextureFormat!=47 or d.m_MipCount!=1:raise ValueError('RGBA source mismatch')
   if mode=='alpha' and (s.m_TextureFormat!=1 or digest(s.get_image_data())!=rec['source_sha256']):raise ValueError('alpha source mismatch')
   d.set_image(im,target_format=4,mipmap_count=1)
  else:
   raw=s.get_image_data();key={'direct':'sha256','exact':'sha256','etc':'pc_raw_sha256','bc':'pc_raw_sha256','bc_rgba':'pc_raw_sha256','normal':'source_sha256','large':'source_sha256','matched':'source_sha256'}[mode]
   if digest(raw)!=rec[key] or s.m_MipCount!=d.m_MipCount:raise ValueError(f'texture source mismatch {tid}')
   if mode in ('direct','exact'):
    if (s.m_TextureFormat,s.m_MipCount)!=(d.m_TextureFormat,d.m_MipCount) or len(raw)!=len(d.image_data):raise ValueError('direct source format mismatch')
    d.image_data=raw
   else:
    if mode=='etc' and (s.m_TextureFormat!=4 or d.m_TextureFormat not in (45,47)):raise ValueError('ETC format mismatch')
    if mode=='bc' and (s.m_TextureFormat,d.m_TextureFormat) not in ((10,45),(12,47)):raise ValueError('BC format mismatch')
    if mode=='bc_rgba' and (s.m_TextureFormat not in (10,12) or d.m_TextureFormat!=4):raise ValueError('BC RGBA format mismatch')
    if mode=='normal' and (s.m_TextureFormat,d.m_TextureFormat)!=(12,34):raise ValueError('normal format mismatch')
    if mode=='large' and (s.m_TextureFormat,d.m_TextureFormat) not in ((12,47),(25,4)):raise ValueError('large texture format mismatch')
    if mode=='matched' and (s.m_TextureFormat!=rec['source_format'] or d.m_TextureFormat!=rec['target_format'] or s.m_TextureFormat not in (1,4,10,12) or d.m_TextureFormat not in (45,47) or 'normal' in d.m_Name.lower()):raise ValueError('matched texture format mismatch')
    data=mip_encode(s,d,raw,mode=='normal')
    if len(data)!=rec['size']:raise ValueError(f'texture output size mismatch {tid}')
    d.image_data=data;d.m_CompleteImageSize=len(data)
  d.save()
for rec in sets['mesh']:
 so=sources[rec['source_file']][rec['source_id']];do=dst[rec['target_id']]
 if so.type.name!='Mesh' or do.type.name!='Mesh':raise ValueError('mesh missing')
 s=so.read();d=do.read();v,i=payload(s)
 source_geometry=geometry(s)
 # The two PC builds name this specific scene mesh differently, while its
 # geometry and both pinned buffers are identical. Keep Android's name.
 if a.windows_source and rec['source_file']=='level1' and rec['source_id']==15 and rec['name']=='pb_Mesh-202146' and s.m_Name=='pb_Mesh-74516':
  source_geometry=(rec['name'],*source_geometry[1:])
 if source_geometry!=geometry(d) or d.m_Name!=rec['name'] or len(v)!=rec['vertices_size'] or len(i)!=rec['indices_size'] or digest(v)!=rec['vertices_sha256'] or digest(i)!=rec['indices_sha256'] or any(d.m_VertexData.m_DataSize) or any(d.m_IndexBuffer):raise ValueError(f"mesh mismatch: {rec['source_file']} id {rec['source_id']} name {rec['name']}")
 d.m_VertexData.m_DataSize=v;d.m_IndexBuffer=list(i);d.save()
for tid,sid,name,count in ((1559,1482,'Cube',2600),(1641,1827,'lp.obj',493)):
 s=src[sid].read();d=dst[tid].read();v=s.m_VertexData.m_DataSize;i=bytes(s.m_IndexBuffer)
 if s.m_Name!=name or d.m_Name!=name or s.m_VertexData.m_VertexCount!=count or d.m_VertexData.m_VertexCount!=count or len(v)!=count*32 or len(d.m_VertexData.m_DataSize)!=count*44 or any(d.m_VertexData.m_DataSize) or any(d.m_IndexBuffer) or len(d.m_IndexBuffer)!=len(i):raise ValueError('outline mesh mismatch')
 d.m_VertexData.m_DataSize=outline(v,count);d.m_IndexBuffer=list(i);d.save()
with tempfile.TemporaryDirectory(prefix='.openbonk-cubefaces-',dir=a.output_dir.parent) as tmp:
 tmp=Path(tmp)
 for rec in sets['cube']:
  s=src[rec['source_id']].read();d=dst[rec['target_id']].read();raw=s.get_image_data()
  if s.m_Name!=rec['name'] or d.m_Name!=s.m_Name or s.m_Width!=512 or s.m_Height!=512 or s.m_ImageCount!=6 or d.m_ImageCount!=6 or d.m_TextureFormat!=45 or s.m_MipCount!=1 or d.m_MipCount!=1 or digest(raw)!=rec['source_sha256'] or len(d.image_data)!=rec['size'] or any(d.image_data):raise ValueError('cubemap mismatch')
  if s.m_TextureFormat==29:
   inp=tmp/'in.crn';out=tmp/'out.dxt';inp.write_bytes(raw);out.unlink(missing_ok=True);subprocess.run([str(a.decoder.resolve()),str(inp),str(out)],check=True,stdout=subprocess.DEVNULL);decoded=out.read_bytes()
  elif s.m_TextureFormat==12:decoded=raw
  else:raise ValueError('cubemap format mismatch')
  if len(decoded)!=1572864 or digest(decoded)!=rec['decoded_sha256']:raise ValueError('cubemap source decode mismatch')
  faces=[]
  for i in range(6):
   im=C.parse_image_data(decoded[i*262144:(i+1)*262144],512,512,12,s.object_reader.version,s.object_reader.platform);data,fmt=C.image_to_texture2d(im,45,d.object_reader.platform,d.m_PlatformBlob)
   if int(fmt)!=45 or len(data)!=131072:raise ValueError('cubemap conversion mismatch')
   faces.append(data)
  d.image_data=b''.join(faces);d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save())
 actual_sha=sha(out)
 expected_sha='cc947601ab5e1d65c06afda412260f36d2d22bc66a818f47ac5b94c7c2227e7b' if a.experimental_all_textures else 'b17ab5d19b53a2c5a076e5d18175a16ddeda9b68cf947add6b901b160466cec3' if a.experimental_remaining_textures else '9715a5ee8c6e589ece19b7996e48fa839eb1cf537e29d3a10e403f466fb760cd'
 if actual_sha!=expected_sha:raise ValueError(f'output asset digest mismatch: {actual_sha}')
 helper=Path(__file__).resolve().parent.parent/'analysis/data-conversion/build-pc-audio-resource-prototype.py'
 subprocess.run([sys.executable,str(helper),str(a.pc_data),str(out),str(a.output_dir/'sharedassets1.resource')]+(['--windows-source'] if a.windows_source else []),check=True)
 if sha(a.output_dir/'sharedassets1.resource')!='93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6':raise ValueError('output audio digest mismatch')
 print('Output includes',847 if a.experimental_all_textures else 843 if a.experimental_remaining_textures else 784,'reconstructed textures, 7 cubemaps, 422 mesh buffers and 118 audio streams; SHA256',sha(out))
 print('Experimental assets retain other game content and are not for distribution.')
except Exception:shutil.rmtree(a.output_dir);raise
