#!/usr/bin/env python3
"""Local experiment rebuilding Unity's splash texture from pinned Steam data.

This does not make the template or resulting APK redistributable: other Unity,
game and third-party data remain, and the splash asset itself needs review.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

import UnityPy
from UnityPy.export import Texture2DConverter as C

APK = '7cd9dc21d007dd7d2443bc6d070d9f7f1b3f0c812e7376f3df9bf11a92d996f2'
PC = 'b3f7db2d847edd89f66e387a9bbc926b5c555314a23ca55a0531cf662c89bbe0'
STREAM = '39dd38fcd8dd95d41d6d55fe16a448d29fe2c31d9a162c825c24c6d99a893d7a'

def digest(data): return hashlib.sha256(data).hexdigest()
def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda: f.read(1048576), b''): h.update(b)
    return h.hexdigest()
def require(path, expected):
    if not path.is_file() or sha(path) != expected: raise ValueError('incompatible input: ' + str(path))
def texture(env):
    objects = [o for o in env.objects if o.type.name == 'Texture2D']
    if len(objects) != 1: raise ValueError('unexpected texture count')
    obj = objects[0]; data = obj.read()
    if (obj.path_id, data.m_Name, data.m_Width, data.m_Height, data.m_MipCount, data.m_ImageCount, data.m_ColorSpace) != (3, 'Splash Screen Unity Logo', 2048, 1024, 12, 1, 0):
        raise ValueError('unexpected texture metadata')
    return obj, data

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('action', choices=('template', 'restore'))
p.add_argument('pc_data', type=Path); p.add_argument('input', type=Path); p.add_argument('output', type=Path)
a = p.parse_args()
if a.output.exists() or not a.output.parent.is_dir(): p.error('output must be new and its parent must exist')
require(a.pc_data / 'globalgamemanagers.assets', PC)
require(a.pc_data / 'globalgamemanagers.assets.resS', STREAM)
source_obj, source = texture(UnityPy.load(str(a.pc_data / 'globalgamemanagers.assets')))
source_raw = bytes(source.get_image_data())
if source.m_TextureFormat != 12 or len(source_raw) != 2796240: raise ValueError('unexpected PC splash format')
if a.action == 'template':
    require(a.input, APK)
    with zipfile.ZipFile(a.input) as z:
        prefix = 'assets/bin/Data/globalgamemanagers.assets.split'
        names = sorted((n for n in z.namelist() if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
        if not names or [int(n[len(prefix):]) for n in names] != list(range(len(names))): raise ValueError('invalid asset fragments')
        original = b''.join(z.read(n) for n in names)
    target_obj, target = texture(UnityPy.load(original))
    payload = bytes(target.image_data)
    if target.m_TextureFormat != 47 or len(payload) != len(source_raw) or not any(payload): raise ValueError('unexpected Android splash format')
    raw = target_obj.get_raw_data(); offset_in_object = raw.find(payload)
    if offset_in_object < 0 or raw.find(payload, offset_in_object + 1) >= 0: raise ValueError('payload position is ambiguous')
    offset = target_obj.byte_start + offset_in_object
    if original[offset:offset + len(payload)] != payload: raise ValueError('asset offset mismatch')
    work = bytearray(original); work[offset:offset + len(payload)] = bytes(len(payload))
    manifest = {'version': 1, 'source_apk_sha256': APK, 'pc_sha256': PC, 'pc_stream_sha256': STREAM,
                'original_sha256': digest(original), 'template_sha256': digest(work), 'offset': offset,
                'size': len(payload), 'source_payload_sha256': digest(source_raw)}
    result = bytes(work)
else:
    manifest = json.loads((a.input / 'manifest.json').read_text())
    if manifest.get('version') != 1 or manifest.get('source_apk_sha256') != APK or manifest.get('pc_sha256') != PC or manifest.get('pc_stream_sha256') != STREAM or manifest.get('source_payload_sha256') != digest(source_raw): raise ValueError('incompatible manifest')
    template = (a.input / 'globalgamemanagers.assets').read_bytes()
    if digest(template) != manifest['template_sha256']: raise ValueError('template hash mismatch')
    target_obj, target = texture(UnityPy.load(template))
    offset, length = manifest['offset'], manifest['size']
    if target.m_TextureFormat != 47 or length != 2796240 or offset < target_obj.byte_start or offset + length > target_obj.byte_start + target_obj.byte_size or any(template[offset:offset + length]): raise ValueError('invalid target range')
    converted = []; pos = 0
    for level in range(12):
        w, h = max(1, 2048 >> level), max(1, 1024 >> level)
        size = ((w + 3) // 4) * ((h + 3) // 4) * 16
        im = C.parse_image_data(source_raw[pos:pos + size], w, h, 12, source.object_reader.version, source.object_reader.platform).convert('RGBA')
        encoded, fmt = C.image_to_texture2d(im, 47, target.object_reader.platform, target.m_PlatformBlob)
        if int(fmt) != 47 or len(encoded) != size: raise ValueError('unexpected converted mip')
        converted.append(encoded); pos += size
    payload = b''.join(converted)
    if pos != len(source_raw) or len(payload) != length: raise ValueError('invalid mip lengths')
    work = bytearray(template); work[offset:offset + length] = payload; result = bytes(work)
    check_obj, check = texture(UnityPy.load(result))
    if check_obj.byte_start != target_obj.byte_start or bytes(check.image_data) != payload: raise ValueError('invalid restored asset')
a.output.mkdir()
try:
    (a.output / 'globalgamemanagers.assets').write_bytes(result)
    if a.action == 'template': (a.output / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    print(a.action, 'splash bytes', len(source_raw), 'asset SHA256', digest(result))
    print('Local experiment; other assets and code remain, not a distributable package.')
except Exception:
    shutil.rmtree(a.output)
    raise
