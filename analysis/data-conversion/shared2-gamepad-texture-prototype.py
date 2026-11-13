#!/usr/bin/env python3
"""Experimentally reconstruct GamepadTemplate from pinned Steam texture data.

BC3 -> ETC2 RGBA conversion is lossy. Other original material remains in the
template. This is a local research tool, not a distribution artifact.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import UnityPy
from UnityPy.export import Texture2DConverter as converter

PC_PINS = {
    'sharedassets1.assets': 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
    'sharedassets1.assets.resS': '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea',
}
WINDOWS_PINS = {**PC_PINS, 'sharedassets1.assets':'64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'}
BASE = 'dc0fa2e17c7607009ad45489f5995aac47cc81a021ae0ce46a0ecd4981bf3206'
TARGET, SOURCE = 171, 989
ANIMATIONS_REBUILT='d94659f3124eb35f9a400e684a1db71e92eba3d80936e74fec9eb7c48d768d86'


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
    p.add_argument('--experimental-animation-streams', action='store_true', help='accept the pinned rebuilt animation-stream variant')
    p.add_argument('--windows-source', action='store_true', help='restore from the pinned Windows asset')
    a = p.parse_args()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    out = a.output.resolve()
    for source in (a.pc_data, a.input_asset):
        source = source.resolve()
        if out.is_relative_to(source) or source.is_relative_to(out):
            p.error('output must be separate from inputs')
    for name, expected in (WINDOWS_PINS if a.windows_source else PC_PINS).items():
        if not (a.pc_data/name).is_file() or sha(a.pc_data/name) != expected:
            raise ValueError('incompatible Steam input: '+name)
    if a.action == 'template':
        if a.experimental_animation_streams:p.error('--experimental-animation-streams requires restore')
        if sha(a.input_asset) != BASE:
            raise ValueError('incompatible Android asset')
        manifest = None
    else:
        if not a.manifest:
            p.error('restore requires --manifest')
        manifest = json.loads(a.manifest.read_text())
        expected_input=ANIMATIONS_REBUILT if a.experimental_animation_streams else manifest.get('template_sha256')
        if manifest.get('version') != 1 or manifest.get('base_sha256') != BASE or sha(a.input_asset) != expected_input:
            raise ValueError('incompatible template or manifest')
    sources = {o.path_id: o for o in UnityPy.load(str(a.pc_data/'sharedassets1.assets')).objects}
    env = UnityPy.load(str(a.input_asset))
    targets = {o.path_id: o for o in env.objects}
    if len(targets) != len(env.objects) or sources[SOURCE].type.name != 'Texture2D' or targets[TARGET].type.name != 'Texture2D':
        raise ValueError('unexpected object inventory')
    src, dst = sources[SOURCE].read(), targets[TARGET].read()
    fields = ('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace')
    if (src.m_Name != 'GamepadTemplate' or src.m_TextureFormat != 12 or dst.m_TextureFormat != 47
            or any(getattr(src,k) != getattr(dst,k) for k in fields) or src.m_ImageCount != 1
            or (dst.m_StreamData and dst.m_StreamData.path)):
        raise ValueError('texture metadata mismatch')
    original = bytes(dst.image_data)
    steam = src.get_image_data()
    if len(original) != len(steam) or not original:
        raise ValueError('texture payload size mismatch')
    rec = {'target_id': TARGET, 'source_id': SOURCE, 'name': src.m_Name,
           'width': src.m_Width, 'height': src.m_Height, 'mips': src.m_MipCount,
           'source_sha256': digest(steam), 'target_size': len(original)}
    untouched = {i: (o.type.name, digest(o.get_raw_data())) for i,o in targets.items() if i != TARGET}
    if manifest is None:
        if not any(original):
            raise ValueError('texture already blank')
        dst.image_data = bytes(len(original))
    else:
        if manifest['texture'] != rec or any(original):
            raise ValueError('texture or template mismatch')
        chunks, offset = [], 0
        for level in range(src.m_MipCount):
            w,h = max(1,src.m_Width>>level),max(1,src.m_Height>>level)
            size = ((w+3)//4)*((h+3)//4)*16
            im = converter.parse_image_data(steam[offset:offset+size],w,h,12,src.object_reader.version,src.object_reader.platform).convert('RGBA')
            encoded, fmt = converter.image_to_texture2d(im,47,dst.object_reader.platform,dst.m_PlatformBlob)
            if int(fmt)!=47 or len(encoded)!=size:
                raise ValueError('unexpected encoded texture format or size')
            chunks.append(encoded);offset+=size
        if offset != len(steam) or sum(map(len,chunks)) != len(original):
            raise ValueError('mip layout mismatch')
        dst.image_data = b''.join(chunks)
    dst.save()
    a.output.mkdir()
    try:
        result=a.output/'sharedassets2.assets';result.write_bytes(env.file.save())
        check={o.path_id:o for o in UnityPy.load(str(result)).objects}
        if set(check)!=set(targets) or any(check[i].type.name!=typ or digest(check[i].get_raw_data())!=h for i,(typ,h) in untouched.items()):
            raise ValueError('unrelated object changed')
        texture=bytes(check[TARGET].read().image_data)
        if len(texture)!=len(original) or (manifest is None and any(texture)):
            raise ValueError('saved texture mismatch')
        report={'version':1,'base_sha256':BASE,'template_sha256':sha(result) if manifest is None else manifest['template_sha256'],
                'result_sha256':sha(result),'texture':rec,'unchanged_objects':len(untouched),
                'limitations':'Lossy conversion; local research only, other content remains.'}
        (a.output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
        print(a.action,'GamepadTemplate;',len(original),'bytes; SHA256',sha(result),flush=True)
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__=='__main__':
    main()
