#!/usr/bin/env python3
"""Experimental Linux -> Android retarget for one *string-table* bundle.

Uses UnityPy 1.25.4; this is a prototype for this game's 2023.2.22f1 data,
not a general AssetBundle converter or Android launcher implementation.
"""
import argparse
import collections
import os
from pathlib import Path
import re
import tempfile
import zipfile
import UnityPy

PREFIX = 'localization-string-tables-'
SUFFIX = '_assets_all.bundle'
LIMIT = 8 * 1024 * 1024

def valid_name(name):
    return ((name == 'localization-assets-shared_assets_all.bundle' or (name.startswith(PREFIX) and name.endswith(SUFFIX)))
            and re.fullmatch(r'[A-Za-z0-9_().-]+', name) is not None)

def source_bytes(source, name):
    if source.suffix.lower() != '.zip':
        if source.name != name:
            raise ValueError('input bundle basename must match --bundle')
        if source.stat().st_size > LIMIT:
            raise ValueError('bundle too large')
        return source.read_bytes()
    expected = 'StreamingAssets/aa/StandaloneLinux64/' + name
    with zipfile.ZipFile(source) as archive:
        if len(archive.infolist()) > 20000:
            raise ValueError('too many ZIP entries')
        matches = []
        for info in archive.infolist():
            path = info.filename
            if path.startswith('/') or '\\' in path or ':' in path or any(p in ('.', '..') for p in path.split('/')):
                raise ValueError('unsafe ZIP path')
            if path == expected or path == 'Megabonk_Data/' + expected or path.endswith('/Megabonk_Data/' + expected):
                matches.append(info)
        if len(matches) != 1:
            raise ValueError('expected exactly one matching bundle')
        if matches[0].file_size <= 0 or matches[0].file_size > LIMIT:
            raise ValueError('bundle missing or too large')
        with archive.open(matches[0]) as inp:
            data = inp.read(LIMIT + 1)
        if len(data) != matches[0].file_size:
            raise ValueError('incomplete or oversized bundle')
        return data

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--bundle', required=True, help='original Linux localization string-table or shared-data bundle basename')
    args = parser.parse_args()
    if not valid_name(args.bundle):
        raise ValueError('invalid string-table bundle name')
    if args.input.resolve() == args.output.resolve():
        raise ValueError('output must differ from input')
    data = source_bytes(args.input, args.bundle)
    env = UnityPy.load(data)
    if len(env.files) != 1:
        raise ValueError('expected exactly one bundle')
    bundle = next(iter(env.files.values()))
    if type(bundle).__name__ != 'BundleFile' or len(bundle.files) != 1:
        raise ValueError('unexpected bundle structure')
    serialized = next(iter(bundle.files.values()))
    if type(serialized).__name__ != 'SerializedFile' or serialized.unity_version != '2023.2.22f1' or serialized._m_target_platform != 24:
        raise ValueError('unsupported Unity version or source platform')
    kinds = collections.Counter(obj.type.name for obj in serialized.objects.values())
    if not kinds.get('MonoBehaviour') or kinds.get('AssetBundle') != 1 or set(kinds) - {'MonoBehaviour', 'MonoScript', 'AssetBundle'}:
        raise ValueError('unexpected asset types; refusing general conversion')
    serialized._m_target_platform = 13
    serialized.mark_changed()
    result = bundle.save(packer='lz4')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    temp_name = None
    try:
        with tempfile.NamedTemporaryFile(dir=args.output.parent, prefix='.retarget-', delete=False) as temp:
            temp_name = temp.name
            temp.write(result)
        os.replace(temp_name, args.output)
    finally:
        if temp_name and os.path.exists(temp_name):
            os.unlink(temp_name)
    print(f'Converted {args.bundle}: {len(data)} -> {len(result)} bytes; objects={dict(kinds)}')

if __name__ == '__main__':
    main()
