#!/usr/bin/env python3
"""Compare visible and transparent sprite mip pixels; does not test filtering."""
import json,sys,zipfile
from pathlib import Path
import UnityPy
from PIL import ImageChops,ImageStat
from UnityPy.export import Texture2DConverter as C
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3]);env=UnityPy.load(str(pc/'sharedassets1.assets'));sources={o.path_id:o for o in env.objects};s=sources[1004].read();duplicate=sources[1309].read()
with zipfile.ZipFile(obb) as z:d=UnityPy.load(z.read('assets/bin/Data/d7f61bd2281833a4e91517c4c2f54dae')).objects[0].read()
a=s.get_image_data();b=d.get_image_data();ao=bo=0;result=[]
for lv in range(s.m_MipCount):
 w=max(1,s.m_Width>>lv);h=max(1,s.m_Height>>lv);n=((w+3)//4)*((h+3)//4)*16
 x=C.parse_image_data(a[ao:ao+n],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA');y=C.parse_image_data(b[bo:bo+n],w,h,d.m_TextureFormat,d.object_reader.version,d.object_reader.platform).convert('RGBA');delta=ImageChops.difference(x,y);alpha=x.getchannel('A')
 result.append({'size':[w,h],'mean_rgba':ImageStat.Stat(delta).mean,'weighted_rgb':[ImageStat.Stat(ImageChops.multiply(delta.getchannel(c),alpha)).mean[0] for c in 'RGB'],'alpha_mean':ImageStat.Stat(alpha).mean[0]});ao+=n;bo+=n
if ao!=len(a) or bo!=len(b):raise ValueError('mip sizes')
out.write_text(json.dumps({'source_payloads_identical':a==duplicate.get_image_data(),'mips':result},indent=2)+'\n')
for r in result:print(r['size'],'weighted',*[round(x,3) for x in r['weighted_rgb']],'alpha difference',round(r['mean_rgba'][3],3))
