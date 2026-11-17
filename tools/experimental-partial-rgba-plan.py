#!/usr/bin/env python3
"""Study exact partial texture reconstruction from local Steam inputs.

The template still contains game data and must remain private. The optional
ETC2 path is experimental; names retain RGBA for plan compatibility. Requires UnityPy.
"""
import argparse
import base64
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parent.parent

def load(name, file):
    spec = importlib.util.spec_from_file_location(name, ROOT / 'tools' / file)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

study = load('partial_texture_study', 'experimental-converted-textures.py')
objects = load('partial_texture_objects', 'experimental-exact-object-plan.py')
masker = load('partial_texture_masker', 'experimental-animation-mask.py')
normal_rgba = load('partial_normal_rgba', 'normal-rgba-reconstruction.py')
ENTRY = re.compile(re.escape(objects.PREFIX) + r'(?:[0-9a-f]{32}|sharedassets[0124]\.assets\.split(?:0|[1-9]\d*))\Z')


def sha(data): return hashlib.sha256(data).hexdigest()


def quality_mode(item):
    value = item.get('quality', item.get('target_format', 4) in (45, 47))
    if type(value) is not bool or (item.get('target_format', 4) == 4 and value):
        raise ValueError('invalid partial texture quality mode')
    return value


def normal_rgba_mode(item):
    value = item.get('normal_rgba', False)
    if type(value) is not bool or (value and (item.get('source_format') != 12 or
            item.get('target_format', 4) != 4 or quality_mode(item) or 'blend' in item)):
        raise ValueError('invalid RGBA normal mode')
    return value


def equal_nonzero(original, output):
    if len(original) != len(output): raise ValueError('texture sizes differ')
    return sum(a != 0 and a == b for a, b in zip(original, output))


def select_encoding(original, quality, fast):
    return (fast, False) if equal_nonzero(original, fast) > equal_nonzero(original, quality) else (quality, True)


