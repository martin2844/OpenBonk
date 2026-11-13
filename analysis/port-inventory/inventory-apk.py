#!/usr/bin/env python3
"""Inventory an APK's native code, Unity boot assets and archive members.

Object names, assembly labels and counts do not establish provenance or rights.
"""
import collections
import hashlib
import json
from pathlib import Path
import re
import sys
import tempfile
import zipfile

import UnityPy

if len(sys.argv) != 2:
    raise SystemExit('usage: inventory-apk.py APK')
apk = Path(sys.argv[1])
h = hashlib.sha256()
with apk.open('rb') as stream:
    for block in iter(lambda: stream.read(1024 * 1024), b''):
        h.update(block)
print('APK', str(apk), 'sha256', h.hexdigest())
with tempfile.TemporaryDirectory(prefix='openbonk-apk-inventory-') as temp, zipfile.ZipFile(apk) as z:
    names = z.namelist()
    counts = collections.Counter()
    for info in z.infolist():
        name = info.filename
        category = ('unity-data' if name.startswith('assets/bin/Data/') else
                    'native-lib' if name.startswith('lib/') else
                    'dex' if re.fullmatch(r'classes\d*\.dex', name) else
                    'android-resources' if name.startswith('res/') else 'other')
        counts[(category, 'entries')] += 1
        counts[(category, 'bytes')] += info.file_size
        if category in ('native-lib', 'dex'):
            print('CODE', json.dumps({'path': name, 'bytes': info.file_size,
                                      'sha256': hashlib.sha256(z.read(info)).hexdigest()}))
    for category in ('unity-data', 'native-lib', 'dex', 'android-resources', 'other'):
        print('CATEGORY', category, 'entries', counts[(category, 'entries')], 'uncompressed_bytes', counts[(category, 'bytes')])
    for stem in ('globalgamemanagers.assets', 'globalgamemanagers', 'level0'):
        base = 'assets/bin/Data/' + stem
        parts = sorted((n for n in names if re.fullmatch(re.escape(base) + r'\.split\d+', n)),
                       key=lambda n: int(n.rsplit('split', 1)[1]))
        if not parts and base in names:
            parts = [base]
        if not parts:
            print('UNITY_FILE_MISSING', stem)
            continue
        path = Path(temp) / stem
        with path.open('wb') as output:
            for part in parts:
                output.write(z.read(part))
        types, assemblies, named, errors = collections.Counter(), collections.Counter(), [], []
        try:
            for obj in UnityPy.load(str(path)).objects:
                types[obj.type.name] += 1
                if obj.type.name not in ('MonoScript', 'Texture2D', 'AudioClip', 'Mesh', 'Font', 'TextAsset'):
                    continue
                try:
                    item = obj.read()
                    if obj.type.name == 'MonoScript':
                        assemblies[getattr(item, 'm_AssemblyName', '<unknown>')] += 1
                    else:
                        named.append((obj.type.name, getattr(item, 'm_Name', ''), obj.path_id))
                except Exception as exc:
                    errors.append((obj.type.name, obj.path_id, type(exc).__name__))
        except Exception as exc:
            errors.append(('<file>', None, type(exc).__name__))
        print('UNITY_FILE', json.dumps({'file': stem, 'bytes': path.stat().st_size, 'types': types,
                                         'script_assemblies': assemblies, 'named_assets': named,
                                         'errors': errors}, ensure_ascii=False))
print('LIMIT: archive and selected Unity boot files only; does not inspect executable code, all resources, licenses, or rights.')
