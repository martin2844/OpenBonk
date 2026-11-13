#!/usr/bin/env python3
"""Compare unique same-name hashed Android texture candidates to PC images.

Scores are alpha-weighted and are investigative only; they do not prove
identity, color-space equivalence, mip quality, provenance or rights.
"""
import argparse,collections,hashlib,json,re,zipfile
from pathlib import Path
from PIL import Image,ImageChops,ImageStat
import UnityPy
p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('obb',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
if a.output.exists() or not a.output.parent.is_dir():p.error('output must not exist and parent must exist')
index=collections.defaultdict(list);envs={};errors=[]
for f in ('sharedassets0.assets','sharedassets1.assets','sharedassets2.assets','sharedassets4.assets','level1','level2','level4'):
 env=UnityPy.load(str(a.pc_data/f));envs[f]=env
 for o in env.objects:
  if o.type.name=='Texture2D':
   try:
    d=o.read();index[(d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)].append((f,o.path_id,d))
   except Exception as x:errors.append((f,o.path_id,type(x).__name__,str(x)[:100]))
results=[]
with zipfile.ZipFile(a.obb) as z:
 for n in z.namelist():
  if not re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}',n):continue
  try:
   raw=z.read(n);env=UnityPy.load(raw);objs=list(env.objects)
   if len(objs)!=1 or objs[0].type.name!='Texture2D':continue
   d=objs[0].read();candidates=index[(d.m_Name,d.m_Width,d.m_Height,d.m_MipCount,d.m_ImageCount,d.m_ColorSpace)]
   if len(candidates)!=1:continue
   f,sid,s=candidates[0];src=s.get_image_data();target=d.image_data
   if not target:raise ValueError('target payload absent')
   x=d.image.convert('RGBA');y=s.image.convert('RGBA');delta=ImageChops.difference(x,y);alpha=ImageChops.lighter(x.getchannel('A'),y.getchannel('A'));weighted=ImageChops.multiply(delta.convert('RGB'),Image.merge('RGB',(alpha,alpha,alpha)))
   results.append({'file':Path(n).name,'name':d.m_Name,'source_file':f,'source_id':sid,'width':d.m_Width,'height':d.m_Height,'mips':d.m_MipCount,'source_format':s.m_TextureFormat,'target_format':d.m_TextureFormat,'target_size':len(target),'source_sha256':hashlib.sha256(src).hexdigest(),'target_sha256':hashlib.sha256(target).hexdigest(),'exact_payload':src==target,'weighted_rgb':list(ImageStat.Stat(weighted).mean),'alpha_difference':ImageStat.Stat(delta.getchannel('A')).mean[0]})
  except Exception as x:errors.append((n,type(x).__name__,str(x)[:100]))
a.output.write_text(json.dumps({'results':results,'errors':errors,'limitation':__doc__},indent=2)+'\n')
print('compared',len(results),'exact',sum(r['exact_payload'] for r in results),'errors',len(errors))
for r in sorted(results,key=lambda x:max(x['weighted_rgb'])+x['alpha_difference'],reverse=True)[:15]:print(r['name'],r['file'],'weighted rgb',*[round(x,2) for x in r['weighted_rgb']],'alpha',round(r['alpha_difference'],2))
for e in errors[:20]:print('ERROR',e)
