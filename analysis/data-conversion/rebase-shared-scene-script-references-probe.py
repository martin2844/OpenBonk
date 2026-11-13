#!/usr/bin/env python3
"""Extend the private script-ID experiment to split Unity assets and scenes."""
import collections
import copy
import hashlib
import importlib.util
from pathlib import Path
import sys
import zipfile

here = Path(__file__).parent
spec = importlib.util.spec_from_file_location('probe', here / 'rebase-hashed-script-references-probe.py')
probe = importlib.util.module_from_spec(spec)
spec.loader.exec_module(probe)

HASHED_OBB_SHA = 'b2acf8ab62136ffb815769a4ba94e5b8056171f83c7f33c3ddadf0738da81905'
ROOT = 'assets/bin/Data/'
GROUPS = [ROOT + n for n in ('level1', 'level2', 'level3', 'level4',
          'sharedassets0.assets', 'sharedassets1.assets', 'sharedassets2.assets',
          'sharedassets3.assets', 'sharedassets4.assets')]

def parts_for(archive, base):
    names = set(archive.namelist())
    if base in names:
        if base + '.split0' in names: raise ValueError('ambiguous split asset')
        return [base]
    parts = []
    while base + '.split' + str(len(parts)) in names:
        parts.append(base + '.split' + str(len(parts)))
    actual = {n for n in names if n.startswith(base + '.split')}
    if not parts or set(parts) != actual: raise ValueError('missing or non-contiguous split asset: ' + base)
    return parts

def main():
    if len(sys.argv) != 5: raise ValueError('usage: SCRIPT OLD_APK NEW_APK HASHED_OBB NEW_UNALIGNED_OBB')
    old_apk, new_apk, obb, output = map(Path, sys.argv[1:])
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    for path, expected in zip((old_apk, new_apk, obb), (*probe.audit.PINS[:2], HASHED_OBB_SHA)):
        if not path.is_file() or probe.audit.digest(path) != expected: raise ValueError('unexpected input')
    old, old_names = probe.audit.script_index(old_apk)
    _, new_names = probe.audit.script_index(new_apk)
    if any(len(v) != 1 for v in old_names.values()): raise ValueError('ambiguous source script')
    mapping = {}
    for path_id, key in old.items():
        candidates = new_names.get(key, [])
        if len(candidates) != 1: raise ValueError('missing or ambiguous target script')
        mapping[path_id] = candidates[0]
    replacements, totals = {}, collections.Counter()
    try:
        with zipfile.ZipFile(obb) as source:
            if len(source.namelist()) != len(set(source.namelist())): raise ValueError('duplicate entries')
            for base in GROUPS:
                names = parts_for(source, base)
                chunks = [source.read(n) for n in names]
                original = b''.join(chunks)
                rewritten, counts = probe.translate(original, mapping)
                totals.update(counts)
                if counts:
                    offset = 0
                    for i, (name, chunk) in enumerate(zip(names, chunks)):
                        length = len(chunk) if i + 1 < len(names) else len(rewritten) - offset
                        if length <= 0 or (len(names) > 1 and length > 1024 * 1024):
                            raise ValueError('unexpected output split size: ' + base)
                        replacements[name] = rewritten[offset:offset + length]
                        offset += length
                    if offset != len(rewritten): raise ValueError('failed to split rewritten asset')
                print(base.rsplit('/', 1)[-1], dict(counts), 'bytes', len(original), '->', len(rewritten), flush=True)
            with zipfile.ZipFile(output, 'x', allowZip64=True) as dest:
                for entry in source.infolist():
                    data = replacements[entry.filename] if entry.filename in replacements else source.read(entry)
                    dest.writestr(copy.copy(entry), data)
        with zipfile.ZipFile(obb) as source, zipfile.ZipFile(output) as dest:
            if source.namelist() != dest.namelist(): raise ValueError('entry inventory differs')
            for name in source.namelist():
                if name in replacements:
                    if hashlib.sha256(dest.read(name)).digest() != hashlib.sha256(replacements[name]).digest(): raise ValueError('changed entry differs')
                elif hashlib.sha256(dest.read(name)).digest() != hashlib.sha256(source.read(name)).digest():
                    raise ValueError('unexpected entry change: ' + name)
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    print('totals', dict(totals), 'changed ZIP entries', len(replacements), flush=True)
    print('Private experimental unaligned OBB; identity mapping alone does not establish compatibility.')

if __name__ == '__main__': main()
