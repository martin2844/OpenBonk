#!/usr/bin/env python3
"""Study bounded ETC2 color blocks using uniquely matched local Steam textures.

This is a private experiment, not a distribution build. Each replaced 4x4
block must pass conservative decoded color and alpha checks at its mip level.
Blocks which do not pass are preserved byte-for-byte.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import re
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('normal_blocks', ROOT / 'tools/experimental-normal-block-fallback.py')
normal = importlib.util.module_from_spec(spec); spec.loader.exec_module(normal)
partial = normal.partial

# Unity can assign different path IDs across otherwise equivalent exports.
TARGETS = {('sharedassets1.assets', 'FBX_Barrel_Color'): (128, 128, 8, 1, 45),
           ('sharedassets1.assets', 't_birdo_blue'): (128, 128, 8, 1, 45),
           ('sharedassets1.assets', 'FBX_Barrel_Color_0'): (128, 128, 8, 1, 45),
           ('sharedassets1.assets', 't_ghost_red_0'): (128, 128, 8, 1, 45),
           ('sharedassets1.assets', 't_ghost_purple_0'): (128, 128, 8, 1, 45),
           ('sharedassets1.assets', 't_ghost_black_0'): (128, 128, 8, 1, 45),
           ('sharedassets4.assets', 'B_Pylon_Color'): (128, 128, 8, 1, 45),
           ('sharedassets4.assets', 'B_Pylon_Emission'): (128, 128, 8, 1, 45)}
ALIASES = {'FBX_Barrel_Color_0': 'FBX_Barrel_Color', 't_ghost_red_0': 't_ghost_red',
           't_ghost_purple_0': 't_ghost_purple', 't_ghost_black_0': 't_ghost_black',
           'Bullseye_0': 'Bullseye', 'Emission_2': 'Emission'}
PRECISE_TARGETS = frozenset((('sharedassets1.assets', 't_birdo_blue'),
                             ('sharedassets1.assets', 't_ghost_red_0'),
                             ('sharedassets1.assets', 't_ghost_purple_0'),
                             ('sharedassets1.assets', 't_ghost_black_0'),
                             ('sharedassets4.assets', 'B_Pylon_Color')))


def discover_bullseye(archive, groups):
    """Find the unique hashed texture group; its export ID can change."""
    import UnityPy
    found = []
    for group, parts in groups.items():
        if not re.fullmatch(r'[0-9a-f]{32}', group): continue
        raw = b''.join(archive.read(part) for part in parts)
        for obj in UnityPy.load(raw).objects:
            if obj.type.name != 'Texture2D': continue
            target = obj.read()
            if target.m_Name != 'Bullseye_0': continue
            metadata = (target.m_Width, target.m_Height, target.m_MipCount,
                        target.m_ImageCount, int(target.m_TextureFormat))
            if metadata != (128, 128, 1, 1, 47): raise ValueError('unexpected bullseye metadata')
            found.append((group, target.m_Name))
    if len(found) != 1: raise ValueError('bullseye target missing or ambiguous')
    return found[0]


def identify_target(group, target, seen, targets=TARGETS):
    key = (group, target.m_Name)
    if key not in targets: return None
    if key in seen: raise ValueError('duplicate color target')
    actual = (target.m_Width, target.m_Height, target.m_MipCount,
              target.m_ImageCount, int(target.m_TextureFormat))
    if actual != targets[key]: raise ValueError('unexpected target metadata')
    return key


def acceptable_color(reference, candidate, level):
    return (partial.study.acceptable_quality_image(reference, candidate, 1) and
            partial.study.acceptable_bounded_mip(reference, candidate, level))


def acceptable_precise(reference, candidate, level):
    """Bound each visible channel to <=1.5 mean and <=16 maximum per block."""
    from PIL import Image, ImageChops, ImageStat
    if not partial.study.acceptable_bounded_mip(reference, candidate, level): return False
    a, b = reference.convert('RGBA'), candidate.convert('RGBA')
    for shade in (0, 255):
        background = Image.new('RGBA', a.size, (shade, shade, shade, 255))
        diff = ImageChops.difference(Image.alpha_composite(background, a).convert('RGB'),
                                     Image.alpha_composite(background, b).convert('RGB'))
        if max(ImageStat.Stat(diff).mean) > 1.5 or max(high for _, high in diff.getextrema()) > 16:
            return False
    return True


def acceptable_block(reference, candidate, level, barrel_bounded=False, precise=False):
    if barrel_bounded:
        return partial.study.acceptable_bounded_mip(reference, candidate, level)
    if precise:
        return acceptable_color(reference, candidate, level) or acceptable_precise(reference, candidate, level)
    return acceptable_color(reference, candidate, level)


def use_bounded_barrel(enabled, key, level):
    return bool(enabled and 0 <= level < 4 and key in (('sharedassets1.assets', 'FBX_Barrel_Color'),
                                                       ('sharedassets1.assets', 'FBX_Barrel_Color_0')))


def use_precise_color(enabled, key, level):
    return bool(enabled and 0 <= level < 4 and key in PRECISE_TARGETS)


def bound_precise_increment(baseline, candidate, baseline_image, candidate_image, level, block_bytes):
    """Retain prior blocks whenever the incremental visual change exceeds bounds."""
    width, height = baseline_image.size
    size = ((width + 3) // 4) * ((height + 3) // 4) * block_bytes
    if (block_bytes not in (8, 16) or width < 1 or height < 1 or candidate_image.size != (width, height) or
            len(baseline) != size or len(candidate) != size):
        raise ValueError('invalid precise color increment')
    result = bytearray(baseline); rejected = offset = 0
    for y in range(0, height, 4):
        for x in range(0, width, 4):
            value = candidate[offset:offset + block_bytes]
            if value != baseline[offset:offset + block_bytes]:
                box = (x, y, min(x + 4, width), min(y + 4, height))
                if acceptable_precise(baseline_image.crop(box), candidate_image.crop(box), level):
                    result[offset:offset + block_bytes] = value
                else:
                    rejected += 1
            offset += block_bytes
    return bytes(result), rejected


def fill_safe_extra_blocks(old, selected, fast, quality, original_image, variants, acceptable, block_bytes):
    """Try more encoders only where both existing choices left the block unchanged."""
    width, height = original_image.size
    expected = ((width + 3) // 4) * ((height + 3) // 4) * block_bytes
    if (block_bytes not in (8, 16) or width < 1 or height < 1 or
            any(len(x) != expected for x in (old, selected, fast, quality)) or
            any(len(payload) != expected or image.size != (width, height) for payload, image in variants)):
        raise ValueError('invalid additional color blocks')
    output = bytearray(selected); pos = added = changed = 0
    for y in range(0, height, 4):
        for x in range(0, width, 4):
            original = old[pos:pos + block_bytes]
            if selected[pos:pos + block_bytes] == original and original not in (fast[pos:pos + block_bytes], quality[pos:pos + block_bytes]):
                box = (x, y, min(x + 4, width), min(y + 4, height)); reference = original_image.crop(box)
                candidates = [payload[pos:pos + block_bytes] for payload, image in variants if acceptable(reference, image.crop(box))]
                if candidates:
                    chosen = max(candidates, key=lambda block: sum(a == b for a, b in zip(original, block)))
                    output[pos:pos + block_bytes] = chosen; added += 1
                    changed += sum(a != b for a, b in zip(original, chosen))
            pos += block_bytes
    return bytes(output), added, changed


def select_source(target, index, group, encoder):
    if target.m_Name == 'Emission_1':
        candidates = partial.candidate_sources(index, target, group, 'Emission')
        matches = []
        for item in candidates:
            src = item[2]
            if (int(src.m_TextureFormat) not in (1, 3, 4, 10, 12, 25) or
                    (src.m_MipCount, src.m_ImageCount, src.m_ColorSpace) !=
                    (target.m_MipCount, target.m_ImageCount, target.m_ColorSpace)):
                continue
            try:
                if partial.study.acceptable_bounded_mip(target.image, src.image, 0): matches.append(item)
            except (ValueError, OSError, TypeError, NotImplementedError, RuntimeError): continue
        return matches[0] if len(matches) == 1 else None
    source_name = ALIASES.get(target.m_Name, target.m_Name)
    candidates = partial.candidate_sources(index, target, group, source_name)
    # Renamed targets need an explicit encoded-byte match, even if their
    # metadata alone appears unique.
    item = (None if target.m_Name in ALIASES else
            partial.study.select_source(target, candidates, texture=lambda x: x[2]))
    if item is None:
        match = partial.partial_etc2_source(target, bytes(target.image_data), candidates, encoder)
        if match is not None: item = match[0]
    return item


def generate(args):
    import UnityPy
    from UnityPy.export import Texture2DConverter as converter
    steam = args.steam_data.resolve(strict=True)
    obb = args.obb.resolve(strict=True)
    output = partial.masker.regular_output(args.output, (steam, obb))
    encoder = partial.study.load_quality_encoder(args.quality_library)
    extras = [encoder.rgba_variant(), *(encoder.extra_variant(i) for i in (1, 2, 3))] if getattr(args, 'extra_etc2', False) else []
    source_checks = None
    if getattr(args, 'barrel_bounded', False) or getattr(args, 'precise_colors', False):
        spec = importlib.util.spec_from_file_location('color_source_checks', ROOT / 'tools/experimental-small-color-block-fallback.py')
        source_checks = importlib.util.module_from_spec(spec); spec.loader.exec_module(source_checks)
    index, _ = partial.sources(steam, True)
    targets = dict(TARGETS)
    if getattr(args, 'emission_alias', False):
        targets[('sharedassets1.assets', 'Emission_1')] = (128, 128, 8, 1, 45)
        targets[('sharedassets0.assets', 'Emission_2')] = (128, 128, 8, 1, 45)
    changes, records, seen = {}, [], set()
    with zipfile.ZipFile(obb) as archive:
        infos = partial.masker.zip_entries(archive)
        groups = partial.objects.asset_groups(archive)
        if getattr(args, 'bullseye_alias', False):
            targets[discover_bullseye(archive, groups)] = (128, 128, 1, 1, 47)
        for group in sorted({key[0] for key in targets}):
            parts = groups[group]
            raw = b''.join(archive.read(part) for part in parts)
            modified = bytearray(raw)
            for obj in UnityPy.load(raw).objects:
                if obj.type.name != 'Texture2D': continue
                target = obj.read()
                key = identify_target(group, target, seen, targets)
                if key is None: continue
                fmt = int(target.m_TextureFormat)
                item = select_source(target, index, group, encoder)
                if item is None or int(item[2].m_TextureFormat) not in (1, 3, 4, 10, 12, 25):
                    raise ValueError('source missing or ambiguous')
                src = item[2]
                old = bytes(target.image_data)
                body = obj.get_raw_data()
                if not old or body.count(old) != 1: raise ValueError('ambiguous texture payload')
                fast = partial.converted(src, target.m_Width, target.m_Height, target.m_MipCount, fmt, encoder, True)
                quality = partial.converted(src, target.m_Width, target.m_Height, target.m_MipCount, fmt, encoder)
                variants = [partial.converted(src, target.m_Width, target.m_Height, target.m_MipCount, fmt, extra) for extra in extras]
                mipsets = [partial.study.mip_slices(data, fmt, target.m_Width, target.m_Height, target.m_MipCount)
                           for data in (old, fast, quality, *variants)]
                chunks, safe, unsafe, changed, rejected_precise = [], 0, 0, 0, 0
                for level, mips in enumerate(zip(*mipsets)):
                    w, h = mips[0][:2]
                    images = [partial.decode_block_image(converter, mip[2], w, h, fmt, target.object_reader.version,
                                                         target.object_reader.platform, 'RGBA') for mip in mips]
                    # Preserve the stricter preexisting gate on the smallest
                    # mips: a 4x4 barrel mip visibly shifted tint under the
                    # broader limits in the local study.
                    is_barrel = use_bounded_barrel(getattr(args, 'barrel_bounded', False), key, level)
                    precise = use_precise_color(getattr(args, 'precise_colors', False), key, level)
                    acceptable = lambda a, b: acceptable_block(a, b, level, is_barrel, precise)
                    chunk, s, u, c = normal.blend_mip(*(mip[2] for mip in mips[:3]), *images[:3],
                                                     acceptable,
                                                     8 if fmt == 45 else 16)
                    if variants:
                        chunk, added, additional_changed = fill_safe_extra_blocks(mips[0][2], chunk, mips[1][2], mips[2][2], images[0],
                            [(mip[2], image) for mip, image in zip(mips[3:], images[3:])], acceptable, 8 if fmt == 45 else 16)
                        s += added; u -= added; c += additional_changed
                    if precise:
                        block_bytes = 8 if fmt == 45 else 16
                        old_gate = lambda a, b: acceptable_color(a, b, level)
                        baseline, _, _, _ = normal.blend_mip(*(mip[2] for mip in mips[:3]), *images[:3], old_gate, block_bytes)
                        if variants:
                            baseline, _, _ = fill_safe_extra_blocks(mips[0][2], baseline, mips[1][2], mips[2][2], images[0],
                                [(mip[2], image) for mip, image in zip(mips[3:], images[3:])], old_gate, block_bytes)
                        base_image = partial.decode_block_image(converter, baseline, w, h, fmt, target.object_reader.version, target.object_reader.platform, 'RGBA')
                        next_image = partial.decode_block_image(converter, chunk, w, h, fmt, target.object_reader.version, target.object_reader.platform, 'RGBA')
                        chunk, rejected = bound_precise_increment(baseline, chunk, base_image, next_image, level, block_bytes)
                        rejected_precise += rejected
                        c = sum(a != b for a, b in zip(mips[0][2], chunk))
                    chunks.append(chunk); safe += s; unsafe += u; changed += c
                replacement = b''.join(chunks)
                check_source = (getattr(args, 'barrel_bounded', False) and key in (('sharedassets1.assets', 'FBX_Barrel_Color'), ('sharedassets1.assets', 'FBX_Barrel_Color_0'))) or (getattr(args, 'precise_colors', False) and key in PRECISE_TARGETS)
                if check_source:
                    stable = source_checks.stable_alias_source(target, old, replacement, index, group, encoder)
                    if stable is None or stable[:2] != item[:2]:
                        seen.add(key)
                        continue
                offset = obj.byte_start + body.index(old)
                if len(replacement) != len(old) or raw[offset:offset+len(old)] != old or modified[offset:offset+len(old)] != old:
                    raise ValueError('texture range differs or overlaps')
                modified[offset:offset+len(old)] = replacement
                records.append({'name': target.m_Name, 'path_id': obj.path_id, 'format': fmt,
                                'safe_blocks': safe, 'unchanged_unsafe_blocks': unsafe, 'changed_bytes': changed,
                                **({'rejected_precise_increments': rejected_precise} if getattr(args, 'precise_colors', False) and key in PRECISE_TARGETS else {})})
                seen.add(key)
            offset = 0
            for part in parts:
                changes[part.filename] = bytes(modified[offset:offset+part.file_size]); offset += part.file_size
        if seen != set(targets): raise ValueError('expected textures missing')
        output.mkdir(mode=0o700)
        try:
            with zipfile.ZipFile(output / 'converted.obb', 'x') as dest:
                for info in infos:
                    if info.filename in changes: dest.writestr(info, changes[info.filename])
                    else:
                        with archive.open(info) as inp, dest.open(info, 'w') as out: shutil.copyfileobj(inp, out, 1024*1024)
            (output / 'report.json').write_text(json.dumps({'schema': 1, 'textures': records}, indent=2) + '\n')
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Bounded color blocks:', sum(x['safe_blocks'] for x in records),
          'unsafe blocks preserved:', sum(x['unchanged_unsafe_blocks'] for x in records), 'output remains private.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--obb', required=True, type=Path)
    parser.add_argument('--quality-library', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--emission-alias', action='store_true', help='also reconstruct bounded blocks of a uniquely matched emission alias')
    parser.add_argument('--bullseye-alias', action='store_true', help='also reconstruct bounded blocks of a uniquely identified bullseye alias')
    parser.add_argument('--extra-etc2', action='store_true', help='try additional encoders for blocks rejected by the existing choices')
    parser.add_argument('--barrel-bounded', action='store_true', help='use bounded visible-pixel limits for the two identified barrel colors')
    parser.add_argument('--precise-colors', action='store_true', help='try strictly bounded alternatives for five identified color textures')
    generate(parser.parse_args())


if __name__ == '__main__': main()
