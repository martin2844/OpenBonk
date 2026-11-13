#!/usr/bin/env python3
"""Compare decoded Android and Steam streams for the local audio experiment.

The decoded samples and source files are not exported. Correlation is a simple
same-position measurement, not proof of perceptual or in-game equivalence.
"""
import argparse
from array import array
import hashlib
import io
import json
import math
from pathlib import Path
import sys
import wave
import zipfile

import fmod_toolkit
import UnityPy

PINNED_OBB = 'bada4b92f779ed21d3288f17d14e9821c64a617750ddf7b64dd7fc3cf261cd60'

def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1048576), b''):
            h.update(chunk)
    return h.hexdigest()

def decode(data, clip):
    decoded = fmod_toolkit.raw_to_wav(data, 'clip', clip.m_Channels, clip.m_Frequency)
    if len(decoded) != 1:
        raise ValueError('expected one decoded subsound')
    with wave.open(io.BytesIO(next(iter(decoded.values())))) as w:
        channels, rate, width, frames = w.getnchannels(), w.getframerate(), w.getsampwidth(), w.getnframes()
        if width != 2 or channels < 1 or frames < 1:
            raise ValueError('unsupported decoded PCM')
        values = array('h', w.readframes(frames))
        if sys.byteorder != 'little': values.byteswap()
        return channels, rate, frames, values

def compare(old, new, clip):
    ac, ar, af, av = decode(old, clip)
    bc, br, bf, bv = decode(new, clip)
    row = {'android': [ac, ar, af], 'steam': [bc, br, bf]}
    if (ac, ar) == (bc, br):
        # Equal timestamps, every eighth frame, retaining each channel.
        x, y = array('h'), array('h')
        for channel in range(ac):
            x.extend(av[channel::ac*8]); y.extend(bv[channel::ac*8])
        count = min(len(x), len(y))
        xx = sum(v*v for v in x[:count]); yy = sum(v*v for v in y[:count])
        xy = sum(x[i]*y[i] for i in range(count))
        row['samples_compared'] = count
        row['correlation'] = round(xy / math.sqrt(xx*yy), 6) if xx and yy else None
    return row

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('obb', type=Path)
    p.add_argument('restored', type=Path)
    p.add_argument('manifest', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or not a.output.parent.is_dir(): p.error('output must be new and parent must exist')
    if sha(a.obb) != PINNED_OBB: p.error('incompatible source OBB')
    manifest = json.loads(a.manifest.read_text())
    clips = manifest['clips']
    if manifest['version'] != 1 or len(clips) != 63 or len({c['file'] for c in clips}) != 63:
        p.error('unexpected manifest')
    rows = []
    with zipfile.ZipFile(a.obb) as z:
        for item in clips:
            try:
                name = item['file']
                if len(name) != 32 or any(c not in '0123456789abcdef' for c in name): raise ValueError('invalid file name')
                old = z.read('assets/bin/Data/' + name + '.resource')
                new_path = a.restored / (name + '.resource')
                if hashlib.sha256(old).hexdigest() != item['original_resource_sha256'] or sha(new_path) != item['resource_sha256']:
                    raise ValueError('resource hash mismatch')
                objects = UnityPy.load(z.read('assets/bin/Data/' + name)).objects
                if len(objects) != 1 or objects[0].type.name != 'AudioClip': raise ValueError('unexpected object')
                clip = objects[0].read()
                if clip.m_Name != item['name']: raise ValueError('unexpected clip name')
                row = {'name': clip.m_Name, 'file': name}
                row.update(compare(old, new_path.read_bytes(), clip))
                rows.append(row)
            except Exception as e:
                rows.append({'name': item.get('name'), 'file': item.get('file'), 'error': str(e)})
            print(len(rows), item.get('name'), rows[-1].get('error', 'decoded'), flush=True)
    result = {'source_obb_sha256': PINNED_OBB, 'clips': rows,
              'limitations': 'Offline FMOD decode; same-position correlation is not an Android playback or perceptual test.'}
    with a.output.open('x') as f: json.dump(result, f, indent=2); f.write('\n')
    print('decoded', sum('error' not in r for r in rows), 'of', len(rows))
    if any('error' in r for r in rows): raise SystemExit(1)

if __name__ == "__main__":
    main()
