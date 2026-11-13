#!/usr/bin/env python3
"""Private experiment replacing one scene in a rebased research OBB."""
import copy
import hashlib
from pathlib import Path
import re
import sys
import zipfile

PINS = ('62f6d020ae63e126dd0e9b0396883fda4f90612e9d22970a7b0fcc209289988c',
        '3153534a93e8021a9f0b3bc074a963def7bf623b68e697a0bbd80f1e3d2a8db5')
NAMES = {'assets/bin/Data/level1.split0', 'assets/bin/Data/level1.split1'}

def digest(path):
    with path.open('rb') as file: return hashlib.file_digest(file, 'sha256').hexdigest()

def main():
    if len(sys.argv) not in (4, 5) or (len(sys.argv) == 5 and sys.argv[4] != '--all-scenes'):
        raise ValueError('usage: SCRIPT REBASED_OBB NEW_RUNTIME_OBB NEW_UNALIGNED_OBB [--all-scenes]')
    original, rebuilt, output = map(Path, sys.argv[1:4])
    for file, expected in zip((original, rebuilt), PINS):
        if not file.is_file() or digest(file) != expected: raise ValueError('unexpected input')
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('output must be new')
    try:
        with zipfile.ZipFile(original) as src, zipfile.ZipFile(rebuilt) as new:
            if any(len(z.namelist()) != len(set(z.namelist())) for z in (src, new)): raise ValueError('duplicate entries')
            names = NAMES
            if len(sys.argv) == 5:
                names = {n for n in src.namelist() if re.fullmatch(r'assets/bin/Data/level[1-4](\.split\d+)?', n)}
                if len(names) != 10 or names != {n for n in new.namelist() if re.fullmatch(r'assets/bin/Data/level[1-4](\.split\d+)?', n)}:
                    raise ValueError('unexpected scene inventory')
            else:
                for z in (src, new):
                    if {n for n in z.namelist() if n.startswith('assets/bin/Data/level1')} != names:
                        raise ValueError('unexpected scene inventory')
            with zipfile.ZipFile(output, 'x', allowZip64=True) as out:
                for entry in src.infolist():
                    out.writestr(copy.copy(entry), new.read(entry.filename) if entry.filename in names else src.read(entry))
            with zipfile.ZipFile(output) as result:
                if result.namelist() != src.namelist(): raise ValueError('inventory differs')
                for name in src.namelist():
                    expected = new.read(name) if name in names else src.read(name)
                    if hashlib.sha256(result.read(name)).digest() != hashlib.sha256(expected).digest(): raise ValueError('entry differs: ' + name)
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    print('Staged private scene compatibility probe; not distributable.')

if __name__ == '__main__': main()
