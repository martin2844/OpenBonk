#!/usr/bin/env python3
"""Locally restore a pinned set of small textures from PC data as RGBA32.

Research prototype only: both the template and output contain game content.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import UnityPy

PC_ASSETS = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
PC_STREAM = '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET = '5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'

def sha_file(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda: source.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()

def require(path, expected):
    if not path.is_file() or sha_file(path) != expected:
        raise ValueError(f'missing or unsupported input: {path}')

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('action', choices=('template', 'restore'))
parser.add_argument('pc_data', type=Path)
parser.add_argument('input_dir', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
if args.output_dir.exists(): raise SystemExit('output directory already exists')
require(args.pc_data / 'sharedassets1.assets', PC_ASSETS)
require(args.pc_data / 'sharedassets1.assets.resS', PC_STREAM)
pc_env = UnityPy.load(str(args.pc_data / 'sharedassets1.assets'))
sources = collections.defaultdict(list)
for obj in pc_env.objects:
    if obj.type.name == 'Texture2D':
        t = obj.read(); sources[(t.m_Name, t.m_Width, t.m_Height)].append((obj.path_id, t))
asset = args.input_dir / 'sharedassets1.assets'
if args.action == 'template':
    require(asset, TARGET)
else:
    manifest = json.loads((args.input_dir / 'manifest.json').read_text(encoding='utf-8'))
    if manifest.get('version') != 1 or manifest.get('target_sha256') != TARGET or len(manifest.get('textures', [])) != 289:
        raise ValueError('invalid manifest')
    require(asset, manifest['template_sha256'])
env = UnityPy.load(str(asset)); objects = {o.path_id: o for o in env.objects}; records = []
if args.action == 'template':
    for obj in env.objects:
        if obj.type.name != 'Texture2D': continue
        dst = obj.read(); matches = sources[(dst.m_Name, dst.m_Width, dst.m_Height)]
        if len(matches) != 1 or dst.m_MipCount != 1 or dst.m_TextureFormat != 47 or dst.m_Width > 64 or dst.m_Height > 64: continue
        sid, src = matches[0]
        if 'normal' in dst.m_Name.lower() or dst.m_ImageCount != 1 or src.m_ImageCount != 1 or dst.m_ColorSpace != src.m_ColorSpace: continue
        image = src.image.convert('RGBA'); raw = dst.image_data
        if not raw: raise ValueError(f'non-inline texture: {dst.m_Name}')
        records.append({'target_id': obj.path_id, 'source_id': sid, 'name': dst.m_Name,
                        'size': len(raw), 'source_rgba_sha256': hashlib.sha256(image.tobytes()).hexdigest()})
        dst.image_data = bytes(len(raw)); dst.save()
    if len(records) != 289: raise ValueError(f'unexpected texture count: {len(records)}')
else:
    seen = set()
    for rec in manifest['textures']:
        tid = rec['target_id']
        if tid in seen or tid not in objects or objects[tid].type.name != 'Texture2D': raise ValueError('invalid target object')
        seen.add(tid); dst = objects[tid].read(); matches = sources[(dst.m_Name, dst.m_Width, dst.m_Height)]
        if len(matches) != 1 or matches[0][0] != rec['source_id'] or dst.m_Name != rec['name']:
            raise ValueError('source mismatch')
        src = matches[0][1]
        if dst.m_TextureFormat != 47 or dst.m_MipCount != 1 or dst.m_ColorSpace != src.m_ColorSpace or dst.image_data != bytes(rec['size']):
            raise ValueError('template texture mismatch')
        image = src.image.convert('RGBA')
        if hashlib.sha256(image.tobytes()).hexdigest() != rec['source_rgba_sha256']:
            raise ValueError('source pixels mismatch')
        dst.set_image(image, target_format=4, mipmap_count=1); dst.save()
    records = manifest['textures']
args.output_dir.mkdir(parents=True)
try:
    path = args.output_dir / 'sharedassets1.assets'; path.write_bytes(env.file.save())
    result_hash = sha_file(path)
    if args.action == 'template':
        manifest = {'version': 1, 'target_sha256': TARGET, 'template_sha256': result_hash, 'textures': records}
        (args.output_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(args.action, len(records), 'textures', 'serialized bytes', path.stat().st_size, 'sha256', result_hash)
    print('Research output includes other game data and is not cleared for distribution.')
except Exception:
    for path in args.output_dir.iterdir(): path.unlink()
    args.output_dir.rmdir(); raise
