#!/usr/bin/env python3
"""Zero unused interior RGB in two locally reconstructed transparent atlases.

Requires matching visible pixels and alpha from a unique local Steam texture.
This is experimental: shaders can read RGB independently of alpha, and mip
levels and real device rendering still require review. Output stays private.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('partial', ROOT / 'tools/experimental-partial-rgba-plan.py')
partial = importlib.util.module_from_spec(spec)
spec.loader.exec_module(partial)
TARGETS = frozenset(('bakedtexture 1', 't_pig'))


def scrub(base, source, width, height):
    """Keep all alpha, visible pixels, transparent borders and lower mips."""
    size = width * height * 4
    if width < 3 or height < 3 or len(base) != len(source) or len(base) < size:
        raise ValueError('invalid RGBA atlas')
    for i in range(0, size, 4):
        if base[i + 3] != source[i + 3] or (base[i + 3] and base[i:i + 3] != source[i:i + 3]):
            raise ValueError('Steam visible pixels or alpha differ')
    result = bytearray(base)
    for y in range(1, height - 1):
        for x in range(1, width - 1):
            i = (y * width + x) * 4
            if any(base[((y + dy) * width + x + dx) * 4 + 3]
                   for dy in (-1, 0, 1) for dx in (-1, 0, 1)):
                continue
            result[i:i + 3] = bytes(3)
    return bytes(result)


def generate(args):
    source = args.steam_data.resolve(strict=True)
    obb = args.obb.resolve(strict=True)
    output = partial.masker.regular_output(args.output, (source, obb))
    index, _ = partial.sources(source)
    changes = {}; records = []
    with zipfile.ZipFile(obb) as archive:
        infos = partial.masker.zip_entries(archive)
        groups = partial.objects.asset_groups(archive)
        parts = groups['sharedassets1.assets']
        raw = b''.join(archive.read(part) for part in parts)
        modified = bytearray(raw)
        for obj in UnityPy.load(raw).objects:
            if obj.type.name != 'Texture2D': continue
            target = obj.read()
            if target.m_Name not in TARGETS: continue
            if (target.m_Width, target.m_Height, target.m_ImageCount, int(target.m_TextureFormat)) != (256, 256, 1, 4):
                raise ValueError('unexpected atlas metadata')
            item = partial.study.select_source(target, index[(target.m_Name, target.m_Width, target.m_Height)], texture=lambda x: x[2])
            if item is None: raise ValueError('Steam atlas missing or ambiguous')
            payload = bytes(target.image_data); body = obj.get_raw_data()
            if not payload or body.count(payload) != 1: raise ValueError('ambiguous atlas payload')
            steam = partial.converted(item[2], target.m_Width, target.m_Height, target.m_MipCount)
            result = scrub(payload, steam, target.m_Width, target.m_Height)
            offset = obj.byte_start + body.index(payload)
            if raw[offset:offset + len(payload)] != payload: raise ValueError('invalid atlas range')
            modified[offset:offset + len(payload)] = result
            records.append({'name': target.m_Name, 'path_id': obj.path_id,
                            'zeroed_nonzero_bytes': sum(bool(a) and not b for a, b in zip(payload, result))})
        if len(records) != len(TARGETS) or {x['name'] for x in records} != TARGETS:
            raise ValueError('expected atlases missing or duplicated')
        offset = 0
        for part in parts:
            changes[part.filename] = bytes(modified[offset:offset + part.file_size]); offset += part.file_size
        output.mkdir(mode=0o700)
        with zipfile.ZipFile(output / 'converted.obb', 'x') as dest:
            for info in infos:
                dest.writestr(info, changes[info.filename] if info.filename in changes else archive.read(info))
    (output / 'report.json').write_text(json.dumps({'schema': 1, 'textures': records}, indent=2) + '\n')
    print('Zeroed', sum(x['zeroed_nonzero_bytes'] for x in records), 'interior transparent RGB bytes; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--steam-data', required=True, type=Path)
    p.add_argument('--obb', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    generate(p.parse_args())


if __name__ == '__main__': main()
