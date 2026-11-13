#!/usr/bin/env python3
"""Evaluate Steam-to-Android texture conversion in a private research OBB.

Candidate matching is heuristic; generated pixels can differ visually. This
does not produce a distributable APK. Inputs, output and report stay local.
Requires UnityPy, Pillow and UnityPy's texture encoding dependencies.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
GROUPS = ('sharedassets1', 'sharedassets2')
NORMAL_MAPS = frozenset(('Tiles1_Normal', 'Tiles4_Normal'))


def select_source(target, candidates, texture=lambda item: item):
    """Select only metadata-compatible candidates with identical image payloads."""
    if target.m_ImageCount != 1: return None
    keys = ('m_MipCount', 'm_ImageCount', 'm_ColorSpace')
    compatible = [item for item in candidates if all(getattr(target, key, None) == getattr(texture(item), key, None) for key in keys)]
    if not compatible: return None
    if len(compatible) > 1:
        fingerprints = {(int(texture(item).m_TextureFormat), hashlib.sha256(bytes(texture(item).get_image_data())).digest()) for item in compatible}
        if len(fingerprints) != 1: return None
    return compatible[0]


def mip_size(fmt, width, height):
    if width < 1 or height < 1: raise ValueError('invalid dimensions')
    if fmt in (1, 3, 4): return width * height * fmt
    if fmt in (10, 12, 25, 34, 45, 47): return ((width + 3) // 4) * ((height + 3) // 4) * (8 if fmt in (10, 34, 45) else 16)
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


def normal_image(image):
    """Convert DXT5nm's alpha/green channels to an RGB normal map."""
    from PIL import Image
    import math
    image = image.convert('RGBA')
    x, y = image.getchannel('A'), image.getchannel('G')
    lut = bytes(round(math.sqrt(max(0, 1 - (a / 127.5 - 1) ** 2 - (b / 127.5 - 1) ** 2)) * 127.5 + 127.5)
                for a in range(256) for b in range(256))
    z = Image.frombytes('L', image.size, bytes(lut[(a << 8) | b] for a, b in zip(x.tobytes(), y.tobytes())))
    return Image.merge('RGBA', (x, y, z, Image.new('L', image.size, 255)))


def parts(archive, group):
    if re.fullmatch(r'[0-9a-f]{32}', group):
        return [archive.getinfo(PREFIX + group)]
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
    aliases = defaultdict(list)
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
                    src = select_source(target, candidates)
                    if src is None: outcomes['missing_or_ambiguous'] += 1; continue
                    normal = target.m_Name in NORMAL_MAPS and src.m_TextureFormat == 12 and target.m_TextureFormat == 34
                    if target.m_TextureFormat not in (4, 45, 47) and not normal or src.m_TextureFormat not in (1, 3, 4, 10, 12, 25):
                        outcomes['unsupported_format'] += 1; continue
                    if 'normal' in target.m_Name.lower() and not normal: outcomes['normal_excluded'] += 1; continue
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
                            if normal: image = normal_image(image)
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
                                 'source_format': int(src.m_TextureFormat), 'target_format': int(target.m_TextureFormat), 'normal': normal,
                                 'bytes': len(old), 'top_mean_rgba_error': [round(x, 3) for x in mean]})
                    alias_key = (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount, target.m_ColorSpace, target.m_TextureFormat, hashlib.sha256(old).digest())
                    aliases[alias_key].append((rows[-1], new))
                    outcomes['converted'] += 1
                offset = 0
                for info in splits:
                    changes[info.filename] = bytes(modified[offset:offset + info.file_size])
                    offset += info.file_size
                if offset != len(modified): raise ValueError('split length changed')
            # Some standalone assets duplicate an inline texture already converted
            # above. Reuse it only if all matching candidates yield the same bytes.
            for info in infos:
                if not re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', info.filename) or info.file_size > 64 * 1024 * 1024: continue
                asset = zin.read(info)
                try:
                    objects = UnityPy.load(asset).objects
                    if len(objects) != 1 or objects[0].type.name != 'Texture2D': continue
                    obj = objects[0]; target = obj.read(); old = bytes(target.image_data)
                    if not old: continue
                    alias_key = (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount, target.m_ColorSpace, target.m_TextureFormat, hashlib.sha256(old).digest())
                    matches = aliases.get(alias_key, [])
                    if not matches or len({hashlib.sha256(new).digest() for _, new in matches}) != 1: continue
                    parent, new = matches[0]; body = obj.get_raw_data(); pos = body.find(old)
                    if pos < 0 or body.count(old) != 1: continue
                    start = obj.byte_start + pos
                    if asset[start:start + len(old)] != old: raise ValueError('standalone texture offset differs')
                    changes[info.filename] = asset[:start] + new + asset[start + len(old):]
                    row = dict(parent, group=info.filename[len(PREFIX):], target_id=obj.path_id, name=target.m_Name,
                               source_name=parent['name'], alias_group=parent['group'], alias_target_id=parent['target_id'])
                    rows.append(row); outcomes['converted'] += 1; outcomes['standalone_aliases'] += 1
                except (ValueError, NotImplementedError, TypeError, IndexError, RuntimeError):
                    outcomes['standalone_unreadable'] += 1
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
