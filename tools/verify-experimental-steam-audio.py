#!/usr/bin/env python3
"""Locally verify the experimental Steam-audio OBB against its source inputs.

Requires UnityPy and fmod_toolkit. No audio is written or played. Matching
decoded properties do not establish audible or perceptual equivalence.
"""
import argparse
from collections import defaultdict
import hashlib
import io
from pathlib import Path
import re
import wave
import zipfile

import UnityPy
import fmod_toolkit


PREFIX = 'assets/bin/Data/'
FIELDS = ('m_Length', 'm_Channels', 'm_Frequency', 'm_BitsPerSample', 'm_LoadType',
          'm_CompressionFormat', 'm_SubsoundIndex')


def wave_properties(data, channels, frequency):
    decoded = fmod_toolkit.raw_to_wav(data, 'clip', channels, frequency)
    if len(decoded) != 1:
        raise ValueError('expected one decoded sample')
    with wave.open(io.BytesIO(next(iter(decoded.values()))), 'rb') as sample:
        properties = (sample.getnchannels(), sample.getframerate(), sample.getsampwidth(), sample.getnframes())
        if not any(sample.readframes(sample.getnframes())):
            raise ValueError('decoded audio is silent')
    return properties


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', type=Path, required=True)
    parser.add_argument('--original', type=Path, required=True)
    parser.add_argument('--replacement', type=Path, required=True)
    args = parser.parse_args()
    source = args.steam_data.resolve(strict=True)
    asset, resource = source / 'sharedassets1.assets', source / 'sharedassets1.resource'
    if any(not p.is_file() or p.is_symlink() for p in (asset, resource)):
        parser.error('Steam audio inputs must be regular files')
    clips = defaultdict(list)
    for obj in UnityPy.load(str(asset)).objects:
        if obj.type.name == 'AudioClip':
            clip = obj.read()
            clips[clip.m_Name].append(clip)
    matched = 0
    modified = set()
    with zipfile.ZipFile(args.original) as original, zipfile.ZipFile(args.replacement) as replacement, resource.open('rb') as steam:
        names = original.namelist()
        if len(names) != len(set(names)) or names != replacement.namelist():
            raise ValueError('archive entries differ or are duplicated')
        available = set(names)
        for name in names:
            if not re.fullmatch(re.escape(PREFIX) + r'[0-9a-f]{32}', name) or name + '.resource' not in available:
                continue
            before = [o.read() for o in UnityPy.load(original.read(name)).objects if o.type.name == 'AudioClip']
            after = [o.read() for o in UnityPy.load(replacement.read(name)).objects if o.type.name == 'AudioClip']
            if len(before) != 1 or len(after) != 1 or len(clips[before[0].m_Name]) != 1:
                raise ValueError('audio clip mapping is missing or ambiguous')
            a, b, pc = before[0], after[0], clips[before[0].m_Name][0]
            if b.m_Name != a.m_Name or any(getattr(x, key, None) != getattr(pc, key, None) for x in (a, b) for key in FIELDS):
                raise ValueError('audio clip metadata differs')
            ref = pc.m_Resource
            if ref.m_Source != 'sharedassets1.resource' or ref.m_Offset < 0 or ref.m_Size <= 0:
                raise ValueError('unsupported Steam resource')
            steam.seek(ref.m_Offset)
            expected = steam.read(ref.m_Size)
            output = replacement.read(name + '.resource')
            if expected != output or b.m_Resource.m_Size != len(output) or b.m_Resource.m_Offset != 0:
                raise ValueError('reconstructed audio differs from Steam')
            if wave_properties(original.read(name + '.resource'), a.m_Channels, a.m_Frequency) != wave_properties(output, b.m_Channels, b.m_Frequency):
                raise ValueError('decoded audio properties differ')
            modified.update((name, name + '.resource'))
            matched += 1
        if not matched:
            raise ValueError('no audio resources found')
        for name in names:
            if name in modified:
                continue
            with original.open(name) as a, replacement.open(name) as b:
                if hashlib.file_digest(a, 'sha256').digest() != hashlib.file_digest(b, 'sha256').digest():
                    raise ValueError('unrelated archive entry changed')
    print(f'{matched} audio pairs decode with matching properties; all other {len(names) - len(modified)} entries match.')
    print('This does not establish audible equivalence or a distributable build.')


if __name__ == '__main__':
    main()
