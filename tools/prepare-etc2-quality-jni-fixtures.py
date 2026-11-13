#!/usr/bin/env python3
"""Create private compressed Steam mip fixtures for the experimental JNI check."""
import argparse
import importlib.util
import random
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec); spec.loader.exec_module(study)


def main():
    import UnityPy
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--steam-data', required=True, type=Path); p.add_argument('--output', required=True, type=Path)
    a = p.parse_args(); source = a.steam_data.expanduser().resolve(strict=True); out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    out = out.parent.resolve() / out.name
    if out.is_relative_to(ROOT) or out.is_relative_to(source) or source.is_relative_to(out): p.error('output must be separate from source inputs')
    names = ('sprite_0091', 'sprite_0097'); found = {name: [] for name in names}
    for group in study.GROUPS:
        path = source / (group + '.assets')
        if path.is_symlink() or not path.is_file(): p.error('missing Steam texture source')
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name != 'Texture2D': continue
            texture = obj.read()
            if texture.m_Name in found:
                data = bytes(texture.get_image_data())
                found[texture.m_Name].append((texture.m_Width, texture.m_Height, texture.m_MipCount, int(texture.m_TextureFormat), data))
    rows = []
    for name in names:
        candidates = set(found[name])
        if len(candidates) != 1: raise ValueError('fixture texture is missing or ambiguous: ' + name)
        w, h, count, fmt, data = next(iter(candidates))
        if fmt not in (3, 4, 10, 12, 25): raise ValueError('unsupported fixture format')
        mips = study.mip_slices(data, fmt, w, h, count)
        for level in sorted({0, len(mips) // 2, len(mips) - 1}):
            width, height, payload = mips[level]
            if width > 2048 or height > 2048: raise ValueError('fixture exceeds adapter dimensions')
            rows.append((width, height, fmt, payload))
    rng = random.Random(35)
    for width, height in ((1, 1), (3, 7), (4, 4), (9, 13), (64, 64)):
        for fmt in (3, 4, 10, 12):
            length = study.mip_size(fmt, width, height)
            rows.append((width, height, fmt, bytes(rng.randrange(256) for _ in range(length))))
    created = False
    try:
        with out.open('xb') as stream:
            created = True
            stream.write(b'OBHQ0001' + struct.pack('>I', len(rows)))
            for width, height, fmt, payload in rows:
                stream.write(struct.pack('>IIII', width, height, fmt, len(payload))); stream.write(payload)
    except BaseException:
        if created: out.unlink(missing_ok=True)
        raise
    print(f'Created {len(rows)} private fixtures; do not share this file.')


if __name__ == '__main__': main()
