#!/usr/bin/env python3
"""Rebuild 94 font atlases and seven font files from one pinned PC build.

Local prototype only. Atlas conversion to ETC2 is lossy; other game content
remains in the template. This is not a redistribution artifact.
"""
import argparse,collections,hashlib,json,shutil
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
WINDOWS_PC='772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d';STREAM='374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b';BASE='e174835b0f61a83ce8df2e02b93cdc8b726f4cb5ca3da5261db161e8cdc94571';FONT_MANIFEST='4fd3e38f6c110ebcaff62e5e74da6df53678651f9c3e5a5530670a71c6502272'
def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(p,h):
 if not p.is_file() or sha(p)!=h:raise ValueError(f'unsupported input {p}')
p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('font_manifest',type=Path);p.add_argument('output_dir',type=Path);p.add_argument('--atlas-manifest',type=Path);p.add_argument('--converted-textures',action='store_true');p.add_argument('--leave-font-metadata-blank',action='store_true');p.add_argument('--windows-source',action='store_true',help='use pinned Windows data for restore');a=p.parse_args()
if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
if a.leave_font_metadata_blank and (a.action!='restore' or not a.converted_textures):p.error('--leave-font-metadata-blank requires restore and --converted-textures')
if a.output_dir.exists() or not a.output_dir.parent.is_dir():p.error('output must not exist and its parent must exist')
require(a.pc_data/'sharedassets0.assets',WINDOWS_PC if a.windows_source else PC);require(a.pc_data/'sharedassets0.assets.resS',STREAM);require(a.font_manifest,FONT_MANIFEST)
fonts=json.loads(a.font_manifest.read_text())['fonts']
if len(fonts)!=7 or len({r['target_id'] for r in fonts})!=7:raise ValueError('invalid font manifest')
if a.action=='template':require(a.input_asset,BASE)
else:
 if not a.atlas_manifest:p.error('restore requires --atlas-manifest')
 manifest=json.loads(a.atlas_manifest.read_text());records=manifest.get('atlases',[])
 if manifest.get('version')!=1 or manifest.get('base_sha256')!=BASE or len(records)!=94 or len({r['target_id'] for r in records})!=94:raise ValueError('invalid atlas manifest')
 require(a.input_asset,'7158ed15e1e8d134e836ff0e6c60cdf5309a4a6cb7a14d6e2e7777fceb4c62b7' if a.leave_font_metadata_blank else '8c875f7f1b90cc9509eb8dd2790caa2b5de33b5cc5c11d5135ae687941bae418' if a.converted_textures else manifest['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets0.assets'));env=UnityPy.load(str(a.input_asset));index=collections.defaultdict(list);sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects}
if a.leave_font_metadata_blank and any(i not in targets or targets[i].type.name!='MonoBehaviour' or any(targets[i].get_raw_data()) for i in range(131,138)):raise ValueError('font metadata must be blank')
for o in pc.objects:
 if o.type.name=='Texture2D':
  t=o.read();index[(t.m_Name,t.m_Width,t.m_Height)].append((o.path_id,t))
def valid(d,s):return (d.m_Name==s.m_Name and 'SDF Atlas' in d.m_Name and (d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)==(1024,1024,1,1,s.m_ColorSpace) and (s.m_Width,s.m_Height,s.m_MipCount,s.m_ImageCount)==(1024,1024,1,1) and s.m_TextureFormat==1 and d.m_TextureFormat==47)
atlases=[]
for o in env.objects:
 if o.type.name!='Texture2D':continue
 d=o.read();matches=index[(d.m_Name,d.m_Width,d.m_Height)]
 if len(matches)!=1 or not valid(d,matches[0][1]):continue
 sid,s=matches[0];raw=s.get_image_data();old=d.image_data
 if len(raw)!=1048576 or len(old)!=1048576 or d.m_CompleteImageSize!=1048576:raise ValueError('unexpected atlas payload')
 rec={'target_id':o.path_id,'source_id':sid,'name':d.m_Name,'size':len(old),'pc_sha256':digest(raw)}
 if a.action=='template':
  if not any(old):raise ValueError('empty atlas already')
  d.image_data=bytes(len(old));atlases.append(rec)
 else:
  if next((r for r in records if r['target_id']==o.path_id),None)!=rec or any(old):raise ValueError('atlas source/template mismatch')
  image=s.image.convert('RGBA');out,fmt=C.image_to_texture2d(image,47,d.object_reader.platform,d.m_PlatformBlob)
  if int(fmt)!=47 or len(out)!=len(old):raise ValueError('atlas encoder mismatch')
  d.image_data=out;atlases.append(rec)
 d.save()
if a.action=='template' and (len(atlases)!=94 or len({r['target_id'] for r in atlases})!=94):raise ValueError(f'unexpected atlas count {len(atlases)}')
if a.action=='restore':
 if len(atlases)!=94 or len({r['target_id'] for r in atlases})!=94:raise ValueError(f'unexpected restored atlas count {len(atlases)}')
 for rec in fonts:
  if rec['target_id'] not in targets or rec['source_id'] not in sources or targets[rec['target_id']].type.name!='Font' or sources[rec['source_id']].type.name!='Font':raise ValueError('font missing')
  d=targets[rec['target_id']].read();s=sources[rec['source_id']].read();raw=bytes(s.m_FontData)
  if d.m_Name!=rec['name'] or s.m_Name!=rec['name'] or len(raw)!=rec['size'] or digest(raw)!=rec['sha256'] or len(d.m_FontData)!=rec['size'] or any(d.m_FontData):raise ValueError('font source/template mismatch')
  d.m_FontData=raw;d.save()
a.output_dir.mkdir()
try:
 out=a.output_dir/'sharedassets0.assets';out.write_bytes(env.file.save());h=sha(out)
 if a.leave_font_metadata_blank and h!='e92b4561faf7be547bef42a874067ba3d688947db0bfa0fbd966532792e2e7b1':raise ValueError('unexpected blank-font atlas output')
 if a.action=='template':(a.output_dir/'atlas-manifest.json').write_text(json.dumps({'version':1,'base_sha256':BASE,'template_sha256':h,'atlases':atlases},indent=2)+'\n')
 print(a.action,94,'atlases',('and 7 fonts' if a.action=='restore' else ''),'asset SHA256',h,flush=True);print('Other game data remains: not distributable.')
except Exception:shutil.rmtree(a.output_dir);raise
