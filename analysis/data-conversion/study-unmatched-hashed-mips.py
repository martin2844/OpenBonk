#!/usr/bin/env python3
"""Compare each detail level for the closest PC texture candidates.

These are visual measurements, not proof of provenance or interchangeability.
"""
import hashlib,json,sys,zipfile
from pathlib import Path
from PIL import ImageChops,ImageStat
import UnityPy
from UnityPy.export import Texture2DConverter as C
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);report=Path(sys.argv[3]);out=Path(sys.argv[4])
if out.exists():raise ValueError('output exists')
data=json.loads(report.read_text())['results'];envs={n:UnityPy.load(str(pc/n)) for n in ('resources.assets','sharedassets1.assets')};sources={n:{o.path_id:o for o in e.objects} for n,e in envs.items()}
def size(fmt,w,h):return {1:w*h,4:w*h*4,10:((w+3)//4)*((h+3)//4)*8,12:((w+3)//4)*((h+3)//4)*16,45:((w+3)//4)*((h+3)//4)*8,47:((w+3)//4)*((h+3)//4)*16}[int(fmt)]
results=[]
with zipfile.ZipFile(obb) as z:
 for rec in data:
  best=rec['candidates'][0];d=UnityPy.load(z.read('assets/bin/Data/'+rec['file'])).objects[0].read();s=sources[best['file']][best['path_id']].read();a=s.get_image_data();b=d.get_image_data();ao=bo=0;mips=[]
  if s.m_MipCount!=d.m_MipCount:raise ValueError('mip count differs')
  for level in range(d.m_MipCount):
   w=max(1,d.m_Width>>level);h=max(1,d.m_Height>>level);an=size(s.m_TextureFormat,w,h);bn=size(d.m_TextureFormat,w,h)
   x=C.parse_image_data(a[ao:ao+an],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA');y=C.parse_image_data(b[bo:bo+bn],w,h,d.m_TextureFormat,d.object_reader.version,d.object_reader.platform).convert('RGBA')
   diff=ImageStat.Stat(ImageChops.difference(x,y)).mean;mips.append({'size':[w,h],'mean_rgba':[round(q,3) for q in diff]});ao+=an;bo+=bn
  if ao!=len(a) or bo!=len(b):raise ValueError('payload size mismatch')
  results.append({'file':rec['file'],'name':d.m_Name,'source_file':best['file'],'source_id':best['path_id'],'source_format':int(s.m_TextureFormat),'target_format':int(d.m_TextureFormat),'source_payload_sha256':hashlib.sha256(a).hexdigest(),'target_payload_sha256':hashlib.sha256(b).hexdigest(),'mips':mips})
  print(d.m_Name,rec['file'],'format',s.m_TextureFormat,'->',d.m_TextureFormat,'peak',round(max(max(m['mean_rgba']) for m in mips),3),'mip0',mips[0]['mean_rgba'],flush=True)
out.write_text(json.dumps(results,indent=2)+'\n')
