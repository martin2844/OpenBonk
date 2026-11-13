#!/usr/bin/env python3
"""Create private Steam texture mip fixtures for an Android converter check.

The output contains original game pixels and must remain local to the user's
devices. It does not establish visual correctness or release readiness.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


def main():
    import UnityPy
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'converted', 'manifest', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    args = p.parse_args()
    source, converted, manifest = (x.expanduser().resolve(strict=True) for x in (args.steam_data, args.converted, args.manifest))
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (source, converted, manifest, ROOT)): p.error('output must be separate from inputs and checkout')
    report = json.loads(manifest.read_text())
    rows = report.get('textures')
    if report.get('schema') != 1 or not isinstance(rows, list) or not rows: raise ValueError('invalid study report')
    expected = {group: {} for group in study.GROUPS}
    for row in rows:
        group, key = row['group'], row['target_id']
        if group not in expected or type(key) is not int or key in expected[group]: raise ValueError('invalid or duplicate record')
        expected[group][key] = row
    index = defaultdict(list)
    for group in study.GROUPS:
        path = source / (group + '.assets')
        if not path.is_file() or path.is_symlink(): raise ValueError('missing regular Steam asset')
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name == 'Texture2D':
                tex = obj.read()
                index[(tex.m_Name, tex.m_Width, tex.m_Height)].append(tex)
    count = 0
    created = False
    try:
        with zipfile.ZipFile(converted) as z, output.open('xb') as out:
            created = True
            out.write(b'OBTX0001' + bytes(4))
            for group in study.GROUPS:
                data = b''.join(z.read(info) for info in study.parts(z, group))
                objects = {o.path_id: o for o in UnityPy.load(data).objects if o.type.name == 'Texture2D'}
                for key, row in expected[group].items():
                    if key not in objects: raise ValueError('missing target texture')
                    tex = objects[key].read()
                    candidates = index[(tex.m_Name, tex.m_Width, tex.m_Height)]
                    if (len(candidates) != 1 or tex.m_Name != row['name'] or
                            int(tex.m_TextureFormat) != row['target_format'] or int(candidates[0].m_TextureFormat) != row['source_format']):
                        raise ValueError('texture mapping differs from report')
                    src = candidates[0]
                    if any(getattr(src, k, None) != getattr(tex, k, None) for k in ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')): raise ValueError('texture metadata differs')
                    source_mips = study.mip_slices(bytes(src.get_image_data()), src.m_TextureFormat, src.m_Width, src.m_Height, src.m_MipCount)
                    target_mips = study.mip_slices(bytes(tex.image_data), tex.m_TextureFormat, tex.m_Width, tex.m_Height, tex.m_MipCount)
                    for (w, h, input_data), (tw, th, target_data) in zip(source_mips, target_mips):
                        if (w, h) != (tw, th) or len(input_data) > 64 * 1024 * 1024: raise ValueError('invalid mip data')
                        out.write(struct.pack('>5I', w, h, int(src.m_TextureFormat), int(tex.m_TextureFormat), len(input_data)))
                        out.write(hashlib.sha256(target_data).digest()); out.write(input_data); count += 1
            if not count or count > 50000: raise ValueError('invalid fixture count')
            out.seek(8); out.write(struct.pack('>I', count))
    except BaseException:
        if created: output.unlink(missing_ok=True)
        raise
    print(f'Prepared {count} private mip fixtures; do not share this file.')


if __name__ == '__main__': main()
