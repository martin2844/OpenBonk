#!/usr/bin/env python3
"""Evaluate six-face Steam cubemap conversion in a private research OBB.

The decoder, inputs, converted OBB and report stay local. Converted skies may
look different; this experiment does not create a distributable artifact.
Requires UnityPy, Pillow, texture encoders and a local Unity Crunch decoder.
"""
import argparse
from collections import defaultdict
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('texture_study', ROOT / 'tools/experimental-converted-textures.py')
study = importlib.util.module_from_spec(spec)
spec.loader.exec_module(study)


def run(args):
    import UnityPy
    from PIL import ImageChops, ImageStat
    from UnityPy.export import Texture2DConverter as converter
    source, obb, decoder = (p.resolve(strict=True) for p in (args.steam_data, args.obb, args.decoder))
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (source, obb, decoder, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    asset = source / 'sharedassets1.assets'
    if not asset.is_file() or asset.is_symlink() or not decoder.is_file() or decoder.is_symlink(): raise ValueError('missing regular input')
    index = defaultdict(list)
    for obj in UnityPy.load(str(asset)).objects:
        if obj.type.name == 'Cubemap':
            cube = obj.read()
            index[(cube.m_Name, cube.m_Width, cube.m_Height)].append(cube)
    rows = []; output.mkdir(mode=0o700)
    try:
        with tempfile.TemporaryDirectory(prefix='.cubemap-', dir=output) as temp, zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            parts = study.parts(zin, 'sharedassets1'); data = b''.join(zin.read(i) for i in parts); modified = bytearray(data)
            for obj in UnityPy.load(data).objects:
                if obj.type.name != 'Cubemap': continue
                dst = obj.read(); matches = index[(dst.m_Name, dst.m_Width, dst.m_Height)]
                if len(matches) != 1: raise ValueError('missing or ambiguous cubemap source')
                src = matches[0]
                attrs = ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')
                if (any(getattr(dst, k, None) != getattr(src, k, None) for k in attrs) or
                        (dst.m_Width, dst.m_Height, dst.m_MipCount, dst.m_ImageCount, int(dst.m_TextureFormat)) != (512, 512, 1, 6, 45) or
                        int(src.m_TextureFormat) not in (12, 29)):
                    raise ValueError('unsupported cubemap metadata')
                source_bytes, old = bytes(src.get_image_data()), bytes(dst.image_data)
                if len(old) != 6 * 131072: raise ValueError('unsupported target cubemap size')
                if src.m_TextureFormat == 29:
                    input_path, output_path = Path(temp) / 'input.crn', Path(temp) / 'output.dxt'
                    input_path.write_bytes(source_bytes)
                    output_path.unlink(missing_ok=True)
                    subprocess.run(([args.runner] if args.runner else []) + [str(decoder), str(input_path), str(output_path)],
                                   check=True, stdout=subprocess.DEVNULL)
                    decoded = output_path.read_bytes()
                else: decoded = source_bytes
                if len(decoded) != 6 * 262144: raise ValueError('unexpected source face size')
                encoded, errors = [], []
                for face in range(6):
                    image = converter.parse_image_data(decoded[face * 262144:(face + 1) * 262144], 512, 512, 12, src.object_reader.version, src.object_reader.platform)
                    pixels, fmt = converter.image_to_texture2d(image, 45, dst.object_reader.platform, dst.m_PlatformBlob)
                    if int(fmt) != 45 or len(pixels) != 131072: raise ValueError('unexpected target face encoding')
                    original = converter.parse_image_data(old[face * 131072:(face + 1) * 131072], 512, 512, 45, dst.object_reader.version, dst.object_reader.platform).convert('RGBA')
                    rebuilt = converter.parse_image_data(pixels, 512, 512, 45, dst.object_reader.version, dst.object_reader.platform).convert('RGBA')
                    errors.append([round(x, 3) for x in ImageStat.Stat(ImageChops.difference(original, rebuilt)).mean])
                    encoded.append(pixels)
                body = obj.get_raw_data(); position = body.find(old)
                if position < 0 or body.count(old) != 1: raise ValueError('unsupported inline cubemap layout')
                offset = obj.byte_start + position
                if data[offset:offset + len(old)] != old or modified[offset:offset + len(old)] != old: raise ValueError('overlapping cubemap range')
                modified[offset:offset + len(old)] = b''.join(encoded)
                rows.append({'target_id': obj.path_id, 'name': dst.m_Name, 'source_format': int(src.m_TextureFormat),
                             'bytes': len(old), 'face_mean_rgba_errors': errors})
            if not rows: raise ValueError('no cubemaps found')
            changes = {}; position = 0
            for part in parts:
                changes[part.filename] = bytes(modified[position:position + part.file_size]); position += part.file_size
            if position != len(data): raise ValueError('split length changed')
            with zipfile.ZipFile(output / 'converted.obb', 'x') as zout:
                for info in infos:
                    raw = changes.get(info.filename)
                    zout.writestr(info, raw if raw is not None else zin.read(info))
        report = {'schema': 1, 'limitations': 'Face averages do not prove visual equivalence or gameplay behavior.', 'cubemaps': rows}
        (output / 'report.json').write_text(json.dumps(report, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Converted {len(rows)} private cubemaps ({sum(r["bytes"] for r in rows)} bytes); visual review is required.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'obb', 'decoder', 'output'): p.add_argument('--' + name, type=Path, required=True)
    p.add_argument('--runner', help='optional decoder wrapper, e.g. steam-run')
    run(p.parse_args())


if __name__ == '__main__': main()
