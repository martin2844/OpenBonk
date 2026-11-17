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
           't_ghost_purple_0': 't_ghost_purple', 't_ghost_black_0': 't_ghost_black'}


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
    index, _ = partial.sources(steam, True)
    targets = dict(TARGETS)
    if getattr(args, 'emission_alias', False):
        targets[('sharedassets1.assets', 'Emission_1')] = (128, 128, 8, 1, 45)
    changes, records, seen = {}, [], set()
    with zipfile.ZipFile(obb) as archive:
        infos = partial.masker.zip_entries(archive)
        groups = partial.objects.asset_groups(archive)
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
                mipsets = [partial.study.mip_slices(data, fmt, target.m_Width, target.m_Height, target.m_MipCount)
                           for data in (old, fast, quality)]
                chunks, safe, unsafe, changed = [], 0, 0, 0
                for level, mips in enumerate(zip(*mipsets)):
                    w, h = mips[0][:2]
                    images = [converter.parse_image_data(mip[2], w, h, fmt, target.object_reader.version,
                                                         target.object_reader.platform).convert('RGBA') for mip in mips]
                    chunk, s, u, c = normal.blend_mip(*(mip[2] for mip in mips), *images,
                                                     lambda a, b: acceptable_color(a, b, level),
                                                     8 if fmt == 45 else 16)
                    chunks.append(chunk); safe += s; unsafe += u; changed += c
                replacement = b''.join(chunks)
                offset = obj.byte_start + body.index(old)
                if len(replacement) != len(old) or raw[offset:offset+len(old)] != old or modified[offset:offset+len(old)] != old:
                    raise ValueError('texture range differs or overlaps')
                modified[offset:offset+len(old)] = replacement
                records.append({'name': target.m_Name, 'path_id': obj.path_id, 'format': fmt,
                                'safe_blocks': safe, 'unchanged_unsafe_blocks': unsafe, 'changed_bytes': changed})
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
    generate(parser.parse_args())


if __name__ == '__main__': main()
