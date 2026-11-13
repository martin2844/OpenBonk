#!/usr/bin/env python3
"""Locally source compatible RGBA item icons from one pinned PC build.

Experimental: requires the cleared-icon template and PC data; output
contains game data, is version-specific, and is not a distributable release.
"""
import collections
import hashlib
from pathlib import Path
import shutil
import sys

import UnityPy

if len(sys.argv) != 4:
    raise SystemExit('usage: retarget-pc-icons-prototype.py PC_DATA_DIR CLEARED_ICON_TEMPLATE_DIR OUTPUT_DIR')
pc, target, output = map(Path, sys.argv[1:])
inputs = {
    pc / 'sharedassets1.assets': 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
    pc / 'sharedassets1.assets.resS': '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea',
    target / 'sharedassets1.assets': '2559370af20541274d818b08522edcb631435d18470014ba005648e4544554fa',
    target / 'sharedassets1.resource': '93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6',
}
if output.exists():
    raise SystemExit('output directory already exists')
def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as src:
        for block in iter(lambda: src.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()
for path, expected in inputs.items():
    if not path.is_file() or digest(path) != expected:
        raise SystemExit(f'missing or unsupported input: {path}')

sources = collections.defaultdict(list)
for obj in UnityPy.load(str(pc / 'sharedassets1.assets')).objects:
    if obj.type.name == 'Texture2D':
        tex = obj.read(); sources[(tex.m_Name, tex.m_Width, tex.m_Height)].append(tex)
env = UnityPy.load(str(target / 'sharedassets1.assets'))
changed, skipped = [], []
for obj in env.objects:
    if obj.type.name != 'Texture2D':
        continue
    tex = obj.read()
    if not tex.m_Name.lower().startswith('item') or (tex.m_Width, tex.m_Height) != (32, 32):
        continue
    matches = sources[(tex.m_Name, 32, 32)]
    if len(matches) != 1:
        raise SystemExit(f'missing or ambiguous source: {tex.m_Name}')
    source = matches[0]
    if (source.m_TextureFormat, tex.m_TextureFormat, source.m_MipCount, tex.m_MipCount) != (4, 4, 1, 1):
        skipped.append(tex.m_Name); continue
    if source.m_ColorSpace != tex.m_ColorSpace or source.m_ImageCount != 1 or tex.m_ImageCount != 1:
        raise SystemExit(f'incompatible texture metadata: {tex.m_Name}')
    raw = source.get_image_data()
    if len(raw) != 32 * 32 * 4:
        raise SystemExit(f'unexpected RGBA data size: {tex.m_Name}')
    tex.image_data = raw
    tex.m_CompleteImageSize = len(raw)
    if tex.m_StreamData is not None:
        tex.m_StreamData.path = ''; tex.m_StreamData.offset = 0; tex.m_StreamData.size = 0
    tex.save(); changed.append(tex.m_Name)
if len(changed) != 74 or skipped != ['ItemGhost']:
    raise SystemExit(f'unexpected texture set: {len(changed)} changed, skipped={skipped}')
output.mkdir(parents=True)
try:
    (output / 'sharedassets1.assets').write_bytes(env.file.save())
    shutil.copyfile(target / 'sharedassets1.resource', output / 'sharedassets1.resource')
    print('Locally sourced', len(changed), 'RGBA item icons; skipped:', ', '.join(skipped))
    for name in ('sharedassets1.assets', 'sharedassets1.resource'):
        path = output / name
        print(name, path.stat().st_size, digest(path))
except Exception:
    for path in output.iterdir(): path.unlink()
    output.rmdir()
    raise
