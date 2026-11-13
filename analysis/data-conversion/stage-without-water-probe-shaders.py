#!/usr/bin/env python3
"""Stage a private OBB experiment without three opt-in water probe shaders.

This intentionally bypasses the launcher's pinned OBB: it must only be run as a
separate diagnostic. ResourceManager still references these entries. A short
startup does not establish that removing them is safe for all game paths.
"""
import copy
import hashlib
from pathlib import Path
import sys
from zipfile import ZipFile

EXPECTED = '5acfe4f4b62c97e972e46525977375378cabdd09e33d5311f89bb637e2abb931'
REMOVED = {'assets/bin/Data/' + n for n in (
    'df272285d21fd527096b3c5d8a8c9adf',
    'baf39c96675258361b63538c61a6f54e',
    'f85e78285b9fb8553bc9aff72231fd4d')}

def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()

def main():
    if len(sys.argv) != 3: raise ValueError('usage: SCRIPT INPUT_OBB NEW_UNALIGNED_OBB')
    source, target = map(Path, sys.argv[1:])
    if not source.is_file() or sha(source) != EXPECTED: raise ValueError('unexpected input')
    if target.exists() or target.is_symlink() or not target.parent.is_dir(): raise ValueError('invalid output')
    try:
        with ZipFile(source) as inp, ZipFile(target, 'x', allowZip64=True) as out:
            entries = inp.infolist(); names = [e.filename for e in entries]
            if len(names) != 1081 or len(set(names)) != len(names) or not REMOVED <= set(names): raise ValueError('unexpected inventory')
            out.comment = inp.comment
            for entry in entries:
                if entry.is_dir() or entry.file_size > 64 * 1024 * 1024: raise ValueError('unexpected entry')
                if entry.filename not in REMOVED: out.writestr(copy.copy(entry), inp.read(entry))
        with ZipFile(source) as inp, ZipFile(target) as out:
            if set(out.namelist()) != set(inp.namelist()) - REMOVED: raise ValueError('inventory changed')
            for entry in out.infolist():
                old = inp.getinfo(entry.filename)
                if (entry.file_size, entry.CRC) != (old.file_size, old.CRC): raise ValueError('entry changed')
    except BaseException:
        target.unlink(missing_ok=True)
        raise
    print('Staged private experiment; align OBB and start Unity directly; not distributable.')

if __name__ == '__main__': main()
