#!/usr/bin/env python3
"""Local experiment: point a new Unity runtime at an existing private OBB GUID."""
import hashlib
import copy
from pathlib import Path
import sys
import zipfile

APK_SHA = 'f50aff5a9af4ae78968d4c56e721e961ae69c94e21cf012715fe07e6cfd8b260'
OBB_SHA = '632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29'
APK_GUID = 'assets/unity_obb_guid'
OBB_GUID = 'unity_obb_guid'
BASE_SHA = '0bf818e3978f4363623931d5636eee73a9ac5458b6aa52e0458ebfd9495043d9'
REGISTRY = {'assets/bin/Data/globalgamemanagers'} | {
    f'assets/bin/Data/globalgamemanagers.assets.split{i}' for i in range(4)
}

def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()

def unique(archive):
    entries = archive.namelist()
    if len(entries) != len(set(entries)): raise ValueError('duplicate entries')

def main():
    if len(sys.argv) not in (4, 5): raise ValueError('usage: SCRIPT APK OBB NEW_UNSIGNED_APK [PREVIOUS_BASE_APK]')
    apk, obb, output = map(Path, sys.argv[1:4])
    if not apk.is_file() or digest(apk) not in {APK_SHA, '5543620d74804f198d8c864dc45962c83b97d82787f13ea21bd7b3f93b19c37f'} or not obb.is_file() or digest(obb) != OBB_SHA:
        raise ValueError('input APK or OBB differs from pinned research pair')
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    replacements = {}
    if len(sys.argv) == 5:
        base = Path(sys.argv[4])
        if not base.is_file() or digest(base) != BASE_SHA: raise ValueError('previous base differs')
        with zipfile.ZipFile(base) as previous:
            unique(previous)
            for name in REGISTRY: replacements[name] = previous.read(name)
    with zipfile.ZipFile(apk) as source, zipfile.ZipFile(obb) as data:
        unique(source); unique(data)
        old, guid = source.read(APK_GUID), data.read(OBB_GUID)
        if len(guid) != 36 or len(old) != 36 or guid == old: raise ValueError('unexpected Unity GUIDs')
        replacements[APK_GUID] = guid
        try:
            with zipfile.ZipFile(output, 'x', allowZip64=True) as out:
                for entry in source.infolist():
                    if entry.filename.startswith('META-INF/'): continue
                    out.writestr(copy.copy(entry), replacements[entry.filename] if entry.filename in replacements else source.read(entry))
            with zipfile.ZipFile(output) as check:
                unique(check)
                if check.read(APK_GUID) != guid: raise ValueError('output GUID differs')
                expected = {n for n in source.namelist() if not n.startswith('META-INF/')}
                if set(check.namelist()) != expected: raise ValueError('output inventory differs')
                for name, content in replacements.items():
                    if check.read(name) != content: raise ValueError('output replacement differs: ' + name)
                for name in sorted(expected - replacements.keys()):
                    if check.read(name) != source.read(name): raise ValueError('unexpected modified entry: ' + name)
        except BaseException:
            output.unlink(missing_ok=True)
            raise
    print('Staged private runtime/OBB compatibility experiment; not a release.')

if __name__ == '__main__': main()
