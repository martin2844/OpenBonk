#!/usr/bin/env python3
"""Approximate GPU straight-alpha bilinear filtering on selected texture mips.

This is a CPU image comparison, not a Unity shader, color-space or device test.
"""
import json,sys,zipfile
from pathlib import Path
import UnityPy
from PIL import Image,ImageChops,ImageStat
from UnityPy.export import Texture2DConverter as C
pc=Path(sys.argv[1]);obb=Path(sys.argv[2]);out=Path(sys.argv[3]);env=UnityPy.load(str(pc/'sharedassets1.assets'));objs={o.path_id:o for o in env.objects};s=objs[1004].read()
with zipfile.ZipFile(obb) as z:d=UnityPy.load(z.read('assets/bin/Data/d7f61bd2281833a4e91517c4c2f54dae')).objects[0].read()
a=s.get_image_data();b=d.get_image_data();off=0;result=[]
def filtered(im,w,h):
 # Resize channels separately to model straight (non-premultiplied) sampling.
 return Image.merge('RGBA',[ch.resize((w,h),Image.Resampling.BILINEAR) for ch in im.split()])
def composited(im,bg):
 rgb=im.convert('RGB');return Image.composite(rgb,Image.new('RGB',im.size,bg),im.getchannel('A'))
for lv in range(s.m_MipCount):
 w=max(1,s.m_Width>>lv);h=max(1,s.m_Height>>lv);n=((w+3)//4)*((h+3)//4)*16
 x=C.parse_image_data(a[off:off+n],w,h,s.m_TextureFormat,s.object_reader.version,s.object_reader.platform).convert('RGBA');y=C.parse_image_data(b[off:off+n],w,h,d.m_TextureFormat,d.object_reader.version,d.object_reader.platform).convert('RGBA');off+=n
 x=filtered(x,w*2,h*2);y=filtered(y,w*2,h*2);row={'size':[w,h],'backgrounds':{}}
 for bg in ((0,0,0),(128,128,128),(255,255,255)):
  delta=ImageChops.difference(composited(x,bg),composited(y,bg));red,green,blue=delta.split();peak=ImageChops.lighter(ImageChops.lighter(red,green),blue);hist=peak.histogram();count=sum(hist);acc=0;p99=0
  for value,frequency in enumerate(hist):
   acc+=frequency
   if acc>=count*0.99:p99=value;break
  row['backgrounds'][str(bg)]={'mean_rgb':[round(q,3) for q in ImageStat.Stat(delta).mean],'p99_max_channel':p99,'fraction_above_10':round(sum(hist[11:])/count,6)}
 result.append(row)
if off!=len(a) or off!=len(b):raise ValueError('unexpected payload size')
out.write_text(json.dumps({'assumption':__doc__,'mips':result},indent=2)+'\n')
for row in result:print(row['size'],row['backgrounds']['(0, 0, 0)'],flush=True)
