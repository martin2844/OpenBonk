#!/usr/bin/env python3
"""Local Unity script-reference experiment, restricted to hashed OBB assets.

This is not an installer and is not distributable. Scenes, shared assets and
other references are deliberately left alone; identity does not prove layout.
"""
import collections
import copy
import importlib.util
import re
import struct
import sys
import zipfile
from pathlib import Path

import UnityPy

spec = importlib.util.spec_from_file_location('audit', Path(__file__).with_name('audit-runtime-script-references.py'))
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)

def translate(data, mapping):
    env = UnityPy.load(data)
    file = env.file
    if file is None or not hasattr(file, 'externals'): raise ValueError('not a serialized file')
    external = [x.path.rsplit('/', 1)[-1] for x in file.externals]
    def translate_id(index, path_id):
        if not path_id or index <= 0 or index > len(external) or external[index - 1] != 'globalgamemanagers.assets': return path_id
        if path_id not in mapping: raise ValueError('unknown or ambiguous script')
        return mapping[path_id]
    metadata_expected, counts = [], collections.Counter()
    for record in file.script_types:
        old = record.local_identifier_in_file
        new = translate_id(record.local_serialized_file_index, old)
        if old != new: counts['metadata'] += 1
        record.local_identifier_in_file = new
        metadata_expected.append((record.local_serialized_file_index, new))
    expected = {}
    for obj in env.objects:
        raw = obj.get_raw_data()
        if obj.type.name == 'MonoBehaviour':
            if len(raw) < 28 or obj.reader.endian != '<': raise ValueError('unsupported MonoBehaviour')
            index, old = struct.unpack_from('<iq', raw, 16)
            new = translate_id(index, old)
            if old != new:
                edited = bytearray(raw)
                struct.pack_into('<q', edited, 20, new)
                raw = bytes(edited)
                obj.set_raw_data(raw)
                counts['behaviour'] += 1
        if obj.path_id in expected: raise ValueError('duplicate object id')
        expected[obj.path_id] = (obj.type.name, raw)
    if not counts: return data, counts
    output = file.save()
    check = UnityPy.load(output)
    if [(r.local_serialized_file_index, r.local_identifier_in_file) for r in check.file.script_types] != metadata_expected:
        raise ValueError('rewritten metadata differs')
    if [x.path for x in check.file.externals] != [x.path for x in file.externals]: raise ValueError('external references differ')
    actual = {obj.path_id: (obj.type.name, obj.get_raw_data()) for obj in check.objects}
    if actual != expected: raise ValueError('rewritten object data differs')
    return output, counts

def main():
    if len(sys.argv) != 5: raise ValueError('usage: SCRIPT OLD_APK NEW_APK OBB NEW_UNALIGNED_OBB')
    old_apk, new_apk, obb, output = map(Path, sys.argv[1:])
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    for path, expected in zip((old_apk, new_apk, obb), audit.PINS):
        if not path.is_file() or audit.digest(path) != expected: raise ValueError('unexpected input')
    old, old_names = audit.script_index(old_apk)
    _, new_names = audit.script_index(new_apk)
    if any(len(v) != 1 for v in old_names.values()): raise ValueError('ambiguous old script identity')
    mapping = {}
    for key, identity in old.items():
        candidates = new_names.get(identity, [])
        if len(candidates) != 1: raise ValueError('missing or ambiguous new script identity')
        mapping[key] = candidates[0]
    counters = collections.Counter()
    try:
        with zipfile.ZipFile(obb) as source, zipfile.ZipFile(output, 'x', allowZip64=True) as dest:
            infos = source.infolist()
            if len(infos) != len({i.filename for i in infos}): raise ValueError('duplicate entries')
            for info in infos:
                data = source.read(info)
                if re.fullmatch(r'assets/bin/Data/[0-9a-f]{32}', info.filename):
                    if len(data) > 64 * 1024 * 1024: raise ValueError('asset too large')
                    data, count = translate(data, mapping)
                    counters.update(count)
                    counters['assets'] += 1
                    if count: counters['modified_assets'] += 1
                dest.writestr(copy.copy(info), data)
        if counters['assets'] != 713 or counters['metadata'] != 300 or counters['behaviour'] != 310:
            raise ValueError('unexpected scope or counts')
        with zipfile.ZipFile(obb) as source, zipfile.ZipFile(output) as dest:
            if source.namelist() != dest.namelist() or source.read('unity_obb_guid') != dest.read('unity_obb_guid'):
                raise ValueError('OBB inventory or GUID differs')
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    print(dict(counters))
    print('Unaligned local research OBB; does not cover scenes or shared assets and is not distributable.')

if __name__ == '__main__': main()