def blend_blocks(quality, fast, choices, unit=16):
    if len(quality) != len(fast): raise ValueError('texture sizes differ')
    if type(unit) is not int or unit not in (1, 16): raise ValueError('invalid blend unit')
    blocks = (len(quality) + unit - 1) // unit
    if len(choices) != (blocks + 7) // 8 or (blocks % 8 and choices[-1] >> (blocks % 8)):
        raise ValueError('invalid encoder choices')
    output = bytearray(quality)
    for i in range(blocks):
        if choices[i // 8] & (1 << (i % 8)):
            start = i * unit; output[start:start + unit] = fast[start:start + unit]
    return bytes(output)


def select_blocks(original, quality, fast, unit=16):
    if len(original) != len(quality) or len(original) != len(fast): raise ValueError('texture sizes differ')
    if type(unit) is not int or unit not in (1, 16): raise ValueError('invalid blend unit')
    blocks = (len(original) + unit - 1) // unit
    choices = bytearray((blocks + 7) // 8)
    for i in range(blocks):
        start = i * unit; end = start + unit
        if equal_nonzero(original[start:end], fast[start:end]) > equal_nonzero(original[start:end], quality[start:end]):
            choices[i // 8] |= 1 << (i % 8)
    return blend_blocks(quality, fast, choices, unit), bytes(choices)


def converted_plan_texture(source, item, encoder):
    target = item.get('target_format', 4)
    quality = quality_mode(item)
    normal = normal_rgba_mode(item)
    if 'blend' not in item:
        if 'blend_unit' in item or 'blend_rgba' in item: raise ValueError('blend unit or RGBA choices without base choices')
        output = converted(source, item['width'], item['height'], item['mips'], target, encoder, not quality and target in (45, 47))
        return normal_rgba.reconstruct(output) if normal else output
    mask = item['blend']
    if not quality or target not in (45, 47) or not isinstance(mask, str): raise ValueError('invalid blended texture mode')
    choices = base64.b64decode(mask, validate=True)
    high = converted(source, item['width'], item['height'], item['mips'], target, encoder)
    fast = converted(source, item['width'], item['height'], item['mips'], target, encoder, True)
    unit = item.get('blend_unit', 16)
    output = blend_blocks(high, fast, choices, unit)
    if 'blend_rgba' in item:
        if unit != 1 or not isinstance(item['blend_rgba'], str) or encoder is None: raise ValueError('invalid RGBA encoder mode')
        alternate = converted(source, item['width'], item['height'], item['mips'], target, encoder.rgba_variant())
        output = blend_blocks(output, alternate, base64.b64decode(item['blend_rgba'], validate=True), 1)
    return output


def converted(source, width, height, mips, target_format=4, encoder=None, fast=False):
    from UnityPy.export import Texture2DConverter as cv
    chunks = []
    for w, h, data in study.mip_slices(bytes(source.get_image_data()), int(source.m_TextureFormat), width, height, mips):
        if target_format == 4:
            image = cv.parse_image_data(data, w, h, source.m_TextureFormat, source.object_reader.version, source.object_reader.platform)
            raw, fmt = cv.image_to_texture2d(image, 4, 13, None)
            if int(fmt) != 4 or len(raw) != w * h * 4: raise ValueError('invalid RGBA conversion')
        elif target_format in (45, 47) and fast:
            image = cv.parse_image_data(data, w, h, source.m_TextureFormat, source.object_reader.version, source.object_reader.platform)
            raw, fmt = cv.image_to_texture2d(image, target_format, 13, None)
            if int(fmt) != target_format or len(raw) != study.mip_size(target_format, w, h): raise ValueError('invalid fast ETC2 conversion')
        elif target_format in (45, 47) and encoder is not None:
            raw = encoder.encode(data, w, h, int(source.m_TextureFormat))
            if target_format == 45:
                raw = b''.join(raw[i + 8:i + 16] for i in range(0, len(raw), 16))
            if len(raw) != study.mip_size(target_format, w, h): raise ValueError('invalid ETC2 conversion')
        else:
            raise ValueError('unsupported partial texture conversion')
        chunks.append(raw)
    return b''.join(chunks)


def fragment_rows(parts, offset, original, decoded, texture_id):
    if len(original) != len(decoded): raise ValueError('texture sizes differ')
    rows = []; base = 0
    for part in parts:
        lo, hi = max(offset, base), min(offset + len(original), base + part.file_size)
        if lo < hi:
            start, size = lo - offset, hi - lo
            before = original[start:start + size]; source = decoded[start:start + size]
            masked, bits, count = masker.mask_equal(before, source)
            if count:
                rows.append({'entry': part.filename, 'target_offset': lo - base, 'size': size,
                             'texture_id': texture_id, 'converted_offset': start, 'mask': base64.b64encode(bits).decode('ascii'),
                             'masked_bytes': count, 'original_sha256': sha(before), 'masked_sha256': sha(masked)})
        base += part.file_size
    if offset + len(original) > base: raise ValueError('texture exceeds asset group')
    return rows


def apply(raw, rows, decoded, restore=False):
    result = bytearray(raw); end = 0
    for row in sorted(rows, key=lambda x: x['target_offset']):
        start, size, pos, key = (row[k] for k in ('target_offset', 'size', 'converted_offset', 'texture_id'))
        if any(type(v) is not int for v in (start, size, pos, key)) or start < end or size <= 0 or start + size > len(result) or pos < 0 or key not in decoded or pos + size > len(decoded[key]):
            raise ValueError('invalid or overlapping fragment')
        bits = base64.b64decode(row['mask'], validate=True)
        if len(bits) != (size + 7) // 8 or (size % 8 and bits[-1] >> (size % 8)) or sum(x.bit_count() for x in bits) != row['masked_bytes']:
            raise ValueError('invalid mask')
        current = bytes(result[start:start + size]); source = decoded[key][pos:pos + size]
        if restore:
            if sha(current) != row['masked_sha256']: raise ValueError('masked fragment differs')
            output = masker.restore_masked(current, source, bits)
            if sha(output) != row['original_sha256']: raise ValueError('restored fragment differs')
        else:
            if sha(current) != row['original_sha256']: raise ValueError('original fragment differs')
            output, expected, count = masker.mask_equal(current, source)
            if expected != bits or count != row['masked_bytes'] or sha(output) != row['masked_sha256']: raise ValueError('mask differs')
        result[start:start + size] = output; end = start + size
    return bytes(result)


def sources(folder, include_resources=False):
    import UnityPy
    index = defaultdict(list); by_id = {}
    for group in (*study.GROUPS, *(['sharedassets0', 'sharedassets4', 'resources'] if include_resources else [])):
        file = folder / (group + '.assets')
        if file.is_symlink() or not file.is_file(): raise ValueError('missing regular Steam asset file')
        for obj in UnityPy.load(str(file)).objects:
            if obj.type.name != 'Texture2D': continue
            texture = obj.read(); item = (group, obj.path_id, texture)
            index[(texture.m_Name, texture.m_Width, texture.m_Height)].append(item)
            by_id[(group, obj.path_id)] = texture
    return index, by_id


def rgba_alias(target, payload, candidates):
    """Match small aliases exactly, or large aliases by near-exact nonzero bytes."""
    if (not target.m_Name.endswith('_0') or int(target.m_TextureFormat) != 4 or
            target.m_ImageCount != 1 or not payload):
        return None
    nonzero = len(payload) - payload.count(0)
    if not nonzero: return None
    matches = []
    for item in candidates:
        src = item[2]
        if (int(src.m_TextureFormat) != 4 or
                (src.m_MipCount, src.m_ImageCount, src.m_ColorSpace) !=
                (target.m_MipCount, target.m_ImageCount, target.m_ColorSpace)):
            continue
        try: output = converted(src, target.m_Width, target.m_Height, target.m_MipCount)
        except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError): continue
        if len(output) != len(payload): continue
        matched = sum(a != 0 and a == b for a, b in zip(payload, output))
        if output == payload or (len(payload) >= 65536 and nonzero >= 30000 and matched * 100 >= nonzero * 99):
            matches.append((item, output))
    # Identical candidates are interchangeable; differing candidates are ambiguous.
    if not matches or len({sha(output) for _, output in matches}) != 1: return None
    return matches[0]


def etc2_alpha_alias(target, payload, candidates, encoder):
    """Find a unique renamed alpha source for exact byte-level ETC2 restoration."""
    if (encoder is None or not target.m_Name.endswith('_0') or int(target.m_TextureFormat) != 47 or
            target.m_ImageCount != 1 or len(payload) < 65536):
        return None
    nonzero = len(payload) - payload.count(0)
    if nonzero < 30000: return None
    item = study.select_source(target, candidates, texture=lambda x: x[2])
    if item is None or int(item[2].m_TextureFormat) != 1: return None
    try: output = converted(item[2], target.m_Width, target.m_Height, target.m_MipCount, 47, encoder)
    except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError): return None
    if len(output) != len(payload): return None
    matched = sum(a != 0 and a == b for a, b in zip(payload, output))
    return (item, output) if matched * 2 >= nonzero else None


def eligible_etc2_payload(payload, include_small=False, include_tiny=False):
    minimum_size, minimum_nonzero = (64, 16) if include_tiny else (4096, 1024) if include_small else (65536, 30000)
    return len(payload) >= minimum_size and len(payload) - payload.count(0) >= minimum_nonzero


def partial_etc2_source(target, payload, candidates, encoder, tiny_alias=False):
    """Choose a clearly matching source; restoration copies only equal bytes."""
    if encoder is None or int(target.m_TextureFormat) not in (45, 47) or target.m_ImageCount != 1:
        return None
    nonzero = len(payload) - payload.count(0)
    if not eligible_etc2_payload(payload, True, tiny_alias): return None
    matches = []
    for item in candidates:
        src = item[2]
        if (int(src.m_TextureFormat) not in (1, 3, 4, 10, 12, 25) or
                (src.m_MipCount, src.m_ImageCount, src.m_ColorSpace) !=
                (target.m_MipCount, target.m_ImageCount, target.m_ColorSpace)):
            continue
        try:
            output = converted(src, target.m_Width, target.m_Height, target.m_MipCount,
                               int(target.m_TextureFormat), encoder)
        except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError):
            continue
        if len(output) != len(payload): continue
        score = sum(a != 0 and a == b for a, b in zip(payload, output))
        if score: matches.append((score, item, output))
    if not matches: return None
    matches.sort(key=lambda match: match[0], reverse=True)
    best, item, output = matches[0]
    if tiny_alias:
        # Small aliases need a majority match, or substantial evidence at
        # the same 25% ratio used for larger textures.
        if not ((best >= 16 and best * 2 >= nonzero) or
                (best >= 128 and best * 4 >= nonzero)): return None
    elif best < 1024 or best * 4 < nonzero: return None
    # Identical outputs are interchangeable; dissimilar close contenders are not.
    if any(other * 2 >= best and data != output for other, _, data in matches[1:]): return None
    return item, output


