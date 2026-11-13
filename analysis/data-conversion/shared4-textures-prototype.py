#!/usr/bin/env python3
"""Experimentally reconstruct final-scene textures from pinned Steam assets.

BC1 -> ETC2 conversion is lossy. The template retains other original content;
neither the template nor the result is cleared for distribution.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import UnityPy
from UnityPy.export import Texture2DConverter as converter

PINS = {
    'sharedassets4.assets': 'cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3',
    'sharedassets4.assets.resS': '862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127',
}
WINDOWS_PINS = {**PINS, 'sharedassets4.assets':'896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9'}
BASE = 'd9461f6047e92558dfdc552da980f1c1086cea9feacb22d8cee2e7b9debc333e'
TEXTURES = {5: 'B_Pylon_Color', 6: 'B_Pylon_Emission', 7: 'CONCRETE_1B'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def sha(path):
    with path.open('rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('template', 'restore'))
    p.add_argument('pc_data', type=Path)
    p.add_argument('input_asset', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--manifest', type=Path)
    p.add_argument('--windows-source', action='store_true')
    a = p.parse_args()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    out = a.output.resolve()
    for source in (a.pc_data, a.input_asset):
        source = source.resolve()
        if out.is_relative_to(source) or source.is_relative_to(out):
            p.error('output must be separate from inputs')
    for name, expected in (WINDOWS_PINS if a.windows_source else PINS).items():
        if not (a.pc_data / name).is_file() or sha(a.pc_data / name) != expected:
            raise ValueError('incompatible Steam input: ' + name)
    if a.action == 'template':
        if sha(a.input_asset) != BASE:
            raise ValueError('unexpected asset input')
        prior = None
    else:
        if not a.manifest:
            p.error('restore requires --manifest')
        prior = json.loads(a.manifest.read_text())
        if (prior.get('version') != 1 or prior.get('base_sha256') != BASE
                or sha(a.input_asset) != prior.get('template_sha256')):
            raise ValueError('unexpected template or manifest')
        if [r['target_id'] for r in prior['textures']] != list(TEXTURES):
            raise ValueError('unexpected texture inventory')
    source = {o.path_id: o for o in UnityPy.load(str(a.pc_data / 'sharedassets4.assets')).objects}
    env = UnityPy.load(str(a.input_asset))
    targets = {o.path_id: o for o in env.objects}
    if len(targets) != len(env.objects):
        raise ValueError('duplicate object IDs')
    untouched = {i: (o.type.name, digest(o.get_raw_data())) for i, o in targets.items() if i not in TEXTURES}
    records = []
    for target_id, expected_name in TEXTURES.items():
        src_obj, dst_obj = source[target_id], targets[target_id]
        if src_obj.type.name != 'Texture2D' or dst_obj.type.name != 'Texture2D':
            raise ValueError('texture type mismatch')
        src, dst = src_obj.read(), dst_obj.read()
        fields = ('m_Width', 'm_Height', 'm_MipCount', 'm_ImageCount', 'm_ColorSpace')
        if (src.m_Name != expected_name or dst.m_Name != expected_name or src.m_TextureFormat != 10
                or dst.m_TextureFormat != 45 or src.m_ImageCount != 1
                or any(getattr(src, k) != getattr(dst, k) for k in fields)
                or (dst.m_StreamData and dst.m_StreamData.path)):
            raise ValueError('texture metadata mismatch')
        raw = src.get_image_data()
        existing = bytes(dst.image_data)
        rec = {'target_id': target_id, 'source_id': target_id, 'name': expected_name,
               'width': src.m_Width, 'height': src.m_Height, 'mips': src.m_MipCount,
               'source_sha256': digest(raw), 'target_size': len(existing)}
        if not existing or len(existing) != len(raw):
            raise ValueError('texture payload size mismatch')
        if a.action == 'template':
            if not any(existing):
                raise ValueError('texture is already empty')
            dst.image_data = bytes(len(existing))
        else:
            if prior['textures'][len(records)] != rec or any(existing):
                raise ValueError('texture template mismatch')
            chunks, offset = [], 0
            for level in range(src.m_MipCount):
                width, height = max(1, src.m_Width >> level), max(1, src.m_Height >> level)
                size = ((width + 3) // 4) * ((height + 3) // 4) * 8
                pixels = converter.parse_image_data(raw[offset:offset + size], width, height,
                                                     10, src.object_reader.version, src.object_reader.platform).convert('RGBA')
                encoded, fmt = converter.image_to_texture2d(pixels, 45, dst.object_reader.platform, dst.m_PlatformBlob)
                if int(fmt) != 45 or len(encoded) != size:
                    raise ValueError('unexpected encoded texture format or size')
                chunks.append(encoded)
                offset += size
            rebuilt = b''.join(chunks)
            if offset != len(raw) or len(rebuilt) != len(existing):
                raise ValueError('texture mip layout mismatch')
            dst.image_data = rebuilt
        dst.save()
        records.append(rec)
    a.output.mkdir()
    try:
        result = a.output / 'sharedassets4.assets'
        result.write_bytes(env.file.save())
        check = {o.path_id: o for o in UnityPy.load(str(result)).objects}
        if set(check) != set(targets) or any(check[i].type.name != typ or digest(check[i].get_raw_data()) != h for i, (typ, h) in untouched.items()):
            raise ValueError('unrelated object changed')
        for rec in records:
            data = bytes(check[rec['target_id']].read().image_data)
            if len(data) != rec['target_size'] or (a.action == 'template' and any(data)):
                raise ValueError('saved texture mismatch')
        report = {'version': 1, 'base_sha256': BASE,
                  'template_sha256': sha(result) if prior is None else prior['template_sha256'],
                  'result_sha256': sha(result), 'textures': records, 'unchanged_objects': len(untouched),
                  'limitations': 'Lossy conversion; local research only, other game content and code remain.'}
        (a.output / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n')
        print(a.action, len(records), 'textures;', sum(r['target_size'] for r in records), 'bytes; SHA256', sha(result))
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__ == '__main__':
    main()
