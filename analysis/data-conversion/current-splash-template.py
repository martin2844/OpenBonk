#!/usr/bin/env python3
"""Blank and locally reconstruct the current diagnostic's Unity splash texture.

Research only. Other original assets, code and third-party content remain;
neither this template nor its output is cleared for distribution.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

import UnityPy
from UnityPy.export import Texture2DConverter as converter

APK_SHA = 'd20c58a2548196270e57b6e0392b4a103e09014ee4f01d4f4b970bb2924026a2'
ASSET_SHA = '349ba8a5672bf84b982f1eda17ecab568804c78d5c49e577f3330891515fb081'
ORIGINAL_PAYLOAD_SHA = '17357ee9b8400630b4495aeea8276001be1564e28d77f5de9ee8d38097a0dd23'
PC_SHA = 'b3f7db2d847edd89f66e387a9bbc926b5c555314a23ca55a0531cf662c89bbe0'
WINDOWS_PC_SHA = '9d7146c2d338034b798aec477ce5c3e494a9cc0969861c2706996bf69ae16915'
STREAM_SHA = '39dd38fcd8dd95d41d6d55fe16a448d29fe2c31d9a162c825c24c6d99a893d7a'
CONVERTED_SHA = '9f9a8db887c9b77a22704c48eb4923cb0889b951311be0efe3bdf01219719e6c'
SIZE = 2796240


def digest(data): return hashlib.sha256(data).hexdigest()
def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def texture(data):
    objects = [o for o in UnityPy.load(data).objects if o.type.name == 'Texture2D']
    if len(objects) != 1: raise ValueError('unexpected texture count')
    obj = objects[0]; value = obj.read()
    if (obj.path_id, value.m_Name, value.m_Width, value.m_Height, value.m_MipCount,
        value.m_ImageCount, value.m_ColorSpace) != (3, 'Splash Screen Unity Logo', 2048, 1024, 12, 1, 0):
        raise ValueError('unexpected splash metadata')
    return obj, value


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('template', 'restore'))
    p.add_argument('input', type=Path, help='pinned base APK or template directory')
    p.add_argument('output', type=Path, help='new output directory')
    p.add_argument('--steam-data', type=Path, help='required for restore')
    p.add_argument('--windows-source', action='store_true', help='use the pinned Windows splash for restore')
    a = p.parse_args(); out = a.output.absolute()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    for source in (a.input, a.steam_data):
        if source is not None:
            root = source.resolve()
            if out.resolve().is_relative_to(root) or root.is_relative_to(out.resolve()): p.error('output must be separate from inputs')
    if a.action == 'template':
        if not a.input.is_file() or sha(a.input) != APK_SHA: raise ValueError('incompatible base APK')
        with zipfile.ZipFile(a.input) as z:
            if len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate APK entry')
            prefix = 'assets/bin/Data/globalgamemanagers.assets.split'
            parts = sorted((n for n in z.namelist() if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
            if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))): raise ValueError('invalid asset fragments')
            original = b''.join(z.read(n) for n in parts)
        if digest(original) != ASSET_SHA: raise ValueError('incompatible original asset')
        obj, value = texture(original); payload = bytes(value.image_data)
        if value.m_TextureFormat != 47 or len(payload) != SIZE or digest(payload) != ORIGINAL_PAYLOAD_SHA:
            raise ValueError('unexpected Android splash payload')
        raw = obj.get_raw_data(); offset = raw.find(payload)
        if offset < 0 or raw.find(payload, offset + 1) >= 0: raise ValueError('ambiguous payload position')
        start = obj.byte_start + offset
        if original[start:start+SIZE] != payload: raise ValueError('invalid payload position')
        result = original[:start] + bytes(SIZE) + original[start+SIZE:]
        manifest = {'version':1, 'apk_sha256':APK_SHA, 'original_sha256':ASSET_SHA,
                    'original_payload_sha256':ORIGINAL_PAYLOAD_SHA, 'template_sha256':digest(result),
                    'pc_sha256':PC_SHA, 'pc_stream_sha256':STREAM_SHA,
                    'converted_payload_sha256':CONVERTED_SHA, 'offset':start, 'size':SIZE}
    else:
        if a.steam_data is None: p.error('restore requires --steam-data')
        m = json.loads((a.input/'manifest.json').read_text())
        expected = {'version':1, 'apk_sha256':APK_SHA, 'original_sha256':ASSET_SHA,
                    'original_payload_sha256':ORIGINAL_PAYLOAD_SHA, 'pc_sha256':PC_SHA,
                    'pc_stream_sha256':STREAM_SHA, 'converted_payload_sha256':CONVERTED_SHA, 'size':SIZE}
        if any(m.get(k) != v for k,v in expected.items()): raise ValueError('incompatible manifest')
        template = (a.input/'globalgamemanagers.assets').read_bytes()
        if digest(template) != m['template_sha256']: raise ValueError('incompatible template')
        obj, value = texture(template); start = m['offset']
        if (value.m_TextureFormat != 47 or not isinstance(start, int) or start < obj.byte_start
                or start + SIZE > obj.byte_start + obj.byte_size or any(template[start:start+SIZE])):
            raise ValueError('invalid blank payload range')
        for name, expected_hash in (('globalgamemanagers.assets',WINDOWS_PC_SHA if a.windows_source else PC_SHA), ('globalgamemanagers.assets.resS',STREAM_SHA)):
            if not (a.steam_data/name).is_file() or sha(a.steam_data/name) != expected_hash: raise ValueError('incompatible Steam data: '+name)
        _, pc = texture(str(a.steam_data/'globalgamemanagers.assets'))
        raw = bytes(pc.get_image_data())
        if pc.m_TextureFormat != 12 or len(raw) != SIZE or digest(raw) != STREAM_SHA: raise ValueError('invalid Steam splash payload')
        chunks = []; pos = 0
        for level in range(12):
            w, h = max(1, 2048 >> level), max(1, 1024 >> level)
            size = ((w + 3)//4) * ((h + 3)//4) * 16
            image = converter.parse_image_data(raw[pos:pos+size], w, h, 12, pc.object_reader.version, pc.object_reader.platform).convert('RGBA')
            encoded, fmt = converter.image_to_texture2d(image, 47, value.object_reader.platform, value.m_PlatformBlob)
            if int(fmt) != 47 or len(encoded) != size: raise ValueError('invalid converted mip')
            chunks.append(encoded); pos += size
        payload = b''.join(chunks)
        if pos != len(raw) or digest(payload) != CONVERTED_SHA: raise ValueError('unexpected converted payload')
        result = template[:start] + payload + template[start+SIZE:]
        restored_obj, restored = texture(result)
        if restored_obj.byte_start != obj.byte_start or bytes(restored.image_data) != payload: raise ValueError('invalid restored asset')
        manifest = {**m, 'restored_sha256':digest(result), 'limitations':'Lossy conversion; other game and third-party material remains.'}
    out.mkdir(mode=0o700)
    try:
        (out/'globalgamemanagers.assets').write_bytes(result)
        (out/'manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    except BaseException:
        shutil.rmtree(out); raise
    print(a.action, 'splash bytes', SIZE, 'output SHA-256', digest(result), '; research only')


if __name__ == '__main__': main()
