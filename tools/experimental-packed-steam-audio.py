#!/usr/bin/env python3
"""Replace compatible packed Android audio with local Steam clips for research.

Output contains game data, must stay local, and is not a distributable APK.
Requires UnityPy. Unmatched clips and non-audio data remain in the output.
"""
import argparse
from collections import defaultdict
import importlib.util
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('audio_inspect', ROOT / 'tools/inspect-packed-steam-audio.py')
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)


def repack(resource, records):
    """Repack nonoverlapping clips, preserving gaps and returning new offsets."""
    result = bytearray()
    cursor = 0
    offsets = {}
    for key, start, size, replacement in sorted(records, key=lambda row: row[1]):
        if key in offsets or start < cursor or size <= 0 or start + size > len(resource):
            raise ValueError('overlapping or invalid audio resource ranges')
        result.extend(resource[cursor:start])
        result.extend(bytes((-len(result)) % 16))
        offsets[key] = len(result)
        result.extend(replacement if replacement is not None else resource[start:start + size])
        cursor = start + size
    result.extend(resource[cursor:])
    return bytes(result), offsets


def build(obb, steam, output):
    import UnityPy
    index = defaultdict(list)
    for group in audio.GROUPS:
        asset = steam / (group + '.assets')
        resource = steam / (group + '.resource')
        if any(not p.is_file() or p.is_symlink() for p in (asset, resource)):
            raise ValueError('missing regular Steam asset or resource file')
        for obj in UnityPy.load(str(asset)).objects:
            if obj.type.name == 'AudioClip':
                clip = obj.read()
                index[clip.m_Name].append(clip)
    if not index:
        raise ValueError('no Steam audio clips found')
    replacements = {}
    replaced = unmatched = 0
    with zipfile.ZipFile(obb) as zin:
        infos = zin.infolist()
        if len({i.filename for i in infos}) != len(infos):
            raise ValueError('duplicate OBB entry')
        for group in audio.GROUPS:
            data = audio.split_data(zin, group)
            raw_resource = zin.read(audio.PREFIX + group + '.resource')
            records = []
            for obj in UnityPy.load(data).objects:
                if obj.type.name != 'AudioClip':
                    continue
                clip = obj.read()
                dst = clip.m_Resource
                if not dst.m_Source or not dst.m_Size:
                    continue
                if (dst.m_Source.rsplit('/', 1)[-1] != group + '.resource' or dst.m_Offset < 0 or
                        dst.m_Offset + dst.m_Size > len(raw_resource)):
                    raise ValueError('unexpected target resource reference')
                body = obj.get_raw_data()
                if (len(body) < 20 or data[obj.byte_start:obj.byte_start + len(body)] != body or
                        struct.unpack_from('<QQ', body, len(body) - 20) != (dst.m_Offset, dst.m_Size)):
                    raise ValueError('unexpected audio object layout')
                candidates = [c for c in index[clip.m_Name] if audio.compatible(clip, c)]
                if len(candidates) > 1:
                    raise ValueError('ambiguous Steam audio clip')
                payload = None
                if candidates:
                    src = candidates[0].m_Resource
                    name = Path(src.m_Source)
                    if (name.name != str(name) or name.name not in {g + '.resource' for g in audio.GROUPS}
                            or src.m_Offset < 0 or src.m_Size <= 0):
                        raise ValueError('unexpected Steam resource reference')
                    with (steam / name).open('rb') as stream:
                        stream.seek(src.m_Offset)
                        payload = stream.read(src.m_Size)
                    original = raw_resource[dst.m_Offset:dst.m_Offset + dst.m_Size]
                    if len(payload) != src.m_Size or audio.fsb_codec(payload) != audio.fsb_codec(original):
                        raise ValueError('incompatible Steam audio payload')
                    replaced += 1
                else:
                    unmatched += 1
                records.append((obj.path_id, dst.m_Offset, dst.m_Size, payload, obj.byte_start + len(body) - 20))
            if not records:
                raise ValueError('no packed audio clips found')
            packed, offsets = repack(raw_resource, [(k, start, size, p) for k, start, size, p, _ in records])
            modified = bytearray(data)
            for key, start, size, payload, field in records:
                struct.pack_into('<QQ', modified, field, offsets[key], len(payload) if payload is not None else size)
            prefix = audio.PREFIX + group + '.assets.split'
            split_infos = sorted((i for i in infos if i.filename.startswith(prefix)), key=lambda i: int(i.filename[len(prefix):]))
            pos = 0
            for info in split_infos:
                replacements[info.filename] = bytes(modified[pos:pos + info.file_size])
                pos += info.file_size
            if pos != len(modified):
                raise ValueError('asset split length changed')
            replacements[audio.PREFIX + group + '.resource'] = packed
        if not replaced:
            raise ValueError('no clips replaced')
        created = False
        try:
            with output.open('xb') as target:
                created = True
                with zipfile.ZipFile(target, 'w') as zout:
                    for info in infos:
                        raw = replacements.get(info.filename)
                        zout.writestr(info, raw if raw is not None else zin.read(info))
        except BaseException:
            if created:
                output.unlink(missing_ok=True)
            raise
    print(f'Replaced {replaced} clips; preserved {unmatched} unmatched clips. Output contains game data.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--obb', required=True, type=Path)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    obb = args.obb.resolve(strict=True)
    steam = args.steam_data.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new file in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (obb, steam, ROOT)):
        parser.error('output must be outside the inputs and source checkout')
    build(obb, steam, output)


if __name__ == '__main__':
    main()
