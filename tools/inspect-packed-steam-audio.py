#!/usr/bin/env python3
"""Count packed Android audio clips that can be matched to local Steam clips.

This read-only research tool emits aggregate counts, not game data. Matching
metadata and FSB codecs does not prove audible equivalence or create a build.
Requires UnityPy and the exact local inputs being investigated.
"""
import argparse
from collections import Counter, defaultdict
import struct
from pathlib import Path
import zipfile

FIELDS = ('m_Length', 'm_Channels', 'm_Frequency', 'm_BitsPerSample', 'm_LoadType',
          'm_CompressionFormat', 'm_SubsoundIndex')
GROUPS = ('sharedassets1', 'sharedassets2')
PREFIX = 'assets/bin/Data/'


def compatible(target, candidate):
    return all(getattr(target, field, None) == getattr(candidate, field, None) for field in FIELDS)


def fsb_codec(data):
    if len(data) < 60 or data[:4] != b'FSB5':
        raise ValueError('not a supported FSB5 clip')
    version, samples, header, names, audio, mode = struct.unpack_from('<6I', data, 4)
    if version != 1 or samples != 1 or not header or 60 + header + names + audio > len(data):
        raise ValueError('unsupported FSB5 layout')
    return mode


def split_data(archive, group):
    prefix = PREFIX + group + '.assets.split'
    selected = []
    for name in archive.namelist():
        if name.startswith(prefix):
            suffix = name[len(prefix):]
            if not suffix.isdecimal() or str(int(suffix)) != suffix:
                raise ValueError('invalid split name')
            selected.append((int(suffix), name))
    selected.sort()
    if not selected or [n for n, _ in selected] != list(range(len(selected))):
        raise ValueError('missing or duplicate split')
    return b''.join(archive.read(name) for _, name in selected)


def inspect(obb, steam_data):
    import UnityPy
    index = defaultdict(list)
    for group in GROUPS:
        source_asset = steam_data / (group + '.assets')
        source_resource = steam_data / (group + '.resource')
        if any(not p.is_file() or p.is_symlink() for p in (source_asset, source_resource)):
            raise ValueError('missing regular Steam asset or resource file')
        for obj in UnityPy.load(str(source_asset)).objects:
            if obj.type.name == 'AudioClip':
                clip = obj.read()
                index[clip.m_Name].append(clip)
    if not index:
        raise ValueError('no Steam AudioClips found')
    with zipfile.ZipFile(obb) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError('duplicate OBB entry')
        for group in GROUPS:
            data = split_data(archive, group)
            resource = archive.read(PREFIX + group + '.resource')
            counts, sizes = Counter(), Counter()
            for obj in UnityPy.load(data).objects:
                if obj.type.name != 'AudioClip':
                    continue
                clip = obj.read()
                dst = clip.m_Resource
                if not dst.m_Source or not dst.m_Size:
                    continue
                matches = [candidate for candidate in index[clip.m_Name] if compatible(clip, candidate)]
                outcome = 'ambiguous' if len(matches) > 1 else 'no_match' if not matches else None
                if outcome is None:
                    src = matches[0].m_Resource
                    source_name = Path(src.m_Source)
                    if (source_name.name != str(source_name) or source_name.name not in
                            {g + '.resource' for g in GROUPS} or src.m_Offset < 0 or src.m_Size <= 0):
                        raise ValueError('unexpected Steam resource reference')
                    with (steam_data / source_name).open('rb') as stream:
                        stream.seek(src.m_Offset)
                        source_bytes = stream.read(src.m_Size)
                    body = obj.get_raw_data()
                    if (dst.m_Source.rsplit('/', 1)[-1] != group + '.resource' or dst.m_Offset < 0 or
                            dst.m_Offset + dst.m_Size > len(resource) or len(source_bytes) != src.m_Size):
                        raise ValueError('invalid clip resource bounds')
                    target_bytes = resource[dst.m_Offset:dst.m_Offset + dst.m_Size]
                    if len(body) < 20 or struct.unpack_from('<QQ', body, len(body) - 20) != (dst.m_Offset, dst.m_Size):
                        outcome = 'unsupported_object_layout'
                    else:
                        try:
                            outcome = 'compatible' if fsb_codec(source_bytes) == fsb_codec(target_bytes) else 'codec_mismatch'
                        except ValueError:
                            outcome = 'unsupported_fsb'
                counts[outcome] += 1
                sizes[outcome] += dst.m_Size
            print(group)
            for key in sorted(counts):
                print(f'  {key}: {counts[key]} clips, {sizes[key]} Android bytes')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--obb', required=True, type=Path)
    parser.add_argument('--steam-data', required=True, type=Path)
    args = parser.parse_args()
    inspect(args.obb.resolve(strict=True), args.steam_data.resolve(strict=True))


if __name__ == '__main__':
    main()
