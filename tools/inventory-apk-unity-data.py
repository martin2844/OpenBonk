#!/usr/bin/env python3
"""Inventory serialized Unity objects stored directly in a local APK.

This is a classification aid, not an asset, provenance or rights audit. It
lists other assets by name and size without inspecting their contents. It does
not inspect nested archives, managed resources, native libraries, other APK
entries or object contents. Keep the report local because names and paths can
be game-derived.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
BASE = r'(?:globalgamemanagers(?:\.assets)?|level[0-9]+|resources\.assets|sharedassets[0-9]+\.assets|[0-9a-f]{32})'
SINGLE = re.compile(BASE + r'\Z')
SPLIT = re.compile('(' + BASE + r')\.split(0|[1-9][0-9]*)\Z')
MEDIA = frozenset(('Texture2D', 'Texture2DArray', 'Texture3D', 'Cubemap', 'CubemapArray',
                   'RenderTexture', 'AudioClip', 'VideoClip', 'Sprite', 'SpriteAtlas',
                   'Mesh', 'Material', 'AnimationClip', 'TerrainData', 'TextAsset',
                   'Shader', 'ComputeShader', 'ShaderVariantCollection', 'Font', 'MovieTexture'))
BUILTIN = frozenset(('Resources/unity default resources', 'Resources/unity_builtin_extra'))
MAX_GROUP = 256 * 1024 * 1024


def group_entries(archive):
    infos = archive.infolist()
    if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate archive entries')
    groups, other = defaultdict(list), []
    for info in infos:
        if info.is_dir() or not info.filename.startswith(PREFIX): continue
        name = info.filename[len(PREFIX):]
        match = SPLIT.fullmatch(name)
        if match: groups[match.group(1)].append((int(match.group(2)), info, True))
        elif SINGLE.fullmatch(name) or name in BUILTIN: groups[name].append((0, info, False))
        else: other.append({'name': name, 'size': info.file_size})
    result = {}
    for name, parts in groups.items():
        parts.sort(key=lambda x: x[0])
        if ([n for n, _, _ in parts] != list(range(len(parts))) or
                (len(parts) > 1 and not all(split for _, _, split in parts)) or
                sum(info.file_size for _, info, _ in parts) > MAX_GROUP):
            raise ValueError('invalid or oversized Unity group: ' + name)
        result[name] = [info for _, info, _ in parts]
    return result, other


def inventory(archive):
    groups, other = group_entries(archive)
    outer_assets = [{'name': info.filename, 'size': info.file_size}
                    for info in archive.infolist()
                    if not info.is_dir() and info.filename.startswith('assets/')
                    and not info.filename.startswith(PREFIX)]
    output, problems = [], []
    for name, parts in sorted(groups.items()):
        size = sum(info.file_size for info in parts)
        try:
            serialized = b''.join(archive.read(info) for info in parts)
            objects = UnityPy.load(serialized).objects
            counts, media = Counter(), []
            for obj in objects:
                kind = obj.type.name
                counts[kind] += 1
                if kind in MEDIA:
                    try:
                        value = obj.read()
                        asset_name = getattr(value, 'm_Name', '')
                        if kind == 'Shader' and not asset_name:
                            asset_name = obj.read_typetree()['m_ParsedForm']['m_Name']
                        if not isinstance(asset_name, str): raise ValueError('invalid object name')
                        payload = obj.get_raw_data()
                        if len(payload) != obj.byte_size: raise ValueError('invalid object size')
                        media.append({'type': kind, 'name': asset_name,
                                      'path_id': obj.path_id, 'serialized_bytes': obj.byte_size,
                                      'serialized_sha256': hashlib.sha256(payload).hexdigest()})
                    except Exception as error:
                        problems.append({'group': name, 'path_id': obj.path_id, 'reason': type(error).__name__})
            output.append({'group': name, 'size': size, 'part_count': len(parts),
                           'sha256': hashlib.sha256(serialized).hexdigest(),
                           'types': dict(sorted(counts.items())), 'media': media})
        except Exception as error:
            problems.append({'group': name, 'reason': type(error).__name__})
    return {'serialized_groups': output, 'other_data_entries': sorted(other, key=lambda x: x['name']),
            'other_asset_entries': sorted(outer_assets, key=lambda x: x['name']),
            'problems': problems,
            'limitations': 'Object inventory covers direct assets/bin/Data only; other assets are listed without inspection. Nested archives and non-asset APK entries are not covered. This is not an ownership or asset-free audit.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apk', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    if args.apk.is_symlink() or not args.apk.is_file(): parser.error('APK must be a regular file')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): parser.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or output == args.apk.resolve(): parser.error('output must be outside the checkout and input')
    with zipfile.ZipFile(args.apk) as archive: result = inventory(archive)
    with output.open('x', encoding='utf-8') as stream: json.dump(result, stream, indent=2); stream.write('\n')
    print(f"Serialized groups: {len(result['serialized_groups'])}; other Unity data entries: {len(result['other_data_entries'])}; other asset entries: {len(result['other_asset_entries'])}; problems: {len(result['problems'])}")


if __name__ == '__main__': main()
