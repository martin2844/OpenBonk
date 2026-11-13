#!/usr/bin/env python3
"""Evaluate Steam-to-Android texture conversion in a private research OBB.

Candidate matching is heuristic; generated pixels can differ visually. This
does not produce a distributable APK. Inputs, output and report stay local.
Requires UnityPy, Pillow and UnityPy's texture encoding dependencies.
"""
import argparse
from collections import Counter, defaultdict
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
GROUPS = ('sharedassets1', 'sharedassets2')


def mip_size(fmt, width, height):
    if width < 1 or height < 1: raise ValueError('invalid dimensions')
    if fmt in (1, 3, 4): return width * height * fmt
    if fmt in (10, 12, 25, 45, 47): return ((width + 3) // 4) * ((height + 3) // 4) * (8 if fmt in (10, 45) else 16)
    raise ValueError('unsupported texture format')


def mip_slices(data, fmt, width, height, count):
    if count < 1 or count > 32: raise ValueError('invalid mip count')
    out, offset = [], 0
    for level in range(count):
        w, h = max(1, width >> level), max(1, height >> level)
        size = mip_size(fmt, w, h)
        if offset + size > len(data): raise ValueError('truncated mip data')
        out.append((w, h, data[offset:offset + size]))
        offset += size
    if offset != len(data): raise ValueError('unexpected extra mip data')
    return out


def parts(archive, group):
    prefix = PREFIX + group + '.assets.split'
    selected = []
    for info in archive.infolist():
        if info.filename.startswith(prefix):
            suffix = info.filename[len(prefix):]
            if not suffix.isdecimal() or str(int(suffix)) != suffix: raise ValueError('invalid split name')
            selected.append((int(suffix), info))
    selected.sort(key=lambda x: x[0])
    if not selected or [i for i, _ in selected] != list(range(len(selected))): raise ValueError('missing split')
    return [info for _, info in selected]


def run(args):
    import UnityPy
    from PIL import ImageChops, ImageStat
    from UnityPy.export import Texture2DConverter as converter
    source, obb = args.steam_data.resolve(strict=True), args.obb.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (source, obb, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    index = defaultdict(list)
    for group in GROUPS:
        file = source / (group + '.assets')
        if not file.is_file() or file.is_symlink(): raise ValueError('missing regular Steam asset file')
        for obj in UnityPy.load(str(file)).objects:
            if obj.type.name == 'Texture2D':
                tex = obj.read()
                index[(tex.m_Name, tex.m_Width, tex.m_Height)].append(tex)
    changes, outcomes, failures, rows = {}, Counter(), Counter(), []
    output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            for group in GROUPS:
                splits = parts(zin, group)
                asset = b''.join(zin.read(info) for info in splits)
                modified = bytearray(asset)
                for obj in UnityPy.load(asset).objects:
                    if obj.type.name != 'Texture2D': continue
                    target = obj.read()
                    candidates = index[(target.m_Name, target.m_Width, target.m_Height)]
                    if len(candidates) != 1: outcomes['missing_or_ambiguous'] += 1; continue
                    src = candidates[0]
                    if any(getattr(target, k, None) != getattr(src, k, None) for k in ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')) or target.m_ImageCount != 1:
                        outcomes['metadata_mismatch'] += 1; continue
                    if target.m_TextureFormat not in (4, 45, 47) or src.m_TextureFormat not in (1, 3, 4, 10, 12, 25):
                        outcomes['unsupported_format'] += 1; continue
                    if 'normal' in target.m_Name.lower(): outcomes['normal_excluded'] += 1; continue
                    old = bytes(target.image_data)
                    body = obj.get_raw_data()
                    pos = body.find(old)
                    if not old or pos < 0 or body.count(old) != 1: outcomes['nonunique_inline'] += 1; continue
                    try:
                        src_data = bytes(src.get_image_data())
                        source_mips = mip_slices(src_data, src.m_TextureFormat, src.m_Width, src.m_Height, src.m_MipCount)
                        target_mips = mip_slices(old, target.m_TextureFormat, target.m_Width, target.m_Height, target.m_MipCount)
                        chunks = []
                        for w, h, payload in source_mips:
                            image = converter.parse_image_data(payload, w, h, src.m_TextureFormat, src.object_reader.version, src.object_reader.platform)
                            encoded, fmt = converter.image_to_texture2d(image, target.m_TextureFormat, target.object_reader.platform, target.m_PlatformBlob)
                            if int(fmt) != target.m_TextureFormat or len(encoded) != mip_size(target.m_TextureFormat, w, h): raise ValueError('encoded mip differs')
                            chunks.append(encoded)
                        new = b''.join(chunks)
                        if len(new) != len(old): raise ValueError('converted size differs')
                        w, h, old_top = target_mips[0]
                        original_image = converter.parse_image_data(old_top, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                        converted_image = converter.parse_image_data(chunks[0], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                        mean = ImageStat.Stat(ImageChops.difference(original_image, converted_image)).mean
                    except (ValueError, NotImplementedError, TypeError, IndexError, RuntimeError) as error:
                        outcomes['conversion_failed_' + type(error).__name__] += 1
                        failures[type(error).__name__ + ': ' + str(error)[:160]] += 1
                        continue
                    start = obj.byte_start + pos
                    if asset[start:start + len(old)] != old or modified[start:start + len(old)] != old: raise ValueError('target texture offset differs or overlaps')
                    modified[start:start + len(old)] = new
                    rows.append({'group': group, 'target_id': obj.path_id, 'name': target.m_Name,
                                 'source_format': int(src.m_TextureFormat), 'target_format': int(target.m_TextureFormat),
                                 'bytes': len(old), 'top_mean_rgba_error': [round(x, 3) for x in mean]})
                    outcomes['converted'] += 1
                offset = 0
                for info in splits:
                    changes[info.filename] = bytes(modified[offset:offset + info.file_size])
                    offset += info.file_size
                if offset != len(modified): raise ValueError('split length changed')
            if not rows: raise ValueError('no textures converted')
            with zipfile.ZipFile(output / 'converted.obb', 'x') as zout:
                for info in infos:
                    raw = changes.get(info.filename)
                    zout.writestr(info, raw if raw is not None else zin.read(info))
        report = {'schema': 1, 'outcomes': dict(outcomes), 'failures': dict(failures), 'converted_bytes': sum(r['bytes'] for r in rows),
                  'limitations': 'Heuristic mappings and average level-zero RGBA error do not establish visual equivalence.', 'textures': rows}
        (output / 'report.json').write_text(json.dumps(report, separators=(',', ':')) + '\n')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f"Converted {outcomes['converted']} candidate textures ({report['converted_bytes']} bytes). Private output requires visual validation.")
    print('Skipped or failed:', dict((k, v) for k, v in outcomes.items() if k != 'converted'))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--obb', required=True, type=Path)
    p.add_argument('--steam-data', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    run(p.parse_args())


if __name__ == '__main__': main()
