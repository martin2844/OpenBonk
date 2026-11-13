#!/usr/bin/env python3
"""Combine pinned direct and RGBA texture experiments in one local template.

This prototype and its output still contain game content. Not a release tool.
"""
import argparse
import hashlib
import json
from pathlib import Path
import UnityPy

PC_ASSETS = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
PC_STREAM = '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET = '5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
EXPECTED = '50b9946e17800ced44427c625aa9ab1b9418a4c1618813c9ad1d270a34a5a4d9'
DIRECT_MANIFEST = '3bcf4a4ff496fcdac1520384bd30c49d671e9944b4ec5e7e23cc9e23ae5fe100'
RGBA_MANIFEST = 'abbad059169083017f36b6c560faed2ad88c92a17d6b4bc8f76d9816d5327750'

def sha_file(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def require(path, expected):
    if not path.is_file() or sha_file(path) != expected: raise ValueError(f'missing or unsupported file: {path}')

def metadata(t):
    return (t.m_Name, t.m_Width, t.m_Height, t.m_TextureFormat, t.m_MipCount, t.m_ColorSpace, t.m_ImageCount)

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('action', choices=('template', 'restore'))
parser.add_argument('pc_data', type=Path)
parser.add_argument('input_dir', type=Path)
parser.add_argument('direct_manifest', type=Path)
parser.add_argument('rgba_manifest', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
if args.output_dir.exists(): raise SystemExit('output directory already exists')
for path, expected in ((args.pc_data/'sharedassets1.assets', PC_ASSETS),
                       (args.pc_data/'sharedassets1.assets.resS', PC_STREAM),
                       (args.direct_manifest, DIRECT_MANIFEST), (args.rgba_manifest, RGBA_MANIFEST)):
    require(path, expected)
direct = json.loads(args.direct_manifest.read_text())['textures']
rgba = json.loads(args.rgba_manifest.read_text())['textures']
if len(direct) != 168 or len(rgba) != 289 or len({r['target_id'] for r in direct + rgba}) != 457:
    raise ValueError('invalid or overlapping texture manifest')
asset = args.input_dir/'sharedassets1.assets'
if args.action == 'template':
    require(asset, TARGET)
else:
    metadata_path = args.input_dir/'combined.json'
    m = json.loads(metadata_path.read_text())
    if m.get('version') != 1 or m.get('direct_manifest_sha256') != DIRECT_MANIFEST or m.get('rgba_manifest_sha256') != RGBA_MANIFEST:
        raise ValueError('invalid combined manifest')
    require(asset, m['template_sha256'])
pc_env = UnityPy.load(str(args.pc_data/'sharedassets1.assets'))
srcs = {o.path_id: o for o in pc_env.objects}
env = UnityPy.load(str(asset)); targets = {o.path_id: o for o in env.objects}
for mode, records in (('direct', direct), ('rgba', rgba)):
    for rec in records:
        sid, tid = rec['source_id'], rec['target_id']
        if sid not in srcs or tid not in targets or srcs[sid].type.name != 'Texture2D' or targets[tid].type.name != 'Texture2D':
            raise ValueError('texture object missing')
        source, dest = srcs[sid].read(), targets[tid].read()
        if source.m_Name != rec['name'] or dest.m_Name != rec['name'] or (source.m_Width, source.m_Height) != (dest.m_Width, dest.m_Height):
            raise ValueError('texture identity mismatch')
        if len(dest.image_data or b'') != rec['size'] or (dest.m_ColorSpace, dest.m_ImageCount) != (source.m_ColorSpace, source.m_ImageCount):
            raise ValueError('texture size or metadata mismatch')
        if mode == 'direct':
            raw = source.get_image_data()
            if metadata(source) != metadata(dest) or len(raw) != rec['size'] or hashlib.sha256(raw).hexdigest() != rec['sha256']:
                raise ValueError('direct source mismatch')
        else:
            if dest.m_TextureFormat != 47 or dest.m_MipCount != 1 or 'normal' in dest.m_Name.lower():
                raise ValueError('RGBA source metadata mismatch')
            image = source.image.convert('RGBA')
            if hashlib.sha256(image.tobytes()).hexdigest() != rec['source_rgba_sha256']:
                raise ValueError('RGBA source mismatch')
        if args.action == 'template':
            if mode == 'direct' and dest.image_data != raw: raise ValueError('target direct bytes mismatch')
            dest.image_data = bytes(rec['size'])
        else:
            if dest.image_data != bytes(rec['size']): raise ValueError('template was not cleared')
            if mode == 'direct': dest.image_data = raw
            else: dest.set_image(image, target_format=4, mipmap_count=1)
        dest.save()
args.output_dir.mkdir(parents=True)
try:
    path = args.output_dir/'sharedassets1.assets'; path.write_bytes(env.file.save()); result = sha_file(path)
    if args.action == 'restore' and result != EXPECTED: raise ValueError('combined output digest mismatch')
    if args.action == 'template':
        m = {'version': 1, 'direct_manifest_sha256': DIRECT_MANIFEST, 'rgba_manifest_sha256': RGBA_MANIFEST,
             'template_sha256': result}
        (args.output_dir/'combined.json').write_text(json.dumps(m, indent=2)+'\n')
    print(args.action, '457 textures; sha256', result)
    print('Local research output contains other game content and is not a release artifact.')
except Exception:
    for path in args.output_dir.iterdir(): path.unlink()
    args.output_dir.rmdir(); raise
