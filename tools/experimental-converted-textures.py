#!/usr/bin/env python3
"""Evaluate Steam-to-Android texture conversion in a private research OBB.

Candidate matching is heuristic; generated pixels can differ visually. This
does not produce a distributable APK. Inputs, output and report stay local.
Requires UnityPy, Pillow and UnityPy's texture encoding dependencies.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
GROUPS = ('sharedassets1', 'sharedassets2')
NORMAL_MAPS = frozenset(('Tiles1_Normal', 'Tiles4_Normal'))
FONT_ALIASES = {'LiberationSans SDF Atlas_0': 'LiberationSans SDF Atlas'}
RESOURCE_TEXTURES = frozenset(('GridBox_Collider', 'GridBox_Trigger'))
COLLISION_TEXTURES = {'bush': (64, 64, 7, 3, 45), 'retaliation': (32, 32, 1, 12, 47)}
FOREST_TEXTURES = {'Forest1_TopGrass': ('Forest1_TopGrass', 128, 128, 8),
                   'Forest1_TopGrass_0': ('Forest1_TopGrass', 128, 128, 8),
                   'GRASS_1A': ('GRASS_1A', 64, 64, 7)}
SHARED0_SDF_NAME = re.compile(r'(?:NotoSans(?:SC|TC|KR|JP|Thai_Condensed)?-Bold|alagard) SDF Atlas(?: [1-9][0-9]*)?\Z')
MAX_MEAN_ERROR = 10.0
MAX_NORMAL_ANGLE = 10.0


def collision_source(name):
    """Recognize the known case-collision rename without relying on its GUID."""
    if not isinstance(name, str): return None
    match = re.fullmatch(r'(bush|retaliation)__openbonk_[0-9a-f]{8}', name)
    return match.group(1) if match else None


class NormalQualityError(ValueError):
    pass


class ResourceQualityError(ValueError):
    pass


def acceptable_texture_error(mean):
    return len(mean) == 4 and all(math.isfinite(v) and 0 <= v <= MAX_MEAN_ERROR for v in mean)


def acceptable_collision_mip(original, converted):
    from PIL import ImageChops, ImageStat
    a, b = original.convert('RGBA'), converted.convert('RGBA')
    return a.size == b.size and acceptable_texture_error(ImageStat.Stat(ImageChops.difference(a, b)).mean)


def load_quality_encoder(path):
    spec = importlib.util.spec_from_file_location('quality_texture', ROOT / 'tools/quality_texture.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module.QualityEncoder(path)


def acceptable_quality_image(original, converted):
    from PIL import Image, ImageChops, ImageStat
    a, b = original.convert('RGBA'), converted.convert('RGBA')
    if a.size != b.size: return False
    if ImageStat.Stat(ImageChops.difference(a.getchannel('A'), b.getchannel('A'))).mean[0] > 1: return False
    for shade in (0, 255):
        base = Image.new('RGBA', a.size, (shade, shade, shade, 255))
        diff = ImageChops.difference(Image.alpha_composite(base, a).convert('RGB'), Image.alpha_composite(base, b).convert('RGB'))
        if max(ImageStat.Stat(diff).mean) > 1: return False
        r, g, blue = diff.split()
        if ImageChops.lighter(ImageChops.lighter(r, g), blue).getextrema()[1] > 32: return False
    return True


def should_try_quality(mean, encoder, normal, target_format, width, height):
    return bool(not acceptable_texture_error(mean) and encoder and not normal and target_format == 47 and
                1 <= width <= 2048 and 1 <= height <= 2048)


def acceptable_normal_image(original, converted):
    """Compare decoded RGB normal directions, rejecting any unsafe pixel."""
    if original.size != converted.size or original.width < 1 or original.height < 1: return False
    a, b = original.convert('RGB').tobytes(), converted.convert('RGB').tobytes()
    cosine = math.cos(math.radians(MAX_NORMAL_ANGLE))
    for i in range(0, len(a), 3):
        u = (2 * a[i] - 255, 2 * a[i + 1] - 255, 2 * a[i + 2] - 255)
        v = (2 * b[i] - 255, 2 * b[i + 1] - 255, 2 * b[i + 2] - 255)
        u2 = sum(x * x for x in u); v2 = sum(x * x for x in v)
        if u2 < 26 * 26 or v2 < 26 * 26 or sum(x * y for x, y in zip(u, v)) < cosine * math.sqrt(u2 * v2):
            return False
    return True


def acceptable_sdf_image(original, converted):
    if not acceptable_quality_image(original, converted): return False
    a = original.convert('RGBA').getchannel('A').tobytes()
    b = converted.convert('RGBA').getchannel('A').tobytes()
    edge = crossings = 0
    for x, y in zip(a, b):
        if abs(x - y) > 10: return False
        if 96 <= x <= 160:
            edge += 1
            crossings += (x >= 128) != (y >= 128)
    return bool(edge and crossings <= edge * 0.02)


def shared0_sdf_target(texture):
    return (isinstance(texture.m_Name, str) and SHARED0_SDF_NAME.fullmatch(texture.m_Name) is not None and
            (texture.m_Width, texture.m_Height, texture.m_MipCount, texture.m_ImageCount,
             int(texture.m_TextureFormat)) == (1024, 1024, 1, 1, 47))


def should_retry_shared0_sdf(candidate, encoder, original, converted):
    return bool(candidate and encoder is not None and not acceptable_sdf_image(original, converted))


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


def select_forest_source(target, candidates, texture=lambda item: item, matches=None):
    """Select a unique known Forest source after checking every decoded mip."""
    spec = FOREST_TEXTURES.get(target.m_Name)
    if not spec or (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount,
                    int(target.m_TextureFormat)) != (*spec[1:], 1, 45): return None
    if matches is None:
        from PIL import ImageChops, ImageStat
        from UnityPy.export import Texture2DConverter as converter
        def matches(src):
            left = mip_slices(bytes(src.get_image_data()), src.m_TextureFormat, src.m_Width, src.m_Height, src.m_MipCount)
            right = mip_slices(bytes(target.image_data), target.m_TextureFormat, target.m_Width, target.m_Height, target.m_MipCount)
            for (w, h, a), (_, _, b) in zip(left, right):
                original = converter.parse_image_data(a, w, h, src.m_TextureFormat, src.object_reader.version, src.object_reader.platform).convert('RGBA')
                reference = converter.parse_image_data(b, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                if not acceptable_texture_error(ImageStat.Stat(ImageChops.difference(original, reference)).mean): return False
            return True
    selected = []
    for item in candidates:
        src = texture(item)
        if (src.m_Name, src.m_Width, src.m_Height, src.m_MipCount, src.m_ImageCount, src.m_ColorSpace,
                int(src.m_TextureFormat)) != (spec[0], *spec[1:], 1, target.m_ColorSpace, 10): continue
        try:
            if matches(src): selected.append(item)
        except (ValueError, OSError, NotImplementedError, TypeError): continue
    fingerprints = {hashlib.sha256(bytes(texture(item).get_image_data())).digest() for item in selected}
    return selected[0] if len(fingerprints) == 1 else None


def select_shadow_source(target, candidates, texture=lambda item: item):
    """Choose one unambiguous, visually close source for the known shadow."""
    if (target.m_Name != 'DropShadow' or (target.m_Width, target.m_Height, target.m_MipCount,
            target.m_ImageCount, int(target.m_TextureFormat)) != (512, 512, 1, 1, 47)):
        return None
    try:
        image = target.image
        matches = [item for item in candidates
                   if (texture(item).m_Name, texture(item).m_Width, texture(item).m_Height, texture(item).m_MipCount,
                       texture(item).m_ImageCount, texture(item).m_ColorSpace, int(texture(item).m_TextureFormat)) ==
                      ('DropShadow', 512, 512, 1, 1, target.m_ColorSpace, 12)
                   and acceptable_quality_image(image, texture(item).image)]
        return matches[0] if len(matches) == 1 else None
    except (ValueError, OSError, NotImplementedError):
        return None


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
    quality = None
    if getattr(args, 'quality_library', None):
        quality = load_quality_encoder(args.quality_library)
    font_fallback = getattr(args, 'font_fallback', False)
    resource_fallback = getattr(args, 'resource_fallback', False)
    shadow_fallback = getattr(args, 'shadow_fallback', False)
    shared0_sdf = getattr(args, 'shared0_sdf', False)
    if (font_fallback or resource_fallback or shadow_fallback) and quality is None: raise ValueError('resource, font or shadow fallback requires the quality encoder')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (source, obb, ROOT)): raise ValueError('output must be separate from inputs and checkout')
    index = defaultdict(list); resources_index = defaultdict(list); shared0_index = defaultdict(list)
    for group in (*GROUPS, *(['sharedassets0'] if shared0_sdf else []), *(['resources'] if font_fallback or resource_fallback else [])):
        file = source / (group + '.assets')
        if not file.is_file() or file.is_symlink(): raise ValueError('missing regular Steam asset file')
        for obj in UnityPy.load(str(file)).objects:
            if obj.type.name == 'Texture2D':
                tex = obj.read()
                if group == 'resources' and tex.m_Name not in set(FONT_ALIASES.values()) | RESOURCE_TEXTURES: continue
                (resources_index if group == 'resources' else shared0_index if group == 'sharedassets0' else index)[(tex.m_Name, tex.m_Width, tex.m_Height)].append(tex)
    changes, outcomes, failures, rows = {}, Counter(), Counter(), []
    aliases = defaultdict(list); quality_excluded = set()
    output.mkdir(mode=0o700)
    try:
        with zipfile.ZipFile(obb) as zin:
            infos = zin.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate OBB entry')
            standalone = [info.filename[len(PREFIX):] for info in infos
                          if re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', info.filename) and info.file_size <= 64 * 1024 * 1024]
            target_groups = (*GROUPS, *(['sharedassets0'] if shared0_sdf else []))
            for group in (*target_groups, *standalone):
                splits = parts(zin, group)
                asset = b''.join(zin.read(info) for info in splits)
                modified = bytearray(asset)
                try: objects = UnityPy.load(asset).objects
                except Exception:
                    if group in target_groups: raise
                    outcomes['standalone_unreadable'] += 1; continue
                if group not in target_groups and (len(objects) != 1 or objects[0].type.name != 'Texture2D'): continue
                converted_before = len(rows)
                for obj in objects:
                    if obj.type.name != 'Texture2D': continue
                    target = obj.read()
                    shared0_candidate = group == 'sharedassets0' and shared0_sdf_target(target)
                    if group == 'sharedassets0' and not shared0_candidate: continue
                    font_candidate = bool(font_fallback and group not in GROUPS and target.m_Name in FONT_ALIASES)
                    resource_candidate = bool(resource_fallback and group not in GROUPS and target.m_Name in RESOURCE_TEXTURES)
                    alias_source = collision_source(target.m_Name)
                    collision_spec = COLLISION_TEXTURES.get(alias_source)
                    collision = bool(group == 'sharedassets1' and collision_spec and
                                     (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount, int(target.m_TextureFormat)) == (*collision_spec[:3], 1, collision_spec[4]))
                    forest_candidate = group == 'sharedassets1' and target.m_Name in FOREST_TEXTURES
                    source_name = (FONT_ALIASES[target.m_Name] if font_candidate else alias_source if collision else
                                   FOREST_TEXTURES[target.m_Name][0] if forest_candidate else target.m_Name)
                    candidates = (shared0_index if shared0_candidate else resources_index if font_candidate or resource_candidate else index)[(source_name, target.m_Width, target.m_Height)]
                    src = select_forest_source(target, candidates) if forest_candidate else select_source(target, candidates)
                    shadow = bool(src is None and shadow_fallback and group == 'sharedassets1' and target.m_Name == 'DropShadow')
                    if shadow: src = select_shadow_source(target, candidates)
                    if src is None:
                        if font_candidate or resource_candidate: quality_excluded.add(PREFIX + group)
                        outcomes['missing_or_ambiguous'] += 1; continue
                    if shared0_candidate and (src.m_Name != target.m_Name or int(src.m_TextureFormat) != 1):
                        outcomes['shared0_sdf_metadata_differs'] += 1; continue
                    if collision and int(src.m_TextureFormat) != collision_spec[3]:
                        outcomes['collision_metadata_differs'] += 1; continue
                    font = bool(font_candidate and
                                source_name == src.m_Name and int(src.m_TextureFormat) == 1 and int(target.m_TextureFormat) == 47 and
                                (target.m_Width, target.m_Height, target.m_MipCount) == (1024, 1024, 1))
                    if font_candidate and not font:
                        quality_excluded.add(PREFIX + group)
                        outcomes['font_metadata_differs'] += 1; continue
                    resource = bool(resource_candidate and source_name == src.m_Name and int(src.m_TextureFormat) == 12 and
                                    int(target.m_TextureFormat) == 47 and (target.m_Width, target.m_Height, target.m_MipCount) == (512, 512, 10))
                    if resource_candidate and not resource:
                        quality_excluded.add(PREFIX + group)
                        outcomes['resource_metadata_differs'] += 1; continue
                    normal = target.m_Name in NORMAL_MAPS and src.m_TextureFormat == 12 and target.m_TextureFormat == 34
                    high_quality = False
                    if target.m_TextureFormat not in (4, 45, 47) and not normal or src.m_TextureFormat not in (1, 3, 4, 10, 12, 25):
                        outcomes['unsupported_format'] += 1; continue
                    direct_normal = bool(getattr(args, 'normal_fallback', False) and not normal and 'normal' in target.m_Name.lower() and
                                         int(src.m_TextureFormat) in (3, 4, 10, 25) and int(target.m_TextureFormat) == 45)
                    if 'normal' in target.m_Name.lower() and not normal and not direct_normal: outcomes['normal_excluded'] += 1; continue
                    old = bytes(target.image_data)
                    body = obj.get_raw_data()
                    pos = body.find(old)
                    if not old or pos < 0 or body.count(old) != 1: outcomes['nonunique_inline'] += 1; continue
                    try:
                        src_data = bytes(src.get_image_data())
                        source_mips = mip_slices(src_data, src.m_TextureFormat, src.m_Width, src.m_Height, src.m_MipCount)
                        target_mips = mip_slices(old, target.m_TextureFormat, target.m_Width, target.m_Height, target.m_MipCount)
                        def encode_mips(use_quality):
                            encoded_mips = []
                            for level, (w, h, payload) in enumerate(source_mips):
                                if use_quality:
                                    encoded, fmt = quality.encode(payload, w, h, int(src.m_TextureFormat)), target.m_TextureFormat
                                else:
                                    image = converter.parse_image_data(payload, w, h, src.m_TextureFormat, src.object_reader.version, src.object_reader.platform)
                                    if normal: image = normal_image(image)
                                    encoded, fmt = converter.image_to_texture2d(image, target.m_TextureFormat, target.object_reader.platform, target.m_PlatformBlob)
                                if int(fmt) != target.m_TextureFormat or len(encoded) != mip_size(target.m_TextureFormat, w, h): raise ValueError('encoded mip differs')
                                if direct_normal:
                                    original = converter.parse_image_data(target_mips[level][2], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform)
                                    converted = converter.parse_image_data(encoded, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform)
                                    if not acceptable_normal_image(original, converted): raise NormalQualityError('normal direction differs')
                                if resource or shadow:
                                    original = converter.parse_image_data(target_mips[level][2], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform)
                                    converted = converter.parse_image_data(encoded, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform)
                                    if not acceptable_quality_image(original, converted): raise ResourceQualityError('resource mip quality differs')
                                if collision:
                                    original = converter.parse_image_data(target_mips[level][2], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                                    converted = converter.parse_image_data(encoded, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                                    if not acceptable_collision_mip(original, converted): raise ResourceQualityError('collision mip quality differs')
                                if forest_candidate:
                                    original = converter.parse_image_data(target_mips[level][2], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                                    converted = converter.parse_image_data(encoded, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                                    error = ImageStat.Stat(ImageChops.difference(original, converted)).mean
                                    limit = 12.0 if target.m_Name == 'GRASS_1A' else MAX_MEAN_ERROR
                                    if len(error) != 4 or any(not math.isfinite(v) or v < 0 or v > limit for v in error):
                                        raise ResourceQualityError('forest mip quality differs')
                                encoded_mips.append(encoded)
                            return encoded_mips
                        chunks = encode_mips(font or resource or shadow); high_quality = font or resource or shadow
                        new = b''.join(chunks)
                        if len(new) != len(old): raise ValueError('converted size differs')
                        w, h, old_top = target_mips[0]
                        original_image = converter.parse_image_data(old_top, w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                        converted_image = converter.parse_image_data(chunks[0], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                        mean = ImageStat.Stat(ImageChops.difference(original_image, converted_image)).mean
                        retry_sdf = should_retry_shared0_sdf(shared0_candidate, quality, original_image, converted_image)
                        if retry_sdf or (not font and not resource and not shared0_candidate and should_try_quality(mean, quality, normal, target.m_TextureFormat, target.m_Width, target.m_Height)):
                            chunks = encode_mips(True); new = b''.join(chunks); high_quality = True
                            if len(new) != len(old): raise ValueError('quality converted size differs')
                            converted_image = converter.parse_image_data(chunks[0], w, h, target.m_TextureFormat, target.object_reader.version, target.object_reader.platform).convert('RGBA')
                            mean = ImageStat.Stat(ImageChops.difference(original_image, converted_image)).mean
                    except NormalQualityError:
                        if group not in GROUPS: quality_excluded.add(PREFIX + group)
                        outcomes['normal_high_angle'] += 1; continue
                    except ResourceQualityError:
                        quality_excluded.add(PREFIX + group)
                        outcomes['resource_high_error'] += 1; continue
                    except (ValueError, NotImplementedError, TypeError, IndexError, RuntimeError) as error:
                        if font_candidate or resource_candidate: quality_excluded.add(PREFIX + group)
                        outcomes['conversion_failed_' + type(error).__name__] += 1
                        failures[type(error).__name__ + ': ' + str(error)[:160]] += 1
                        continue
                    # A high level-zero error indicates a questionable match or
                    # conversion; keep the existing texture for local review.
                    accepted = (True if forest_candidate else acceptable_sdf_image(original_image, converted_image) if font or shared0_candidate else
                                acceptable_quality_image(original_image, converted_image) if high_quality else acceptable_texture_error(mean))
                    if not accepted:
                        if group not in GROUPS: quality_excluded.add(PREFIX + group)
                        outcomes['high_error'] += 1; continue
                    start = obj.byte_start + pos
                    if asset[start:start + len(old)] != old or modified[start:start + len(old)] != old: raise ValueError('target texture offset differs or overlaps')
                    modified[start:start + len(old)] = new
                    rows.append({'group': group, 'target_id': obj.path_id, 'name': target.m_Name,
                                 'source_format': int(src.m_TextureFormat), 'target_format': int(target.m_TextureFormat), 'normal': normal, 'quality': high_quality, 'direct_normal': direct_normal,
                                 'bytes': len(old), 'top_mean_rgba_error': [round(x, 3) for x in mean]})
                    if font: rows[-1].update(font=True, source_name=source_name)
                    if shared0_candidate: rows[-1].update(shared0_sdf=True)
                    if resource: rows[-1].update(resource=True, source_name=source_name)
                    if shadow: rows[-1].update(shadow=True)
                    if collision: rows[-1].update(collision_alias=True, source_name=source_name)
                    if forest_candidate: rows[-1].update(forest_source=True, source_name=source_name, source_sha256=hashlib.sha256(src_data).hexdigest())
                    alias_key = (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount, target.m_ColorSpace, target.m_TextureFormat, hashlib.sha256(old).digest())
                    if group in GROUPS and not shadow and not collision and not forest_candidate: aliases[alias_key].append((rows[-1], new))
                    outcomes['converted'] += 1
                if group not in target_groups and len(rows) == converted_before: continue
                offset = 0
                for info in splits:
                    changes[info.filename] = bytes(modified[offset:offset + info.file_size])
                    offset += info.file_size
                if offset != len(modified): raise ValueError('split length changed')
            # Some standalone assets duplicate an inline texture already converted
            # above. Reuse it only if all matching candidates yield the same bytes.
            for info in infos:
                if (not re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', info.filename) or
                        info.file_size > 64 * 1024 * 1024 or info.filename in changes or info.filename in quality_excluded): continue
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
    p.add_argument('--quality-library', type=Path, help='optional experimental desktop quality encoder')
    p.add_argument('--normal-fallback', action='store_true', help='experimentally convert RGB normal maps after checking every mip')
    p.add_argument('--shared0-sdf', action='store_true', help='study selected sharedassets0 font atlases with strict edge checks; requires relocated sharedassets0 in the OBB')
    p.add_argument('--font-fallback', action='store_true', help='experimentally reconstruct an SDF font atlas with the quality encoder')
    p.add_argument('--resource-fallback', action='store_true', help='experimentally reconstruct selected resource textures with per-mip quality checks')
    p.add_argument('--shadow-fallback', action='store_true', help='experimentally choose a unique matching DropShadow with strict quality checks')
    run(p.parse_args())


if __name__ == '__main__': main()