def candidate_sources(index, target, group, name):
    items = index[(name, target.m_Width, target.m_Height)]
    isolated = ('sharedassets0', 'sharedassets4')
    if group in tuple(name + '.assets' for name in isolated):
        return [item for item in items if item[0] + '.assets' == group]
    return [item for item in items if item[0] not in isolated]


def generate(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True); obb = args.obb.resolve(strict=True)
    quality_library = getattr(args, 'etc2_quality_library', None)
    encoder = study.load_quality_encoder(quality_library) if quality_library else None
    rgba_encoder = encoder.rgba_variant() if getattr(args, 'rgba_etc2', False) and encoder else None
    out = objects.new_output(args.output, (source, obb)); index, _ = sources(source, bool(encoder))
    rows = []; textures = []; decoded = {}; normal_seen = False; out.mkdir(mode=0o700)
    with zipfile.ZipFile(obb) as zin:
        infos = masker.zip_entries(zin); groups = objects.asset_groups(zin)
        for group, parts in groups.items():
            if encoder is None and group not in ('sharedassets1.assets', 'sharedassets2.assets'): continue
            if any(not ENTRY.fullmatch(part.filename) for part in parts): continue
            raw = b''.join(zin.read(p) for p in parts)
            try: asset_objects = UnityPy.load(raw).objects
            except Exception: continue
            for obj in asset_objects:
                if obj.type.name != 'Texture2D': continue
                try: target = obj.read()
                except (ValueError, TypeError, IndexError): continue
                if group == 'sharedassets0.assets' and target.m_Name != 'Emission_2': continue
                target_format = int(target.m_TextureFormat)
                if target_format != 4 and (encoder is None or target_format not in (45, 47)): continue
                body = obj.get_raw_data(); payload = bytes(target.image_data); pos = body.find(payload)
                if not payload or pos < 0 or body.count(payload) != 1: continue
                item = study.select_source(target, candidate_sources(index, target, group, target.m_Name), texture=lambda x: x[2])
                output = None
                if item is None and target.m_Name.endswith('_0'):
                    candidates = candidate_sources(index, target, group, target.m_Name[:-2])
                    alias = (rgba_alias(target, payload, candidates) if target_format == 4 else
                             etc2_alpha_alias(target, payload, candidates, encoder))
                    if alias: item, output = alias
                if item is None and target_format in (45, 47) and getattr(args, 'include_small_etc2', False):
                    source_name = ('Emission' if group == 'sharedassets0.assets' and target.m_Name == 'Emission_2'
                                   else target.m_Name[:-2] if target.m_Name.endswith('_0') else target.m_Name)
                    partial = partial_etc2_source(target, payload, candidate_sources(index, target, group, source_name), encoder)
                    if partial: item, output = partial
                if item is None and target_format in (45, 47) and getattr(args, 'include_etc2_aliases', False):
                    source_name = re.sub(r'_[0-9]+$', '', target.m_Name)
                    partial = partial_etc2_source(target, payload, candidate_sources(index, target, group, source_name), encoder, tiny_alias=True)
                    if partial: item, output = partial
                if item is None: continue
                group_name, source_id, src = item
                if int(src.m_TextureFormat) not in (1, 3, 4, 10, 12, 25): continue
                if target_format != 4 and not eligible_etc2_payload(payload, getattr(args, 'include_small_etc2', False), getattr(args, 'include_tiny_etc2', False)): continue
                if output is None:
                    try: output = converted(src, target.m_Width, target.m_Height, target.m_MipCount, target_format, encoder)
                    except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError): continue
                normal = (getattr(args, 'normal_rgba', False) and group == 'sharedassets1.assets' and
                          target.m_Name == 'IL3DN_WaterNormal')
                if normal:
                    if normal_seen or (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount,
                                       target_format, int(src.m_TextureFormat)) != (32, 32, 6, 1, 4, 12):
                        raise ValueError('unexpected or duplicate RGBA normal texture')
                    normal_seen = True
                    output = normal_rgba.reconstruct(output)
                quality = target_format in (45, 47); blend = None; blend_rgba = None
                if quality and getattr(args, 'fast_etc2', False):
                    try:
                        fast_output = converted(src, target.m_Width, target.m_Height, target.m_MipCount, target_format, encoder, True)
                        quality_output = output
                        output, quality = select_encoding(payload, quality_output, fast_output)
                        if getattr(args, 'blend_etc2', False):
                            unit = 1 if getattr(args, 'byte_etc2', False) else 16
                            blended, choices = select_blocks(payload, quality_output, fast_output, unit)
                            if equal_nonzero(payload, blended) > equal_nonzero(payload, output):
                                output = blended; quality = True; blend = base64.b64encode(choices).decode('ascii')
                        if rgba_encoder is not None:
                            base, base_choices = select_blocks(payload, quality_output, fast_output, 1)
                            alternate = converted(src, target.m_Width, target.m_Height, target.m_MipCount, target_format, rgba_encoder)
                            improved, alternate_choices = select_blocks(payload, base, alternate, 1)
                            if equal_nonzero(payload, improved) > equal_nonzero(payload, output):
                                output = improved; quality = True
                                blend = base64.b64encode(base_choices).decode('ascii')
                                blend_rgba = base64.b64encode(alternate_choices).decode('ascii')
                    except (ValueError, TypeError, IndexError, NotImplementedError, RuntimeError): pass
                ident = len(textures); fragments = fragment_rows(parts, obj.byte_start + pos, payload, output, ident)
                if not fragments:
                    if normal: raise ValueError('RGBA normal has no matching bytes')
                    continue
                source_data = bytes(src.get_image_data())
                texture = {'id': ident, 'source_group': group_name, 'source_id': source_id,
                                 'width': target.m_Width, 'height': target.m_Height, 'mips': target.m_MipCount,
                                 'source_format': int(src.m_TextureFormat), 'target_format': target_format, 'quality': quality, 'source_sha256': sha(source_data),
                                 'converted_sha256': sha(output)}
                if normal: texture['normal_rgba'] = True
                if blend is not None:
                    texture['blend'] = blend
                    if getattr(args, 'byte_etc2', False): texture['blend_unit'] = 1
                if blend_rgba is not None: texture['blend_rgba'] = blend_rgba
                textures.append(texture)
                decoded[ident] = output; rows.extend(fragments)
        if getattr(args, 'normal_rgba', False) and not normal_seen: raise ValueError('RGBA normal texture missing or unsupported')
        if not rows: raise ValueError('no matching RGBA bytes')
        grouped = defaultdict(list)
        for row in rows: grouped[row['entry']].append(row)
        with zipfile.ZipFile(out / 'template.obb', 'x') as zout:
            for info in infos:
                raw = zin.read(info); zout.writestr(info, apply(raw, grouped[info.filename], decoded) if info.filename in grouped else raw)
    plan = {'schema': 1, 'kind': 'experimental-partial-rgba', 'template_sha256': masker.file_digest(out / 'template.obb'),
            'textures': textures, 'records': rows}
    (out / 'plan.json').write_text(json.dumps(plan, separators=(',', ':')) + '\n')
    print('Masked', sum(r['masked_bytes'] for r in rows), 'nonzero bytes from', len(textures), 'textures. Template remains private.')


