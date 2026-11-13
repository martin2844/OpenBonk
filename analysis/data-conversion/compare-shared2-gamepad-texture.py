#!/usr/bin/env python3
"""Compare decoded Android and experimentally rebuilt controller texture mips."""
import argparse
import hashlib
import json
from pathlib import Path

import UnityPy
from PIL import ImageChops, ImageStat
from UnityPy.export import Texture2DConverter as converter

PINS = ('dc0fa2e17c7607009ad45489f5995aac47cc81a021ae0ce46a0ecd4981bf3206',
        '9a6f385d633c6d2c2699a4edcc53b54699233951a6b2cc5588f90428bf876307')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('original',type=Path);p.add_argument('rebuilt',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    data=[x.read_bytes() for x in (a.original,a.rebuilt)]
    if tuple(hashlib.sha256(x).hexdigest() for x in data)!=PINS:raise ValueError('input hash mismatch')
    objects=[{o.path_id:o for o in UnityPy.load(x).objects} for x in data]
    t=[o[171].read() for o in objects];fields=('m_Name','m_Width','m_Height','m_MipCount','m_TextureFormat')
    if any(getattr(t[0],f)!=getattr(t[1],f) for f in fields) or t[0].m_TextureFormat!=47:raise ValueError('texture metadata mismatch')
    results=[];offset=0
    for level in range(t[0].m_MipCount):
        w,h=max(1,t[0].m_Width>>level),max(1,t[0].m_Height>>level);size=((w+3)//4)*((h+3)//4)*16
        images=[converter.parse_image_data(bytes(x.image_data)[offset:offset+size],w,h,47,x.object_reader.version,x.object_reader.platform).convert('RGBA') for x in t]
        diff=ImageChops.difference(*images)
        results.append({'level':level,'width':w,'height':h,'mean_absolute_error_rgba':ImageStat.Stat(diff).mean,'maximum_channel_error_rgba':[hi for _,hi in diff.getextrema()]})
        offset+=size
    if any(offset!=len(x.image_data) for x in t):raise ValueError('mip payload mismatch')
    report={'inputs':list(PINS),'measure':'decoded RGBA difference from Android, 0-255 per channel','mips':results,'limitations':'Does not establish visual equivalence in menus or rights.'}
    a.output.write_text(json.dumps(report,indent=2)+'\n');print('compared',len(results),'RGBA mip levels')


if __name__=='__main__':main()
