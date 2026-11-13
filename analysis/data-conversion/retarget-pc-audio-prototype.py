#!/usr/bin/env python3
"""Locally repoint one known Android assets file to matching PC FSB audio.

Research prototype: outputs contain original game data and are not a release package.
Requires UnityPy; it has not been validated for arbitrary versions or devices.
"""
import collections
import hashlib
from pathlib import Path
import sys

import UnityPy

if len(sys.argv) != 4:
    raise SystemExit('usage: retarget-pc-audio-prototype.py PC_DATA_DIR ANDROID_ASSETS OUTPUT_DIR')
pc, android, output = map(Path, sys.argv[1:])
source_assets = pc / 'sharedassets1.assets'
source_audio = pc / 'sharedassets1.resource'
if not pc.is_dir() or not android.is_file() or not source_assets.is_file() or not source_audio.is_file():
    raise SystemExit('missing input files')
if output.exists():
    raise SystemExit('output directory already exists')

def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as src:
        for chunk in iter(lambda: src.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()

pins = {
    source_assets: 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
    source_audio: '28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c',
    android: '83915af30962ec09457c70b8bc681875e0a6e5e50a193c58ea1d25f37da60cf5',
}
for path, expected in pins.items():
    if digest(path) != expected:
        raise SystemExit(f'unsupported source version: {path.name}')

original = collections.defaultdict(list)
for obj in UnityPy.load(str(source_assets)).objects:
    if obj.type.name == 'AudioClip':
        clip = obj.read(); original[clip.m_Name].append(clip)
env = UnityPy.load(str(android))
targets = []
attributes = ('m_Length', 'm_Channels', 'm_Frequency', 'm_BitsPerSample', 'm_LoadType',
              'm_CompressionFormat', 'm_SubsoundIndex')
for obj in env.objects:
    if obj.type.name != 'AudioClip':
        continue
    clip = obj.read(); matches = original.get(clip.m_Name, [])
    if len(matches) != 1:
        raise SystemExit(f'missing or ambiguous original audio: {clip.m_Name}')
    source = matches[0]
    if any(getattr(clip, k, None) != getattr(source, k, None) for k in attributes):
        raise SystemExit(f'incompatible original audio metadata: {clip.m_Name}')
    resource = source.m_Resource
    if resource.m_Source != 'sharedassets1.resource' or resource.m_Size <= 0 or resource.m_Offset + resource.m_Size > source_audio.stat().st_size:
        raise SystemExit(f'invalid original audio stream: {clip.m_Name}')
    targets.append((clip, resource.m_Offset, resource.m_Size))
if len(targets) != 118:
    raise SystemExit(f'unsupported Android asset version: expected 118 clips, found {len(targets)}')

output.mkdir(parents=True)
try:
    with source_audio.open('rb') as src, (output / 'sharedassets1.resource').open('xb') as dst:
        for clip, offset, size in targets:
            dst.write(b'\0' * (-dst.tell() % 32))
            dest_offset = dst.tell()
            src.seek(offset); data = src.read(size)
            if len(data) != size or not data.startswith(b'FSB5'):
                raise ValueError(f'invalid or incomplete FSB: {clip.m_Name}')
            dst.write(data)
            clip.m_Resource.m_Offset = dest_offset
            clip.m_Resource.m_Size = size
            clip.m_Resource.m_Source = 'sharedassets1.resource'
            clip.save()
    (output / 'sharedassets1.assets').write_bytes(env.file.save())
    print('Retargeted', len(targets), 'clips from local PC data. Experimental output is not for distribution.')
    for name in ('sharedassets1.assets', 'sharedassets1.resource'):
        path = output / name
        print(name, path.stat().st_size, digest(path))
except Exception:
    for path in output.iterdir():
        path.unlink()
    output.rmdir()
    raise
