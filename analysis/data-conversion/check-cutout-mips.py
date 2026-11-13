#!/usr/bin/env python3
"""Measure threshold crossings across matching compressed cutout mip levels.

Compares decoded alpha only; does not reproduce texture filtering, materials,
lighting, surface UVs, or hardware-dependent rendering.
"""
import argparse
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter as C
p=argparse.ArgumentParser(description=__doc__);p.add_argument('before',type=Path);p.add_argument('after',type=Path);p.add_argument('id',type=int);p.add_argument('--cutoff',type=float,default=.8);a=p.parse_args()
if not 0<=a.cutoff<=1:p.error('cutoff must be in [0,1]')
def obj(path):
 for o in UnityPy.load(str(path)).objects:
  if o.path_id==a.id and o.type.name=='Texture2D':return o.read()
 raise ValueError('texture missing')
x=obj(a.before);y=obj(a.after)
attrs=('m_Name','m_Width','m_Height','m_TextureFormat','m_MipCount','m_ImageCount')
if any(getattr(x,k)!=getattr(y,k) for k in attrs) or x.m_TextureFormat!=47 or x.m_ImageCount!=1:raise ValueError('unsupported texture metadata')
threshold=a.cutoff*255;off=0
for level in range(x.m_MipCount):
 w=max(1,x.m_Width>>level);h=max(1,x.m_Height>>level);size=((w+3)//4)*((h+3)//4)*16
 def alpha(t):return C.parse_image_data(t.image_data[off:off+size],w,h,t.m_TextureFormat,t.object_reader.version,t.object_reader.platform).convert('RGBA').getchannel('A').tobytes()
 a0=alpha(x);a1=alpha(y);orig=sum(v>=threshold for v in a0);new=sum(v>=threshold for v in a1);cross=sum((u>=threshold)!=(v>=threshold) for u,v in zip(a0,a1))
 print(f'{x.m_Name} level={level} size={w}x{h} cutoff={a.cutoff:g} before_visible={orig} after_visible={new} changed_visibility={cross}/{w*h} ({100*cross/(w*h):.4f}%)');off+=size
if off!=len(x.image_data) or off!=len(y.image_data):raise ValueError('invalid mip payload')
