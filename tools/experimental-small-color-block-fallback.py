#!/usr/bin/env python3
"""Study Steam-derived small ETC2 color blocks under bounded visible error.

This opt-in experiment uses uniquely named, metadata-compatible Steam sources.
Only independent 4x4 blocks passing the existing color/alpha gate are replaced;
other bytes remain untouched. The result still contains game data and is private.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('partial_small_colors', ROOT / 'tools/experimental-partial-rgba-plan.py')
partial = importlib.util.module_from_spec(spec); spec.loader.exec_module(partial)
GROUPS = ('sharedassets1.assets', 'sharedassets2.assets', 'sharedassets4.assets')
EXCLUDED = ('normal', 'bump', 'height', 'metallic', 'roughness', 'specular', 'lightmap', 'mask')
LIGHTMAPS = {'LDR_LLL1_1_0': 'LDR_LLL1_1', 'LDR_LLL1_2_0': 'LDR_LLL1_2'}


def eligible_target(target, payload):
    name = target.m_Name
    return (isinstance(name, str) and name and not name.lower().startswith(('ldr_', 'noto', 'alagard')) and
            not any(word in name.lower() for word in EXCLUDED) and
            int(target.m_TextureFormat) in (45, 47) and target.m_ImageCount == 1 and
            64 <= len(payload) <= 4096 and len(payload) - payload.count(0) >= 16 and
            not (getattr(target, 'm_StreamData', None) and
                 (getattr(target.m_StreamData, 'path', '') or getattr(target.m_StreamData, 'size', 0))))


def select_alias_source(target, payload, index, group, encoder):
    """Mirror partial-plan source selection and refuse renamed ambiguous data."""
    item = partial.study.select_source(target, partial.candidate_sources(index, target, group, target.m_Name), texture=lambda x: x[2])
    if item is not None: return item
    if target.m_Name.endswith('_0'):
        alias = partial.etc2_alpha_alias(target, payload, partial.candidate_sources(index, target, group, target.m_Name[:-2]), encoder)
        if alias: return alias[0]
    source_name = target.m_Name[:-2] if target.m_Name.endswith('_0') else target.m_Name
    match = partial.partial_etc2_source(target, payload, partial.candidate_sources(index, target, group, source_name), encoder)
    if match: return match[0]
    match = partial.scored_etc2_alias(target, payload, index, group, encoder)
    return match[0] if match else None


def stable_alias_source(target, original, replacement, index, group, encoder):
    """Require the same Steam object before and after bounded conversion."""
    before = select_alias_source(target, original, index, group, encoder)
    after = select_alias_source(target, replacement, index, group, encoder)
    if before is None or after is None or before[:2] != after[:2]: return None
    return before


def replace_mip(old, outputs, images, level, block_size, lightmap=False):
    """Substitute only bounded blocks which still contain unmatchable bytes."""
    from PIL import ImageChops, ImageStat
    if (block_size not in (8, 16) or level < 0 or not outputs or len(images) != len(outputs) + 1):
        raise ValueError('invalid small color variants')
    width, height = images[0].size
    size = ((width + 3) // 4) * ((height + 3) // 4) * block_size
    if (width < 1 or height < 1 or len(old) != size or
            any(len(payload) != size for payload in outputs) or
            any(image.size != (width, height) for image in images)):
        raise ValueError('invalid small color mip')
    result = bytearray(old); changed = selected = 0; offset = 0
    for y in range(0, height, 4):
        for x in range(0, width, 4):
            original = old[offset:offset + block_size]
            choices = [data[offset:offset + block_size] for data in outputs]
            # If every original byte can already be recovered from one of the
            # encoders, preserve it exactly; this does not change its look.
            if any(value and all(value != block[i] for block in choices) for i, value in enumerate(original)):
                box = (x, y, min(x + 4, width), min(y + 4, height))
                reference = images[0].crop(box)
                safe = []
                for candidate, image in zip(choices, images[1:]):
                    crop = image.crop(box)
                    acceptable = (partial.study.acceptable_lightmap_image(reference, crop) if lightmap else
                                  partial.study.acceptable_bounded_mip(reference, crop, level))
                    if acceptable:
                        # Prefer the visually closest bounded candidate; ties
                        # are stable in encoder order.
                        difference = ImageStat.Stat(ImageChops.difference(reference, crop)).sum
                        safe.append((sum(difference), candidate))
                if safe:
                    replacement = min(safe, key=lambda x: x[0])[1]
                    result[offset:offset + block_size] = replacement
                    changed += sum(a != b for a, b in zip(original, replacement)); selected += 1
            offset += block_size
    return bytes(result), selected, changed


def generate(args):
    import UnityPy
    from UnityPy.export import Texture2DConverter as converter
    steam = args.steam_data.resolve(strict=True); obb = args.obb.resolve(strict=True)
    output = partial.masker.regular_output(args.output, (steam, obb))
    encoder = partial.study.load_quality_encoder(args.quality_library)
    encoders = [encoder, encoder.rgba_variant(), *(encoder.extra_variant(i) for i in (1, 2, 3)), None]
    index, _ = partial.sources(steam, True)
    changes, records, seen_lightmaps = {}, [], set()
    with zipfile.ZipFile(obb) as archive:
        infos = partial.masker.zip_entries(archive); groups = partial.objects.asset_groups(archive)
        for group in GROUPS:
            if group not in groups: continue
            parts = groups[group]; raw = b''.join(archive.read(part) for part in parts); modified = bytearray(raw)
            for obj in UnityPy.load(raw).objects:
                if obj.type.name != 'Texture2D': continue
                target = obj.read(); old = bytes(target.image_data)
                lightmap = bool(getattr(args, 'lightmaps', False) and group == 'sharedassets1.assets' and target.m_Name in LIGHTMAPS)
                if lightmap:
                    if target.m_Name in seen_lightmaps: raise ValueError('duplicate lightmap target')
                    seen_lightmaps.add(target.m_Name)
                    if (target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount,
                            int(target.m_TextureFormat), len(old)) != (64, 64, 1, 1, 47, 4096):
                        raise ValueError('unexpected lightmap metadata')
                elif not eligible_target(target, old): continue
                source = partial.study.select_source(target, partial.candidate_sources(index, target, group, target.m_Name), texture=lambda item: item[2])
                is_alias = source is None
                if is_alias and (getattr(args, 'aliases', False) or lightmap):
                    source = select_alias_source(target, old, index, group, encoder)
                if source is None or int(source[2].m_TextureFormat) not in (1, 3, 4, 10, 12, 25): continue
                if lightmap and (source[0] != 'sharedassets1' or source[2].m_Name != LIGHTMAPS[target.m_Name] or
                                 int(source[2].m_TextureFormat) != 1): continue
                body = obj.get_raw_data()
                if body.count(old) != 1: continue
                fmt = int(target.m_TextureFormat)
                try:
                    candidates = [partial.converted(source[2], target.m_Width, target.m_Height, target.m_MipCount,
                                                    fmt, variant, variant is None) for variant in encoders]
                    mipsets = [partial.study.mip_slices(data, fmt, target.m_Width, target.m_Height, target.m_MipCount)
                               for data in (old, *candidates)]
                    chunks = []; selected = changed = 0
                    for level, mips in enumerate(zip(*mipsets)):
                        w, h = mips[0][:2]
                        images = [converter.parse_image_data(mip[2], w, h, fmt, target.object_reader.version,
                                                            target.object_reader.platform).convert('RGBA') for mip in mips]
                        chunk, n, c = replace_mip(mips[0][2], [m[2] for m in mips[1:]], images, level, 8 if fmt == 45 else 16, lightmap=lightmap)
                        chunks.append(chunk); selected += n; changed += c
                except (ValueError, TypeError, IndexError, OSError, NotImplementedError, RuntimeError):
                    continue
                if not changed: continue
                replacement = b''.join(chunks); offset = obj.byte_start + body.index(old)
                if not partial.eligible_etc2_payload(replacement, True, True): continue
                if is_alias and stable_alias_source(target, old, replacement, index, group, encoder) is None: continue
                if len(replacement) != len(old) or raw[offset:offset + len(old)] != old or modified[offset:offset + len(old)] != old:
                    raise ValueError('small color texture range differs or overlaps')
                modified[offset:offset + len(old)] = replacement
                records.append({'group': group, 'path_id': obj.path_id, 'name': target.m_Name,
                                'selected_blocks': selected, 'changed_bytes': changed})
            offset = 0
            for part in parts:
                changes[part.filename] = bytes(modified[offset:offset + part.file_size]); offset += part.file_size
        if getattr(args, 'lightmaps', False) and seen_lightmaps != set(LIGHTMAPS):
            raise ValueError('expected lightmap targets missing')
        output.mkdir(mode=0o700)
        try:
            with zipfile.ZipFile(output / 'converted.obb', 'x') as dest:
                for info in infos:
                    if info.filename in changes: dest.writestr(info, changes[info.filename])
                    else:
                        with archive.open(info) as inp, dest.open(info, 'w') as out: shutil.copyfileobj(inp, out, 1024 * 1024)
            (output / 'report.json').write_text(json.dumps({'schema': 1, 'textures': records}, indent=2) + '\n')
        except BaseException:
            shutil.rmtree(output); raise
    print('Bounded small-color textures:', len(records), 'changed bytes:', sum(x['changed_bytes'] for x in records), 'output remains private.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--obb', required=True, type=Path)
    parser.add_argument('--quality-library', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--aliases', action='store_true', help='allow unambiguous matched names, rechecking the selected source after conversion')
    parser.add_argument('--lightmaps', action='store_true', help='also process two specified LDR lightmaps with bounded alpha error')
    generate(parser.parse_args())


if __name__ == '__main__': main()
