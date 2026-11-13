#!/usr/bin/env python3
"""Create private ETC2 encoder fixtures from locally owned Steam assets.

The output contains game pixels and must not be distributed. This checks only
encoding: UnityPy still decodes the desktop source and handles mip padding.
"""
import hashlib
import json
from pathlib import Path

import UnityPy
from PIL import Image
from UnityPy.export import Texture2DConverter as C

ROOT = Path(__file__).resolve().parents[3]
STEAM = Path.home() / '.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data'
MANIFEST = ROOT / 'build/android/local-research-manifests/hashed-matched53-template/manifest.json'
OUT = ROOT / 'build/android/etcpak-arm64-study/all-source-fixtures'

def digest(value):
    return hashlib.sha256(value).hexdigest()

def main():
    if OUT.exists():
        raise ValueError('fixture output already exists')
    records = json.loads(MANIFEST.read_text())['textures']
    objects = {name: {obj.path_id: obj for obj in UnityPy.load(str(STEAM / name)).objects}
               for name in ('sharedassets1.assets', 'sharedassets2.assets')}
    OUT.mkdir(parents=True)
    commands, source_commands, results = [], [], []
    for rec in records:
        src = objects[rec['source_file']][rec['source_id']].read()
        raw = bytes(src.get_image_data())
        if digest(raw) != rec['source_sha256']:
            raise ValueError('Steam source mismatch')
        target_bytes = (ROOT / 'build/android/share-source-experiment/hashed-matched53-restored' / rec['file']).read_bytes()
        if digest(target_bytes) != rec['converted_sha256']:
            raise ValueError('converted target mismatch')
        target = UnityPy.load(target_bytes).objects[0].read()
        if target.m_TextureFormat not in (4, 45, 47):
            raise ValueError('unexpected target format')
        off = 0
        for lv in range(target.m_MipCount):
            w, h = max(1, target.m_Width >> lv), max(1, target.m_Height >> lv)
            fmt = src.m_TextureFormat
            n = w*h*(3 if fmt == 3 else 4) if fmt in (3, 4) else ((w+3)//4)*((h+3)//4)*(8 if fmt == 10 else 16)
            image = C.parse_image_data(raw[off:off+n], w, h, fmt, src.object_reader.version, src.object_reader.platform).convert('RGBA')
            expected, _ = C.image_to_texture2d(image, target.m_TextureFormat, target.object_reader.platform, target.m_PlatformBlob)
            image = image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
            pw, ph = C.get_compressed_image_size(w, h, target.m_TextureFormat)
            pixels = C.pad_image(image, pw, ph).tobytes('raw', 'RGBA')
            key = '%s-%02d' % (rec['file'], lv)
            (OUT / (key + '.rgba')).write_bytes(pixels)
            (OUT / (key + '.expected')).write_bytes(expected)
            mode = {4: 'raw', 45: 'rgb', 47: 'rgba'}[target.m_TextureFormat]
            commands.append('./scalar %s.rgba %s.etc %d %d %s' % (key, key, pw, ph, mode))
            (OUT / (key + '.source')).write_bytes(raw[off:off+n])
            source_commands.append('./raw-converter %s.source %s.source.etc %d %d %d %s' % (key, key, w, h, fmt, mode))
            results.append({'key': key, 'source_format': fmt, 'width': w, 'height': h,
                            'mode': mode, 'expected_sha256': digest(expected)})
            off += n
        if off != len(raw):
            raise ValueError('source mip lengths differ')
    (OUT / 'run.sh').write_text('#!/system/bin/sh\nset -e\ncd /data/local/tmp/openbonk-etcpak-all-source\n' + '\n'.join(source_commands) + '\n')
    (OUT / 'fixtures.json').write_text(json.dumps(results, indent=2) + '\n')
    print('generated', len(results), 'mip fixtures from', len({x['key'][:32] for x in results}), 'textures;', len(source_commands), 'raw-source conversions; private game pixels')

if __name__ == '__main__':
    main()