def restore(args):
    source = args.steam_data.resolve(strict=True); template = args.template.resolve(strict=True); plan_file = args.plan.resolve(strict=True)
    out = objects.new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-partial-rgba' or plan.get('template_sha256') != masker.file_digest(template): raise ValueError('plan or template differs')
    encoder = study.load_quality_encoder(args.etc2_quality_library) if getattr(args, 'etc2_quality_library', None) else None
    _, by_id = sources(source, bool(encoder)); decoded = {}
    for row in plan['textures']:
        key = row['id']
        if type(key) is not int or key in decoded or row['source_group'] not in (*study.GROUPS, *(['sharedassets0', 'sharedassets4', 'resources'] if encoder else [])): raise ValueError('invalid texture identity')
        tex = by_id[(row['source_group'], row['source_id'])]
        if (tex.m_Width, tex.m_Height, tex.m_MipCount, int(tex.m_TextureFormat), sha(bytes(tex.get_image_data()))) != (row['width'], row['height'], row['mips'], row['source_format'], row['source_sha256']): raise ValueError('Steam texture differs')
        output = converted_plan_texture(tex, row, encoder)
        if sha(output) != row['converted_sha256']: raise ValueError('converted texture differs')
        decoded[key] = output
    grouped = defaultdict(list)
    for row in plan['records']:
        if not isinstance(row.get('entry'), str) or not ENTRY.fullmatch(row['entry']): raise ValueError('invalid template entry')
        grouped[row['entry']].append(row)
    if not grouped: raise ValueError('empty plan')
    seen = set()
    try:
        with zipfile.ZipFile(template) as zin, zipfile.ZipFile(out, 'x') as zout:
            for info in masker.zip_entries(zin):
                raw = zin.read(info)
                if info.filename in grouped: raw = apply(raw, grouped[info.filename], decoded, True); seen.add(info.filename)
                zout.writestr(info, raw)
        if seen != set(grouped): raise ValueError('missing template entry')
    except BaseException:
        if out.is_file(): out.unlink()
        raise
    print('Restored partial RGBA fragments; compare archive entries separately. Output remains private.')


