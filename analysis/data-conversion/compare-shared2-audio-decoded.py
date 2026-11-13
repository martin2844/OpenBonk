#!/usr/bin/env python3
"""Compare 124 locally retargeted audio clips without exporting decoded audio."""
import argparse
import importlib.util
import json
from pathlib import Path
import zipfile
import UnityPy

spec = importlib.util.spec_from_file_location('audio_compare', Path(__file__).with_name('compare-hashed-audio-decoded.py'))
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)
PINS = {
    'obb': 'baedab6cc8e293494eeaa7a5ab515d9cdc92d1492c4dada88125a16c806c891e',
    'assets': 'af9ac35192083879374d917614c9e2163e4bcd2d23fc6c604359c05e5d56c010',
    'resource': '6abc02d2702824b936add3ea2a6780b5b10e2954e1df7543b8cc79b5c9e1b1bc',
}

def clips(data):
    result = {}
    for obj in UnityPy.load(data).objects:
        if obj.type.name == 'AudioClip':
            clip = obj.read()
            if clip.m_Name in result: raise ValueError('duplicate clip name')
            result[clip.m_Name] = clip
    return result

def stream(resource, clip):
    ref = clip.m_Resource
    if ref.m_Source != 'sharedassets2.resource' or ref.m_Offset < 0 or ref.m_Size <= 0 or ref.m_Offset + ref.m_Size > len(resource):
        raise ValueError('invalid audio resource reference')
    return resource[ref.m_Offset:ref.m_Offset + ref.m_Size]

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('obb', type=Path); p.add_argument('retargeted', type=Path); p.add_argument('output', type=Path)
a = p.parse_args()
if a.output.exists() or not a.output.parent.is_dir(): p.error('output must be new and parent must exist')
paths = {'obb': a.obb, 'assets': a.retargeted / 'sharedassets2.assets', 'resource': a.retargeted / 'sharedassets2.resource'}
for label, path in paths.items():
    if audio.sha(path) != PINS[label]: p.error('incompatible input: ' + label)
with zipfile.ZipFile(a.obb) as z:
    prefix = 'assets/bin/Data/sharedassets2.assets.split'
    parts = sorted((n for n in z.namelist() if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
    if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))): raise ValueError('invalid asset fragments')
    old_clips = clips(b''.join(z.read(n) for n in parts))
    old_resource = z.read('assets/bin/Data/sharedassets2.resource')
new_clips = clips(paths['assets'].read_bytes())
new_resource = paths['resource'].read_bytes()
if len(old_clips) != 124 or set(old_clips) != set(new_clips): raise ValueError('unexpected audio clip inventory')
rows = []
for name in sorted(old_clips):
    old, new = old_clips[name], new_clips[name]
    try:
        attrs = ('m_Length', 'm_Channels', 'm_Frequency', 'm_BitsPerSample', 'm_LoadType', 'm_CompressionFormat', 'm_SubsoundIndex')
        if any(getattr(old, k, None) != getattr(new, k, None) for k in attrs): raise ValueError('clip metadata differs')
        row = {'name': name}
        row.update(audio.compare(stream(old_resource, old), stream(new_resource, new), old))
    except Exception as e: row = {'name': name, 'error': str(e)}
    rows.append(row)
    print(len(rows), name, row.get('error', 'decoded'), flush=True)
result = {'inputs_sha256': PINS, 'clips': rows, 'limitations': 'Offline FMOD decode; sampled correlation does not establish Android playback or perceptual equivalence.'}
with a.output.open('x') as f: json.dump(result, f, indent=2); f.write('\n')
print('decoded', sum('error' not in r for r in rows), 'of', len(rows))
if any('error' in r for r in rows): raise SystemExit(1)
