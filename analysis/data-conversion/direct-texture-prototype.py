#!/usr/bin/env python3
"""Prove local extraction and restoration of byte-identical texture payloads.

Experimental, pinned to one build. The template still contains game content;
neither it nor its manifest is asserted to be cleared for distribution.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import UnityPy

SOURCE_ASSETS = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
SOURCE_STREAM = '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET = '5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'

def sha_file(path):
    h = hashlib.sha256()
    with path.open('rb') as source:
        for b in iter(lambda: source.read(1024 * 1024), b''): h.update(b)
    return h.hexdigest()

def sha_bytes(data): return hashlib.sha256(data).hexdigest()

def require_hash(path, expected):
    if not path.is_file() or sha_file(path) != expected:
        raise ValueError(f'missing or unsupported file: {path}')

def metadata(tex):
    return (tex.m_Name, tex.m_Width, tex.m_Height, tex.m_TextureFormat,
            tex.m_MipCount, tex.m_ColorSpace, tex.m_ImageCount)

def source_map(env):
    result = collections.defaultdict(list)
    for obj in env.objects:
        if obj.type.name == 'Texture2D':
            t = obj.read(); result[(t.m_Name, t.m_Width, t.m_Height)].append((obj.path_id, t))
    return result

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('action', choices=('template', 'restore'))
parser.add_argument('pc_data', type=Path)
parser.add_argument('input_dir', type=Path)
parser.add_argument('output_dir', type=Path)
args = parser.parse_args()
if args.output_dir.exists(): raise SystemExit('output directory already exists')
require_hash(args.pc_data / 'sharedassets1.assets', SOURCE_ASSETS)
require_hash(args.pc_data / 'sharedassets1.assets.resS', SOURCE_STREAM)
pc_env = UnityPy.load(str(args.pc_data / 'sharedassets1.assets'))
sources = source_map(pc_env)
asset = args.input_dir / 'sharedassets1.assets'
manifest_path = args.input_dir / 'manifest.json'
if args.action == 'template':
    require_hash(asset, TARGET)
else:
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    if manifest.get('version') != 1 or manifest.get('target_sha256') != TARGET or len(manifest.get('textures', [])) != 168:
        raise SystemExit('unsupported manifest')
    require_hash(asset, manifest['template_sha256'])
env = UnityPy.load(str(asset))
objects = {o.path_id: o for o in env.objects}
records = []
if args.action == 'template':
    for obj in env.objects:
        if obj.type.name != 'Texture2D': continue
        dst = obj.read(); matches = sources[(dst.m_Name, dst.m_Width, dst.m_Height)]
        if len(matches) != 1: continue
        src_id, src = matches[0]
        if metadata(src) != metadata(dst): continue
        try:
            raw = src.get_image_data(); existing = dst.get_image_data()
        except Exception:
            continue
        if not raw or raw != existing or len(dst.image_data or b'') != len(raw): continue
        records.append({'target_id': obj.path_id, 'source_id': src_id, 'name': dst.m_Name,
                        'size': len(raw), 'sha256': sha_bytes(raw)})
        dst.image_data = bytes(len(raw)); dst.save()
    if len(records) != 168: raise SystemExit(f'unexpected matching texture count: {len(records)}')
else:
    seen = set()
    for rec in manifest['textures']:
        tid = rec['target_id']
        if tid in seen or tid not in objects or objects[tid].type.name != 'Texture2D':
            raise ValueError('invalid target object')
        seen.add(tid); dst = objects[tid].read()
        matches = sources[(dst.m_Name, dst.m_Width, dst.m_Height)]
        if len(matches) != 1 or matches[0][0] != rec['source_id'] or dst.m_Name != rec['name']:
            raise ValueError('source texture identity mismatch')
        src = matches[0][1]
        if metadata(src) != metadata(dst): raise ValueError('texture metadata mismatch')
        raw = src.get_image_data()
        if len(raw) != rec['size'] or sha_bytes(raw) != rec['sha256'] or dst.image_data != bytes(len(raw)):
            raise ValueError('texture bytes do not match manifest or template')
        dst.image_data = raw; dst.save()
    records = manifest['textures']
args.output_dir.mkdir(parents=True)
try:
    path = args.output_dir / 'sharedassets1.assets'
    path.write_bytes(env.file.save())
    result_hash = sha_file(path)
    if args.action == 'restore' and result_hash != TARGET:
        raise ValueError('reconstructed asset hash mismatch')
    if args.action == 'template':
        manifest = {'version': 1, 'target_sha256': TARGET, 'template_sha256': result_hash, 'textures': records}
        (args.output_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(args.action, len(records), 'textures', sum(x['size'] for x in records), 'bytes', 'sha256', result_hash)
    print('Research output includes other game data and is not cleared for distribution.')
except Exception:
    for path in args.output_dir.iterdir(): path.unlink()
    args.output_dir.rmdir()
    raise
