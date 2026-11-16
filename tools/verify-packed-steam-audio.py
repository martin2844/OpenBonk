#!/usr/bin/env python3
"""Verify a local packed-audio experiment against both input archives.

Optionally decode both versions to compare basic WAV properties. This does not
establish perceptual equivalence or make the generated OBB redistributable.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
from pathlib import Path
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('audio_inspect', ROOT / 'tools/inspect-packed-steam-audio.py')
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)
spec = importlib.util.spec_from_file_location('packed_audio', ROOT / 'tools/experimental-packed-steam-audio.py')
packed_audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(packed_audio)


def normalize_video_offsets(before, after, old_resource, new_resource, group, prior, current):
    """Validate relocated video payloads before normalizing their offsets."""
    if prior.keys() != current.keys():
        raise ValueError('video objects changed')
    normalized = bytearray(after)
    for key, old_obj in prior.items():
        new_obj = current[key]
        old = old_obj.read_typetree()['m_ExternalResources']
        new = new_obj.read_typetree()['m_ExternalResources']
        if old['m_Source'] != new['m_Source'] or old['m_Size'] != new['m_Size']:
            raise ValueError('video resource metadata changed')
        if not old['m_Source'] and not old['m_Size']:
            continue
        source, size = old['m_Source'], old['m_Size']
        old_offset, new_offset = old['m_Offset'], new['m_Offset']
        if (source != group + '.resource' or type(size) is not int or size <= 0 or
                type(old_offset) is not int or type(new_offset) is not int or
                old_offset < 0 or new_offset < 0 or old_offset + size > len(old_resource) or
                new_offset + size > len(new_resource)):
            raise ValueError('video resource range is invalid')
        if old_resource[old_offset:old_offset + size] != new_resource[new_offset:new_offset + size]:
            raise ValueError('video payload changed')
        old_field = old_obj.byte_start + packed_audio.video_offset_field(old_obj.get_raw_data(), source, old_offset, size)
        new_field = new_obj.byte_start + packed_audio.video_offset_field(new_obj.get_raw_data(), source, new_offset, size)
        if old_field != new_field:
            raise ValueError('video object layout changed')
        normalized[new_field:new_field + 8] = before[old_field:old_field + 8]
    return normalized


def wave_properties(data, channels, frequency):
    import fmod_toolkit
    import io
    import wave
    decoded = fmod_toolkit.raw_to_wav(data, 'clip', channels, frequency)
    if len(decoded) != 1:
        raise ValueError('expected one decoded sample')
    with wave.open(io.BytesIO(next(iter(decoded.values()))), 'rb') as sample:
        return sample.getnchannels(), sample.getframerate(), sample.getsampwidth(), sample.getnframes()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--original', required=True, type=Path)
    parser.add_argument('--replacement', required=True, type=Path)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--decode', action='store_true')
    args = parser.parse_args()
    source = args.steam_data.resolve(strict=True)
    clips = defaultdict(list)
    for group in audio.GROUPS:
        for obj in UnityPy.load(str(source / (group + '.assets'))).objects:
            if obj.type.name == 'AudioClip':
                clip = obj.read()
                clips[clip.m_Name].append(clip)
    matched = unmatched = decoded = 0
    with zipfile.ZipFile(args.original) as before, zipfile.ZipFile(args.replacement) as after:
        names = before.namelist()
        if len(names) != len(set(names)) or names != after.namelist():
            raise ValueError('archive entries differ or are duplicated')
        modified = set()
        for group in audio.GROUPS:
            a = audio.split_data(before, group)
            b = audio.split_data(after, group)
            if len(a) != len(b):
                raise ValueError('asset length changed')
            old_objects = UnityPy.load(a).objects
            new_objects = UnityPy.load(b).objects
            prior = {o.path_id: o for o in old_objects if o.type.name == 'AudioClip'}
            current = {o.path_id: o for o in new_objects if o.type.name == 'AudioClip'}
            if prior.keys() != current.keys():
                raise ValueError('audio objects changed')
            old_resource = before.read(audio.PREFIX + group + '.resource')
            new_resource = after.read(audio.PREFIX + group + '.resource')
            normalized = normalize_video_offsets(a, b, old_resource, new_resource, group,
                {o.path_id: o for o in old_objects if o.type.name == 'VideoClip'},
                {o.path_id: o for o in new_objects if o.type.name == 'VideoClip'})
            for key, obj in prior.items():
                old, new = obj.read(), current[key].read()
                x, y = old.m_Resource, new.m_Resource
                if old.m_Name != new.m_Name or not audio.compatible(old, new) or x.m_Source != y.m_Source:
                    raise ValueError('clip metadata changed')
                if not x.m_Source or not x.m_Size:
                    if x != y:
                        raise ValueError('inline audio metadata changed')
                    continue
                if (y.m_Offset < 0 or y.m_Size <= 0 or y.m_Offset + y.m_Size > len(new_resource) or
                        x.m_Offset < 0 or x.m_Offset + x.m_Size > len(old_resource)):
                    raise ValueError('audio range out of bounds')
                body = obj.get_raw_data()
                start = obj.byte_start + len(body) - 20
                normalized[start:start + 16] = a[start:start + 16]
                old_payload = old_resource[x.m_Offset:x.m_Offset + x.m_Size]
                new_payload = new_resource[y.m_Offset:y.m_Offset + y.m_Size]
                candidates = [c for c in clips[old.m_Name] if audio.compatible(old, c)]
                if len(candidates) > 1:
                    raise ValueError('ambiguous Steam clip')
                if not candidates:
                    if old_payload != new_payload:
                        raise ValueError('unmatched audio changed')
                    unmatched += 1
                    continue
                ref = candidates[0].m_Resource
                path = Path(ref.m_Source)
                if path.name != str(path) or path.name not in {g + '.resource' for g in audio.GROUPS}:
                    raise ValueError('unexpected Steam resource')
                with (source / path).open('rb') as stream:
                    stream.seek(ref.m_Offset)
                    expected = stream.read(ref.m_Size)
                if new_payload != expected or audio.fsb_codec(old_payload) != audio.fsb_codec(new_payload):
                    raise ValueError('replacement differs from Steam or codec changed')
                if args.decode:
                    if wave_properties(old_payload, old.m_Channels, old.m_Frequency) != wave_properties(new_payload, new.m_Channels, new.m_Frequency):
                        raise ValueError('decoded WAV properties differ')
                    decoded += 1
                matched += 1
            if normalized != a:
                raise ValueError('other serialized data changed')
            prefix = audio.PREFIX + group + '.assets.split'
            modified.update(n for n in names if n.startswith(prefix))
            modified.add(audio.PREFIX + group + '.resource')
        if not matched:
            raise ValueError('no audio replacements found')
        for name in names:
            if name not in modified:
                with before.open(name) as x, after.open(name) as y:
                    if hashlib.file_digest(x, 'sha256').digest() != hashlib.file_digest(y, 'sha256').digest():
                        raise ValueError('unrelated archive entry changed')
    print(f'{matched} replaced clips verified; {unmatched} unmatched clips preserved; {decoded} pairs decoded.')
    print(f'All {len(names) - len(modified)} unrelated archive entries match.')


if __name__ == '__main__':
    main()
