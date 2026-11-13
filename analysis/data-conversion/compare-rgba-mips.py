#!/usr/bin/env python3
"""Compare raw RGBA32 Unity texture mip levels in two local asset files.

Reports alpha differences and RGB differences among pixels whose old or new
alpha is nonzero. This measures pixels, not filtered rendering or provenance.
The manifest selects texture path IDs; neither input is a release artifact.
"""
import argparse
import json
from pathlib import Path
import UnityPy
from PIL import Image, ImageChops, ImageStat

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('before', type=Path)
parser.add_argument('after', type=Path)
parser.add_argument('manifest', type=Path)
args = parser.parse_args()
records = json.loads(args.manifest.read_text())['textures']
before = {o.path_id: o for o in UnityPy.load(str(args.before)).objects}
after = {o.path_id: o for o in UnityPy.load(str(args.after)).objects}
if before.keys() != after.keys():
    raise ValueError('object IDs differ')
for rec in records:
    tid = rec['target_id']
    if tid not in before or tid not in after or before[tid].type.name != 'Texture2D' or after[tid].type.name != 'Texture2D':
        raise ValueError(f'missing texture {tid}')
    old, new = before[tid].read(), after[tid].read()
    identity = lambda t: (t.m_Name, t.m_Width, t.m_Height, t.m_TextureFormat, t.m_MipCount, t.m_ImageCount)
    if identity(old) != identity(new) or old.m_Name != rec['name'] or old.m_TextureFormat != 4 or old.m_ImageCount != 1:
        raise ValueError(f'texture metadata differs: {tid}')
    offset = 0
    visible_levels = alpha_levels = 0
    max_mean = 0.0
    for level in range(old.m_MipCount):
        width, height = max(1, old.m_Width >> level), max(1, old.m_Height >> level)
        size = width * height * 4
        o = Image.frombytes('RGBA', (width, height), old.image_data[offset:offset+size])
        n = Image.frombytes('RGBA', (width, height), new.image_data[offset:offset+size])
        diff = ImageChops.difference(o, n)
        mask = ImageChops.lighter(o.getchannel('A'), n.getchannel('A')).point(lambda a: 255 if a else 0)
        masked = ImageChops.multiply(diff.convert('RGB'), Image.merge('RGB', (mask, mask, mask)))
        if masked.getbbox():
            visible_levels += 1
            max_mean = max(max_mean, sum(ImageStat.Stat(masked).mean) / 3)
        if diff.getchannel('A').getbbox():
            alpha_levels += 1
        offset += size
    if offset != len(old.image_data) or offset != len(new.image_data):
        raise ValueError(f'bad mip payload: {tid}')
    print(f"{rec['name']!r}: levels={old.m_MipCount} visible_rgb_difference_levels={visible_levels} alpha_difference_levels={alpha_levels} max_masked_mean_rgb={max_mean:.5f}")