def export_android(args):
    """Bind desktop selections to exact streamed Steam offsets for Android."""
    source = args.steam_data.resolve(strict=True); template = args.template.resolve(strict=True); plan_file = args.plan.resolve(strict=True)
    out = objects.new_output(args.output, (source, template, plan_file)); plan = json.loads(plan_file.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-partial-rgba' or plan.get('template_sha256') != masker.file_digest(template): raise ValueError('plan or template differs')
    encoder = study.load_quality_encoder(args.etc2_quality_library) if getattr(args, 'etc2_quality_library', None) else None
    _, by_id = sources(source, bool(encoder)); records = []; used = {}; grouped = defaultdict(list)
    for fragment in plan['records']: grouped[fragment['texture_id']].append(fragment)
    for i, item in enumerate(plan['textures']):
        if item['id'] != i or item['source_group'] not in (*study.GROUPS, *(['sharedassets0', 'sharedassets4', 'resources'] if encoder else [])) or i not in grouped: raise ValueError('invalid texture identity')
        tex = by_id[(item['source_group'], item['source_id'])]; data = bytes(tex.get_image_data()); stream = tex.m_StreamData
        if (tex.m_Width, tex.m_Height, tex.m_MipCount, int(tex.m_TextureFormat), sha(data)) != (item['width'], item['height'], item['mips'], item['source_format'], item['source_sha256']): raise ValueError('Steam texture differs')
        name, offset, size = stream.path, stream.offset, stream.size
        if not objects.SOURCE.fullmatch(name) or not name.endswith('.assets.resS') or offset < 0 or size != len(data): raise ValueError('unsupported Steam stream')
        path = source / name
        if path.is_symlink() or not path.is_file(): raise ValueError('invalid Steam stream')
        with path.open('rb') as f:
            f.seek(offset)
            if f.read(size) != data: raise ValueError('Steam stream differs')
        target_format = item.get('target_format', 4)
        quality = quality_mode(item)
        output = converted_plan_texture(tex, item, encoder)
        if sha(output) != item['converted_sha256']: raise ValueError('converted texture differs')
        used[name] = masker.file_digest(path)
        fragments = [{k: value for k, value in row.items() if k != 'texture_id'} for row in grouped[i]]
        record = {'id': i, 'source': name, 'source_offset': offset, 'source_size': size, 'source_sha256': sha(data),
                        'width': item['width'], 'height': item['height'], 'mips': item['mips'], 'source_format': item['source_format'],
                        'target_format': target_format, 'quality': quality,
                        'target_size': len(output), 'target_sha256': sha(output), 'fragments': fragments}
        if normal_rgba_mode(item): record['normal_rgba'] = True
        if 'blend' in item: record['blend'] = item['blend']
        if 'blend_unit' in item: record['blend_unit'] = item['blend_unit']
        if 'blend_rgba' in item: record['blend_rgba'] = item['blend_rgba']
        records.append(record)
    if not records or len(grouped) != len(records): raise ValueError('invalid fragment mapping')
    result = {'schema': 1, 'kind': 'experimental-partial-rgba-texture-offsets', 'template_sha256': plan['template_sha256'], 'sources': used, 'records': records}
    with out.open('x', encoding='utf-8') as f: json.dump(result, f, separators=(',', ':')); f.write('\n')
    print('Exported local Android offset plan; Android restoration remains unverified.')


def main():
    p = argparse.ArgumentParser(description=__doc__); sub = p.add_subparsers(dest='action', required=True)
    g = sub.add_parser('generate'); g.add_argument('--steam-data', type=Path, required=True); g.add_argument('--obb', type=Path, required=True); g.add_argument('--output', type=Path, required=True)
    g.add_argument('--include-small-etc2', action='store_true', help='also attempt partial matching for smaller ETC2 textures (experimental)')
    g.add_argument('--include-tiny-etc2', action='store_true', help='also match exact bytes in small uniquely sourced ETC2 textures')
    g.add_argument('--include-etc2-aliases', action='store_true', help='also match unambiguous small ETC2 aliases by encoded bytes')
    g.add_argument('--normal-rgba', action='store_true', help='match exact bytes in the unpacked WaterNormal RGBA texture')
    g.add_argument('--rgba-etc2', action='store_true', help='also match exact ETC2 bytes using the RGBA error metric; requires --byte-etc2')
    g.add_argument('--fast-etc2', action='store_true', help='experimentally select the fast ETC2 encoder when it recovers more exact bytes')
    g.add_argument('--blend-etc2', action='store_true', help='experimentally choose an encoder per 16-byte output block; requires --fast-etc2')
    g.add_argument('--byte-etc2', action='store_true', help='experimentally choose an encoder per output byte; requires --blend-etc2')
    r = sub.add_parser('restore'); r.add_argument('--steam-data', type=Path, required=True); r.add_argument('--template', type=Path, required=True); r.add_argument('--plan', type=Path, required=True); r.add_argument('--output', type=Path, required=True)
    e = sub.add_parser('export-android'); e.add_argument('--steam-data', type=Path, required=True); e.add_argument('--template', type=Path, required=True); e.add_argument('--plan', type=Path, required=True); e.add_argument('--output', type=Path, required=True)
    for parser in (g, r, e): parser.add_argument('--etc2-quality-library', type=Path, help='experimentally match exact ETC2 bytes with a local quality encoder')
    a = p.parse_args()
    if a.action == 'generate' and a.include_small_etc2 and not a.etc2_quality_library:
        p.error('--include-small-etc2 requires --etc2-quality-library')
    if a.action == 'generate' and a.include_tiny_etc2 and (not a.include_small_etc2 or not a.byte_etc2):
        p.error('--include-tiny-etc2 requires --include-small-etc2 and --byte-etc2')
    if a.action == 'generate' and a.include_etc2_aliases and not a.include_tiny_etc2:
        p.error('--include-etc2-aliases requires --include-tiny-etc2')
    if a.action == 'generate' and a.rgba_etc2 and not a.byte_etc2:
        p.error('--rgba-etc2 requires --byte-etc2')
    if a.action == 'generate' and a.fast_etc2 and not a.etc2_quality_library:
        p.error('--fast-etc2 requires --etc2-quality-library')
    if a.action == 'generate' and a.blend_etc2 and not a.fast_etc2:
        p.error('--blend-etc2 requires --fast-etc2')
    if a.action == 'generate' and a.byte_etc2 and not a.blend_etc2:
        p.error('--byte-etc2 requires --blend-etc2')
    {'generate': generate, 'restore': restore, 'export-android': export_android}[a.action](a)

if __name__ == '__main__': main()
