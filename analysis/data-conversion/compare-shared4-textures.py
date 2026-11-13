#!/usr/bin/env python3
"""Compare every decoded mip in two pinned sharedassets4 texture files."""
import argparse
import hashlib
import json
from pathlib import Path

import UnityPy
from PIL import ImageChops, ImageStat
from UnityPy.export import Texture2DConverter as converter

PINS = ('d9461f6047e92558dfdc552da980f1c1086cea9feacb22d8cee2e7b9debc333e',
        'e12a3f6edd2a570bdf8caf871460a73ff6538a7fa47581d5dcc7a6f1e272a9e7')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('original', type=Path)
    p.add_argument('rebuilt', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    data = [path.read_bytes() for path in (a.original, a.rebuilt)]
    if tuple(hashlib.sha256(x).hexdigest() for x in data) != PINS:
        raise ValueError('input hash mismatch')
    objects = [{o.path_id: o for o in UnityPy.load(x).objects} for x in data]
    results = []
    for target_id in (5, 6, 7):
        textures = [obj[target_id].read() for obj in objects]
        x, y = textures
        fields = ('m_Name', 'm_Width', 'm_Height', 'm_MipCount', 'm_TextureFormat')
        if any(getattr(x, f) != getattr(y, f) for f in fields) or x.m_TextureFormat != 45:
            raise ValueError('texture metadata mismatch')
        offset = 0
        mips = []
        for level in range(x.m_MipCount):
            w, h = max(1, x.m_Width >> level), max(1, x.m_Height >> level)
            size = ((w + 3) // 4) * ((h + 3) // 4) * 8
            images = [converter.parse_image_data(bytes(t.image_data)[offset:offset + size], w, h, 45,
                      t.object_reader.version, t.object_reader.platform).convert('RGB') for t in textures]
            diff = ImageChops.difference(*images)
            mips.append({'level': level, 'width': w, 'height': h,
                         'mean_absolute_rgb_error': sum(ImageStat.Stat(diff).mean) / 3,
                         'maximum_channel_error': max(hi for _, hi in diff.getextrema())})
            offset += size
        if any(offset != len(t.image_data) for t in textures):
            raise ValueError('mip size mismatch')
        results.append({'id': target_id, 'name': x.m_Name, 'mips': mips})
    report = {'inputs': list(PINS), 'measure': 'decoded RGB difference from Android, 0-255 per channel',
              'textures': results, 'limitation': 'Does not establish visual equivalence in game.'}
    a.output.write_text(json.dumps(report, indent=2) + '\n')
    print('compared', len(results), 'textures and', sum(len(r['mips']) for r in results), 'mips')


if __name__ == '__main__':
    main()
