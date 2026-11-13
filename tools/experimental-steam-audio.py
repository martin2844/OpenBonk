#!/usr/bin/env python3
"""Experimentally substitute local Steam audio into a local Android OBB.

Inputs and output contain game-derived data and must stay local. This is not
a distributable APK or a complete asset conversion. Requires UnityPy.
"""
import argparse
from collections import defaultdict
from pathlib import Path
import re
import struct
import zipfile


ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
FIELDS = ('m_Length', 'm_Channels', 'm_Frequency', 'm_BitsPerSample', 'm_LoadType',
          'm_CompressionFormat', 'm_SubsoundIndex')


def patch_size(raw, object_start, body, old_size, new_size):
    if new_size <= 0 or raw[object_start:object_start + len(body)] != body or len(body) < 12:
        raise ValueError('invalid audio object')
    position = object_start + len(body) - 12
    if struct.unpack_from('<Q', raw, position)[0] != old_size:
        raise ValueError('unexpected audio size field')
    updated = bytearray(raw)
    struct.pack_into('<Q', updated, position, new_size)
    return bytes(updated)


def fsb_format(data):
    if len(data) < 60 or data[:4] != b'FSB5':
        raise ValueError('unsupported audio resource')
    version, count, header_size, names_size, audio_size, mode = struct.unpack_from('<6I', data, 4)
    if version != 1 or count != 1 or not header_size or audio_size > len(data):
        raise ValueError('unsupported FSB layout')
    return mode


def run(args):
    import UnityPy
    source = args.steam_data.resolve(strict=True)
    original = args.obb.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        raise ValueError('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (source, original, ROOT)):
        raise ValueError('output must be outside the source and checkout')
    asset = source / 'sharedassets1.assets'
    resource = source / 'sharedassets1.resource'
    if any(not path.is_file() or path.is_symlink() for path in (asset, resource)):
        raise ValueError('Steam audio inputs must be regular files')
    index = defaultdict(list)
    for obj in UnityPy.load(str(asset)).objects:
        if obj.type.name == 'AudioClip':
            clip = obj.read()
            index[clip.m_Name].append(clip)
    replacements = {}
    matched = 0
    created = False
    try:
        with zipfile.ZipFile(original) as zin, resource.open('rb') as steam:
            infos = zin.infolist()
            names = {info.filename for info in infos}
            if len(infos) != len(names):
                raise ValueError('duplicate ZIP entry')
            for info in infos:
                name = info.filename
                if not re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', name) or name + '.resource' not in names:
                    continue
                raw = zin.read(name)
                objects = [obj for obj in UnityPy.load(raw).objects if obj.type.name == 'AudioClip']
                if len(objects) != 1:
                    raise ValueError('expected one audio clip per resource')
                obj = objects[0]
                target = obj.read()
                candidates = index[target.m_Name]
                if len(candidates) != 1:
                    raise ValueError('Steam clip name is absent or ambiguous')
                clip = candidates[0]
                if any(getattr(clip, key, None) != getattr(target, key, None) for key in FIELDS):
                    raise ValueError('Steam and Android audio metadata differ')
                src, dst = clip.m_Resource, target.m_Resource
                current = zin.read(name + '.resource')
                if (src.m_Source != 'sharedassets1.resource' or src.m_Offset < 0 or src.m_Size <= 0 or
                        dst.m_Offset != 0 or dst.m_Size != len(current) or
                        not dst.m_Source.endswith(name.rsplit('/', 1)[-1] + '.resource')):
                    raise ValueError('unsupported audio resource reference')
                steam.seek(src.m_Offset)
                replacement = steam.read(src.m_Size)
                if len(replacement) != src.m_Size or fsb_format(current) != fsb_format(replacement):
                    raise ValueError('audio resource format differs')
                replacements[name] = patch_size(raw, obj.byte_start, obj.get_raw_data(), len(current), len(replacement))
                replacements[name + '.resource'] = replacement
                matched += 1
            if not matched:
                raise ValueError('no audio resources found')
            with output.open('xb') as stream:
                created = True
                with zipfile.ZipFile(stream, 'w') as zout:
                    for info in infos:
                        zout.writestr(info, replacements[info.filename] if info.filename in replacements else zin.read(info))
    except BaseException:
        if created:
            output.unlink(missing_ok=True)
        raise
    print(f'Replaced {matched} audio resources locally. Output contains game data and requires device validation.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', type=Path, required=True)
    parser.add_argument('--obb', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    run(parser.parse_args())


if __name__ == '__main__':
    main()
