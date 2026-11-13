#!/usr/bin/env python3
"""Compare uncompressed texture candidates to previous Android pixels per mip.

The source is PC data; metrics compare Android decoded RGB. They are not proof
of visual parity in game, transparency semantics or performance.
"""
import json,zipfile
from pathlib import Path
from PIL import ImageChops,ImageStat
import UnityPy
from UnityPy.export import Texture2DConverter as C
p=Path('build/android/share-source-experiment');pc=Path('/home/your-user/.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data');comp=json.loads((p/'hashed-texture-comparison.json').read_text())['results'];quality={x['file']:x for x in json.loads((p/'hashed-matched64-quality.json').read_text())};candidates=[r for r in comp if not r['exact_payload'] and r['file'] in quality and max(max(m['weighted_rgb'])+m['alpha'] for m in quality[r['file']]['mips'])>15];envs={x:UnityPy.load(str(pc/x)) for x in ('sharedassets1.assets','sharedassets2.assets')};sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()};results=[]
with zipfile.ZipFile(p/'shared-hashed-all-scrubbed-base/base.obb') as z:
 for rec in candidates:
  old=UnityPy.load(z.read('assets/bin/Data/'+rec['file'])).objects[0].read();s=sources[rec['source_file']][rec['source_id']].read();raw=s.get_image_data();off=toff=0;mips=[]
  for lv in range(old.m_MipCount):
   w=max(1,old.m_Width>>lv);h=max(1,old.m_Height>>lv);fmt=s.m_TextureFormat;sz=w*h*(3 if fmt==3 else 4) if fmt in (3,4) else ((w+3)//4)*((h+3)//4)*(8 if fmt==10 else 16);tn=w*h*4 if old.m_TextureFormat==4 else ((w+3)//4)*((h+3)//4)*(8 if old.m_TextureFormat==45 else 16)
   x=C.parse_image_data(old.image_data[toff:toff+tn],w,h,old.m_TextureFormat,old.object_reader.version,old.object_reader.platform).convert('RGBA');y=C.parse_image_data(raw[off:off+sz],w,h,fmt,s.object_reader.version,s.object_reader.platform).convert('RGBA');delta=ImageChops.difference(x.convert('RGB'),y.convert('RGB'));mips.append({'size':[w,h],'rgb':[round(v,3) for v in ImageStat.Stat(delta).mean],'source_alpha_minmax':y.getchannel('A').getextrema()});off+=sz;toff+=tn
  assert off==len(raw) and toff==len(old.image_data)
  results.append({'file':rec['file'],'name':rec['name'],'source_format':s.m_TextureFormat,'target_format':old.m_TextureFormat,'mips':mips})
(p/'hashed-excluded-rgba-study.json').write_text(json.dumps(results,indent=2)+'\n')
for r in results:
 print(r['name'],'source',r['source_format'],'target',r['target_format'],'mip0',r['mips'][0],'worst',max(max(q['rgb']) for q in r['mips']))
