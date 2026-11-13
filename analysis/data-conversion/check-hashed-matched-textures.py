#!/usr/bin/env python3
"""Compare original and converted pixels for each mip of local texture probes."""
import json,zipfile
from pathlib import Path
from PIL import Image,ImageChops,ImageStat
import UnityPy
from UnityPy.export import Texture2DConverter as C
p=Path('build/android/share-source-experiment');m=json.loads((p/'hashed-matched53-template/manifest.json').read_text());results=[]
with zipfile.ZipFile(p/'shared-hashed-all-scrubbed-base/base.obb') as z:
 for rec in m['textures']:
  name=rec['file'];old=UnityPy.load(z.read('assets/bin/Data/'+name)).objects[0].read();new=UnityPy.load(str(p/'hashed-matched53-restored'/name)).objects[0].read();scores=[];off=0
  for lv in range(old.m_MipCount):
   w=max(1,old.m_Width>>lv);h=max(1,old.m_Height>>lv);fmt=old.m_TextureFormat;n=w*h*4 if fmt==4 else ((w+3)//4)*((h+3)//4)*(8 if fmt==45 else 16)
   x=C.parse_image_data(old.image_data[off:off+n],w,h,fmt,old.object_reader.version,old.object_reader.platform).convert('RGBA');y=C.parse_image_data(new.image_data[off:off+n],w,h,fmt,new.object_reader.version,new.object_reader.platform).convert('RGBA');diff=ImageChops.difference(x,y);alpha=ImageChops.lighter(x.getchannel('A'),y.getchannel('A'));wtd=ImageChops.multiply(diff.convert('RGB'),Image.merge('RGB',(alpha,alpha,alpha)));scores.append({'size':[w,h],'weighted_rgb':[round(v,3) for v in ImageStat.Stat(wtd).mean],'alpha':round(ImageStat.Stat(diff.getchannel('A')).mean[0],3)});off+=n
  if off!=len(old.image_data):raise ValueError('mip size mismatch')
  results.append({'file':name,'name':rec['name'],'mips':scores})
(p/'hashed-matched53-quality.json').write_text(json.dumps(results,indent=2)+'\n')
for r in sorted(results,key=lambda q:max(q['mips'][0]['weighted_rgb'])+q['mips'][0]['alpha'],reverse=True):print(r['name'],r['file'],'mip0',r['mips'][0],'worst_mip',max(max(x['weighted_rgb'])+x['alpha'] for x in r['mips']))
print('Alpha-weighted scores compare prior Android pixels, not visual identity or in-game parity.')
