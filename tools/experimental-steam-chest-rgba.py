#!/usr/bin/env python3
"""Reserialize one chest normal as RGBA32 from local Steam data.

This opt-in private research stage must run before generating any OBB plans.
The output OBB contains game data and must not be distributed. UnityPy
reparsing does not establish Unity Android loading or visual equivalence.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('chest_partial', ROOT / 'tools/experimental-partial-rgba-plan.py')
partial = importlib.util.module_from_spec(spec); spec.loader.exec_module(partial)
GROUP = 'sharedassets1.assets'
NAME = 't_chest_normal'
CHUNK = 1024 * 1024
RGBA_SIZE = 349524


def split_serialized(data, parts):
    """Keep the original split names; fail closed if another part is needed."""
    if (not parts or len({part.filename for part in parts}) != len(parts) or
            any(part.file_size != CHUNK for part in parts[:-1]) or not 0 < parts[-1].file_size <= CHUNK):
        raise ValueError('unexpected serialized asset split sizes')
    if not 0 < len(data) - (len(parts) - 1) * CHUNK <= CHUNK:
        raise ValueError('reserialized asset needs a different split count')
    return {part.filename: data[i * CHUNK:(i + 1) * CHUNK] for i, part in enumerate(parts)}


def augmented_manifest(report, path_id):
    if (not isinstance(report, dict) or report.get('schema') != 1 or
            not isinstance(report.get('textures'), list) or type(path_id) is not int):
        raise ValueError('invalid texture study manifest')
    result = copy.deepcopy(report)
    if any(row.get('group') == 'sharedassets1' and row.get('target_id') == path_id for row in result['textures']):
        raise ValueError('chest already appears in texture study manifest')
    result['textures'].append({'group': 'sharedassets1', 'target_id': path_id, 'name': NAME,
                               'source_format': 10, 'target_format': 4, 'normal': False,
                               'quality': False, 'direct_normal': False, 'bytes': RGBA_SIZE})
    return result


def object_fingerprints(env):
    rows = {}
    for obj in env.objects:
        if obj.path_id in rows: raise ValueError('duplicate asset PathID')
        rows[obj.path_id] = (obj.type.name, hashlib.sha256(obj.get_raw_data()).digest())
    return rows


def externals(asset):
    return [(entry.path, entry.guid, entry.type) for entry in asset.externals]


def validate_target(target, tree):
    stream = getattr(target, 'm_StreamData', None)
    if ((target.m_Width, target.m_Height, target.m_MipCount, target.m_ImageCount,
         target.m_TextureDimension, int(target.m_TextureFormat), len(bytes(target.image_data))) != (256, 256, 9, 1, 2, 45, 43704) or
            tree.get('m_CompleteImageSize') != 43704 or tree.get('m_MipsStripped') != 0 or
            tree.get('image data') != bytes(target.image_data) or
            (stream and (getattr(stream, 'path', '') or getattr(stream, 'size', 0)))):
        raise ValueError('unexpected chest texture metadata')


def select_source(target, index):
    candidates = partial.candidate_sources(index, target, GROUP, NAME)
    source = partial.study.select_source(target, candidates, texture=lambda item: item[2])
    if source is None or source[0] != 'sharedassets1' or int(source[2].m_TextureFormat) != 10:
        raise ValueError('chest Steam source missing or ambiguous')
    return source


def generate(args):
    import UnityPy
    if (args.obb.is_symlink() or args.manifest.is_symlink() or
            not args.obb.is_file() or not args.manifest.is_file()):
        raise ValueError('OBB and manifest must be regular files')
    steam = args.steam_data.resolve(strict=True); obb = args.obb.resolve(strict=True); manifest = args.manifest.resolve(strict=True)
    output = partial.masker.regular_output(args.output, (steam, obb, manifest))
    report = json.loads(manifest.read_text()); index, _ = partial.sources(steam)
    with zipfile.ZipFile(obb) as archive:
        infos = partial.masker.zip_entries(archive); groups = partial.objects.asset_groups(archive)
        if GROUP not in groups: raise ValueError('sharedassets1 group missing')
        parts = groups[GROUP]; original = b''.join(archive.read(info) for info in parts)
        env = UnityPy.load(original)
        if len(env.files) != 1: raise ValueError('unexpected serialized asset container')
        asset = next(iter(env.files.values())); before_externals = externals(asset); before = object_fingerprints(env)
        matches = [obj for obj in env.objects if obj.type.name == 'Texture2D' and obj.read().m_Name == NAME]
        if len(matches) != 1: raise ValueError('chest texture missing or ambiguous')
        obj = matches[0]; target = obj.read(); tree = obj.read_typetree()
        validate_target(target, tree)
        source = select_source(target, index)
        rgba = partial.converted(source[2], 256, 256, 9, 4)
        if len(rgba) != RGBA_SIZE: raise ValueError('unexpected RGBA texture size')
        updated_manifest = augmented_manifest(report, obj.path_id)
        old_tree = {k: v for k, v in tree.items() if k != 'image data'}
        tree['m_TextureFormat'] = 4; tree['m_CompleteImageSize'] = len(rgba); tree['image data'] = rgba
        obj.save_typetree(tree); encoded = asset.save()
        replacements = split_serialized(encoded, parts)
        reread = UnityPy.load(encoded)
        if len(reread.files) != 1 or externals(next(iter(reread.files.values()))) != before_externals:
            raise ValueError('serialized external references differ')
        after = object_fingerprints(reread)
        if set(before) != set(after) or [key for key in before if before[key] != after[key]] != [obj.path_id]:
            raise ValueError('unrelated serialized object changed')
        verified = next(item for item in reread.objects if item.path_id == obj.path_id).read_typetree()
        if verified['image data'] != rgba: raise ValueError('rewritten RGBA payload differs')
        new_tree = {k: v for k, v in verified.items() if k != 'image data'}
        differences = {k for k in set(old_tree) | set(new_tree) if old_tree.get(k) != new_tree.get(k)}
        if differences != {'m_TextureFormat', 'm_CompleteImageSize'} or new_tree['m_TextureFormat'] != 4 or new_tree['m_CompleteImageSize'] != RGBA_SIZE:
            raise ValueError('unexpected chest metadata change')
        output.mkdir(mode=0o700)
        try:
            with zipfile.ZipFile(output / 'converted.obb', 'x') as dest:
                for info in infos:
                    data = replacements[info.filename] if info.filename in replacements else archive.read(info)
                    dest.writestr(copy.copy(info), data)
            (output / 'manifest.json').write_text(json.dumps(updated_manifest, separators=(',', ':')) + '\n')
            summary = {'schema': 1, 'target_path_id': obj.path_id, 'source_group': source[0], 'source_path_id': source[1],
                       'old_payload_bytes': 43704, 'rgba_payload_bytes': RGBA_SIZE, 'other_objects_checked': len(before) - 1,
                       'external_references_checked': len(before_externals), 'split_parts': len(parts),
                       'limitations': ['Private game data remains.', 'Unity Android loading and appearance are unverified.']}
            (output / 'report.json').write_text(json.dumps(summary, indent=2) + '\n')
        except BaseException:
            shutil.rmtree(output); raise
    print('Reserialized one chest texture from local Steam data; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--steam-data', required=True, type=Path); p.add_argument('--obb', required=True, type=Path)
    p.add_argument('--manifest', required=True, type=Path); p.add_argument('--output', required=True, type=Path)
    generate(p.parse_args())


if __name__ == '__main__': main()
