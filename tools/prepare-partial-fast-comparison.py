#!/usr/bin/env python3
"""Prepare private fast-conversion fixtures for the Android texture checker.

The template, Steam input and output contain game data. Keep them private.
This compares encoder output, not visual quality or release readiness.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('partial_fast_plan', ROOT / 'tools/experimental-partial-rgba-plan.py')
partial = importlib.util.module_from_spec(spec)
spec.loader.exec_module(partial)


def write_fixtures(path, records):
    if not records or len(records) > 50000:
        raise ValueError('invalid fixture count')
    created = False
    try:
        with path.open('xb') as out:
            created = True
            out.write(b'OBTX0001' + struct.pack('>I', len(records)))
            for width, height, source, target, data, converted in records:
                if not data or len(data) > 64 * 1024 * 1024 or not converted:
                    raise ValueError('invalid fixture data')
                out.write(struct.pack('>5I', width, height, source, target, len(data)))
                out.write(hashlib.sha256(converted).digest())
                out.write(data)
    except BaseException:
        if created: path.unlink(missing_ok=True)
        raise


def main():
    import UnityPy
    from UnityPy.export import Texture2DConverter as converter
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'template', 'quality-library', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--name', action='append', required=True, help='target texture name; repeat for additional names')
    args = p.parse_args()
    source, template, library = (x.expanduser().resolve(strict=True) for x in (args.steam_data, args.template, args.quality_library))
    out = partial.objects.new_output(args.output, (source, template, library))
    names = set(args.name)
    if len(names) != len(args.name) or any(not name for name in names): p.error('texture names must be unique and nonempty')
    index, _ = partial.sources(source, True)
    encoder = partial.study.load_quality_encoder(library)
    records, found = [], set()
    with zipfile.ZipFile(template) as archive:
        for group, parts in partial.objects.asset_groups(archive).items():
            if any(not partial.ENTRY.fullmatch(part.filename) for part in parts): continue
            for obj in UnityPy.load(b''.join(archive.read(part) for part in parts)).objects:
                if obj.type.name != 'Texture2D': continue
                try: target = obj.read()
                except (ValueError, TypeError, IndexError): continue
                if target.m_Name not in names or int(target.m_TextureFormat) not in (45, 47): continue
                if target.m_Name in found: raise ValueError('ambiguous target name: ' + target.m_Name)
                candidates = partial.candidate_sources(index, target, group, target.m_Name)
                item = partial.study.select_source(target, candidates, texture=lambda x: x[2])
                if item is None:
                    alias = target.m_Name[:-2] if target.m_Name.endswith('_0') else target.m_Name
                    match = partial.partial_etc2_source(target, bytes(target.image_data),
                        partial.candidate_sources(index, target, group, alias), encoder)
                    if match: item = match[0]
                if item is None: raise ValueError('no unambiguous source: ' + target.m_Name)
                src = item[2]
                for width, height, data in partial.study.mip_slices(bytes(src.get_image_data()), int(src.m_TextureFormat), src.m_Width, src.m_Height, src.m_MipCount):
                    image = converter.parse_image_data(data, width, height, src.m_TextureFormat, src.object_reader.version, src.object_reader.platform)
                    encoded, fmt = converter.image_to_texture2d(image, target.m_TextureFormat, target.object_reader.platform, target.m_PlatformBlob)
                    if int(fmt) != int(target.m_TextureFormat) or len(encoded) != partial.study.mip_size(fmt, width, height):
                        raise ValueError('unexpected conversion: ' + target.m_Name)
                    records.append((width, height, int(src.m_TextureFormat), int(fmt), data, encoded))
                found.add(target.m_Name)
    if found != names: raise ValueError('missing target names: ' + ', '.join(sorted(names - found)))
    write_fixtures(out, records)
    print(f'Prepared {len(records)} private mip fixtures for {len(found)} textures; do not share them.')


if __name__ == '__main__': main()
