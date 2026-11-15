#!/usr/bin/env python3
"""Check the rebuilt Buffering3 video reference against the local source clip.

This checks one game's known video payload; it does not validate playback or
the provenance of the game data. The OBB and source clip must stay local.
"""
import argparse
import importlib.util
from pathlib import Path
import zipfile

import UnityPy

spec = importlib.util.spec_from_file_location('packed_audio_inspect', Path(__file__).with_name('inspect-packed-steam-audio.py'))
audio = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audio)


def check_payload(resource, offset, size, expected):
    if type(offset) is not int or type(size) is not int or offset < 0 or size <= 0 or offset + size > len(resource):
        raise ValueError('invalid video resource range')
    if resource[offset:offset + size] != expected:
        raise ValueError('video reference does not point to the source clip')


def verify(obb, clip):
    expected = clip.read_bytes()
    if not expected:
        raise ValueError('empty source clip')
    found = 0
    with zipfile.ZipFile(obb) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError('duplicate OBB entries')
        for group in audio.GROUPS:
            for obj in UnityPy.load(audio.split_data(archive, group)).objects:
                if obj.type.name != 'VideoClip':
                    continue
                video = obj.read_typetree()
                if video['m_Name'] != 'Buffering3':
                    raise ValueError('unexpected video clip')
                source = video['m_ExternalResources']
                name = source['m_Source']
                if name != group + '.resource':
                    raise ValueError('unexpected video resource')
                check_payload(archive.read(audio.PREFIX + name), source['m_Offset'], source['m_Size'], expected)
                found += 1
    if found != 1:
        raise ValueError('expected exactly one Buffering3 video clip')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--obb', type=Path, required=True)
    parser.add_argument('--clip', type=Path, required=True)
    args = parser.parse_args()
    try:
        if any(path.is_symlink() or not path.is_file() for path in (args.obb, args.clip)):
            raise ValueError('OBB and clip must be regular files')
        verify(args.obb, args.clip)
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error:
        parser.error(str(error))
    print('PASS: Buffering3 video reference resolves to the local source clip.')


if __name__ == '__main__':
    main()
