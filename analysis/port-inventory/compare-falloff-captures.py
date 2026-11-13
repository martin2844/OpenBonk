#!/usr/bin/env python3
"""Measure two falloff captures and a disabled-projector control.

This reports pixels from one controlled scene, not perceptual equivalence or
behavior on other hardware, scenes, camera angles or all mip levels.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops


def compare(first, second):
    diff=ImageChops.difference(first,second)
    pixels=list(diff.getdata());channels=len(pixels)*3
    return {'changed_pixels':sum(any(p) for p in pixels), 'mean_absolute_rgb':sum(sum(p) for p in pixels)/channels,
            'max_channel_difference':max(max(p) for p in pixels), 'pixels_with_channel_difference_at_least_3':sum(max(p)>=3 for p in pixels),
            'bounds':diff.getbbox()}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('directory',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    rows=[]
    for factor in (1,2,4):
        images={};hashes={}
        for variant in ('old','converted','disabled'):
            path=a.directory/f'v2-{factor}x-{variant}.png';data=path.read_bytes();hashes[variant]=hashlib.sha256(data).hexdigest()
            im=Image.open(path)
            if im.size!=(640,320):raise ValueError('unexpected capture dimensions')
            images[variant]=im.convert('RGB')
        row={'factor':factor,'image_sha256':hashes,'regions':{}}
        for label,box in (('full',(0,0,640,320)),('center',(240,80,400,240))):
            old,new,disabled=(images[k].crop(box) for k in ('old','converted','disabled'))
            row['regions'][label]={'bounds':box,'old_vs_converted':compare(old,new),'old_vs_disabled':compare(old,disabled),'converted_vs_disabled':compare(new,disabled)}
        rows.append(row)
    report={'images':rows,'limitations':'Controlled overhead view with post-processing disabled; screenshots and differences do not establish perceptual equivalence or hardware behavior.'}
    with a.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    for x in rows:
        c=x['regions']['center'];print(x['factor'],'center old/new max',c['old_vs_converted']['max_channel_difference'],'control mean',round(c['old_vs_disabled']['mean_absolute_rgb'],3))


if __name__=='__main__':main()
