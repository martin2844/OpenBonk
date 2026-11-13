#!/usr/bin/env python3
"""Rank same-sized PC textures by decoded pixel similarity to Android textures.

This is an investigative aid, not a content identity or rights determination.
The image score ignores fully transparent RGB but not filtering or mipmaps.
Only candidates in the specified PC asset file are examined.
"""
import argparse, collections, json
from pathlib import Path
from PIL import Image, ImageChops, ImageStat
import UnityPy

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('pc_assets',type=Path);p.add_argument('target_assets',type=Path);p.add_argument('manifests',nargs='+',type=Path);a=p.parse_args()
excluded=set()
for f in a.manifests:
 for r in json.loads(f.read_text())['textures']:
  if r['target_id'] in excluded:raise ValueError('overlapping manifests')
  excluded.add(r['target_id'])
source_env=UnityPy.load(str(a.pc_assets));target_env=UnityPy.load(str(a.target_assets))
targets=[];keys=set()
for o in target_env.objects:
 if o.type.name!='Texture2D' or o.path_id in excluded:continue
 t=o.read()
 if not t.m_Width or not t.m_Height or t.m_ImageCount!=1 or not t.image_data:continue
 targets.append((o.path_id,t));keys.add((t.m_Width,t.m_Height))
sources=collections.defaultdict(list);errors=[]
for o in source_env.objects:
 if o.type.name!='Texture2D':continue
 t=o.read();key=(t.m_Width,t.m_Height)
 if key not in keys or t.m_ImageCount!=1:continue
 try:
  image=t.image.convert('RGBA');small=image.resize((16,16))
  sources[key].append((o.path_id,t,small,image))
 except Exception as e:errors.append({'side':'pc','id':o.path_id,'name':t.m_Name,'error':str(e)})
def score(x,y):
 d=ImageChops.difference(x,y);ma=ImageChops.lighter(x.getchannel('A'),y.getchannel('A'))
 m=ImageChops.multiply(d.convert('RGB'),Image.merge('RGB',(ma,ma,ma)))
 return (sum(ImageStat.Stat(m).mean)+ImageStat.Stat(d.getchannel('A')).mean[0])/4
results=[]
for tid,t in targets:
 try: image=t.image.convert('RGBA');small=image.resize((16,16))
 except Exception as e:errors.append({'side':'android','id':tid,'name':t.m_Name,'error':str(e)});continue
 candidates=[]
 for sid,s,ss,full in sources[(t.m_Width,t.m_Height)]:
  if s.m_ColorSpace!=t.m_ColorSpace:continue
  candidates.append((score(small,ss),sid,s,full))
 candidates.sort(key=lambda x:(x[0],x[1]));rank=[]
 for quick,sid,s,full in candidates[:8]:
  rank.append({'source_id':sid,'name':s.m_Name,'format':s.m_TextureFormat,'mips':s.m_MipCount,'preview_score':round(quick,3),'full_score':round(score(image,full),3)})
 rank.sort(key=lambda x:(x['full_score'],x['source_id']))
 results.append({'target_id':tid,'name':t.m_Name,'size':[t.m_Width,t.m_Height],'format':t.m_TextureFormat,'mips':t.m_MipCount,'candidates_examined':len(candidates),'best_candidates':rank[:4]})
print(json.dumps({'method':'Rough top-8 preview then full-resolution alpha-weighted RGBA difference; not a proof of identity.','excluded':len(excluded),'targets':results,'errors':errors},indent=2))
