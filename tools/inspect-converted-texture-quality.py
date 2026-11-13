#!/usr/bin/env python3
"""Verify a private texture study and measure visible pixel differences.

Mean and threshold metrics cannot prove visual equivalence, mapping identity,
or shader behavior. The generated report contains game-derived metadata and
must remain local. Requires UnityPy and Pillow.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


def visible_metrics(original, converted):
    from PIL import Image, ImageChops, ImageStat
    a, b = original.convert('RGBA'), converted.convert('RGBA')
    if a.size != b.size: raise ValueError('image sizes differ')
    values = []
    for shade in (0, 255):
        base = Image.new('RGBA', a.size, (shade, shade, shade, 255))
        x = Image.alpha_composite(base, a).convert('RGB')
        y = Image.alpha_composite(base, b).convert('RGB')
        diff = ImageChops.difference(x, y)
        r, g, blue = diff.split()
        maximum = ImageChops.lighter(ImageChops.lighter(r, g), blue)
        histogram = maximum.histogram()
        pixels = a.width * a.height
        values.append({'background': shade, 'mean_rgb': [round(v, 4) for v in ImageStat.Stat(diff).mean],
                       'fraction_any_channel_over_32': round(sum(histogram[33:]) / pixels, 6),
                       'maximum_channel_error': maximum.getextrema()[1]})
    alpha = ImageChops.difference(a.getchannel('A'), b.getchannel('A'))
    alpha_hist = alpha.histogram()
    return {'composited': values, 'alpha_mean_error': round(ImageStat.Stat(alpha).mean[0], 4),
            'alpha_fraction_over_32': round(sum(alpha_hist[33:]) / (a.width * a.height), 6)}


def hash_entry(z, name):
    with z.open(name) as stream: return hashlib.file_digest(stream, 'sha256').digest()


def run(args):
    import UnityPy
    from UnityPy.export import Texture2DConverter as converter
    before, after, manifest = (p.resolve(strict=True) for p in (args.original, args.converted, args.manifest))
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (before, after, manifest, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    report = json.loads(manifest.read_text())
    rows = report.get('textures')
    if report.get('schema') != 1 or not isinstance(rows, list) or not rows: raise ValueError('invalid study report')
    expected = {group: {} for group in study.GROUPS}
    for row in rows:
        group, key = row['group'], row['target_id']
        if group not in expected or type(key) is not int or key in expected[group]: raise ValueError('duplicate or invalid texture')
        expected[group][key] = row
    measured = []
    with zipfile.ZipFile(before) as original, zipfile.ZipFile(after) as converted:
        names = original.namelist()
        if len(names) != len(set(names)) or names != converted.namelist(): raise ValueError('archive entry lists differ')
        modified = set()
        for group in study.GROUPS:
            splits = study.parts(original, group)
            old = b''.join(original.read(info) for info in splits)
            new = b''.join(converted.read(info.filename) for info in splits)
            if len(old) != len(new): raise ValueError('asset length differs')
            prior = {o.path_id: o for o in UnityPy.load(old).objects if o.type.name == 'Texture2D'}
            current = {o.path_id: o for o in UnityPy.load(new).objects if o.type.name == 'Texture2D'}
            normalized = bytearray(new)
            for key, row in expected[group].items():
                if key not in prior or key not in current: raise ValueError('texture not found')
                a, b = prior[key], current[key]
                x, y = a.read(), b.read()
                attrs = ('m_Name', 'm_Width', 'm_Height', 'm_MipCount', 'm_TextureFormat', 'm_ImageCount', 'm_ColorSpace')
                if any(getattr(x, k, None) != getattr(y, k, None) for k in attrs) or x.m_Name != row['name']: raise ValueError('texture metadata differs')
                x_data, y_data = bytes(x.image_data), bytes(y.image_data)
                if len(x_data) != len(y_data) or len(x_data) != row['bytes']: raise ValueError('texture size differs')
                body = a.get_raw_data(); pos = body.find(x_data)
                if not x_data or pos < 0 or body.count(x_data) != 1: raise ValueError('unsupported texture layout')
                start = a.byte_start + pos
                if old[start:start + len(x_data)] != x_data or new[start:start + len(y_data)] != y_data: raise ValueError('texture bounds differ')
                normalized[start:start + len(x_data)] = old[start:start + len(x_data)]
                old_top = study.mip_slices(x_data, x.m_TextureFormat, x.m_Width, x.m_Height, x.m_MipCount)[0]
                new_top = study.mip_slices(y_data, y.m_TextureFormat, y.m_Width, y.m_Height, y.m_MipCount)[0]
                image_x = converter.parse_image_data(old_top[2], old_top[0], old_top[1], x.m_TextureFormat, x.object_reader.version, x.object_reader.platform)
                image_y = converter.parse_image_data(new_top[2], new_top[0], new_top[1], y.m_TextureFormat, y.object_reader.version, y.object_reader.platform)
                measured.append({**row, 'visible_quality': visible_metrics(image_x, image_y)})
            if normalized != old: raise ValueError('other serialized bytes changed')
            modified.update(info.filename for info in splits)
        for name in names:
            if name not in modified and hash_entry(original, name) != hash_entry(converted, name): raise ValueError('unrelated archive entry differs')
    result = {'schema': 1, 'verified_textures': len(measured), 'verified_unrelated_entries': len(names) - len(modified),
              'limitations': 'Metrics are level-zero comparisons, not proof of correctness, matching identity, or visual/gameplay equivalence.', 'textures': measured}
    with output.open('x', encoding='utf-8') as stream:
        json.dump(result, stream, separators=(',', ':'))
        stream.write('\n')
    print(f'Verified {len(measured)} texture ranges and {len(names) - len(modified)} unrelated entries; local quality report created.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('original', 'converted', 'manifest', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    run(p.parse_args())


if __name__ == '__main__': main()
