#!/usr/bin/env python3
"""Remove a bounded set of icon pixels for a local reconstruction experiment.

The result still contains other game content and is not a distribution artifact.
"""
import hashlib
from pathlib import Path
import shutil
import sys

import UnityPy

if len(sys.argv) != 3:
    raise SystemExit('usage: make-icon-template-prototype.py AUDIO_RETARGETED_DIR OUTPUT_DIR')
source, output = map(Path, sys.argv[1:])
assets, resource = source / 'sharedassets1.assets', source / 'sharedassets1.resource'
def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as src:
        for block in iter(lambda: src.read(1024 * 1024), b''): h.update(block)
    return h.hexdigest()
for path, expected in ((assets, '5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'),
                       (resource, '93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6')):
    if not path.is_file() or digest(path) != expected: raise SystemExit(f'unsupported input: {path}')
if output.exists(): raise SystemExit('output directory already exists')
env = UnityPy.load(str(assets)); changed = []
for obj in env.objects:
    if obj.type.name != 'Texture2D': continue
    tex = obj.read()
    if not tex.m_Name.lower().startswith('item') or tex.m_Name == 'ItemGhost' or (tex.m_Width, tex.m_Height) != (32, 32): continue
    if (tex.m_TextureFormat, tex.m_MipCount, tex.m_ImageCount, tex.m_ColorSpace) != (4, 1, 1, 1) or len(tex.image_data) != 4096:
        raise SystemExit(f'unsupported target icon: {tex.m_Name}')
    tex.image_data = bytes(4096); tex.save(); changed.append(tex.m_Name)
if len(changed) != 74: raise SystemExit(f'unexpected icon count: {len(changed)}')
output.mkdir(parents=True)
try:
    (output / 'sharedassets1.assets').write_bytes(env.file.save())
    shutil.copyfile(resource, output / 'sharedassets1.resource')
    print('Cleared',len(changed),'icons; template retains other game data and is not for distribution')
    print('assets_sha256',digest(output / 'sharedassets1.assets'))
except Exception:
    for path in output.iterdir(): path.unlink()
    output.rmdir(); raise
