#!/usr/bin/env python3
"""Reconstruct a pinned Linux resource from Windows data for local research.

The output contains original game content and is not distributable. This
converts only one resource; full Windows game conversion remains incomplete.
"""
import argparse
import hashlib
import os
from pathlib import Path
import tempfile

SIZE = 60845700
WINDOWS_SHA = '29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d'
LINUX_SHA = '28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'
OFFSET = 60837212
OLD = b'\xb4'*7
NEW = b'p'*7


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('windows_data', type=Path, help='Windows Megabonk_Data directory')
    p.add_argument('output', type=Path, help='new output file')
    a = p.parse_args(); source = a.windows_data.resolve(strict=True); out = a.output.absolute()
    if not source.is_dir() or not out.parent.is_dir() or out.exists() or out.is_symlink(): p.error('input must exist and output must be new')
    if out.resolve().is_relative_to(source): p.error('output must be outside the game data directory')
    path = source/'sharedassets1.resource'
    if not path.is_file() or path.stat().st_size != SIZE: p.error('unsupported Windows resource')
    old_hash, new_hash = hashlib.sha256(), hashlib.sha256(); temporary = None
    try:
        with path.open('rb') as reader, tempfile.NamedTemporaryFile(prefix='.openbonk-resource-', dir=out.parent, delete=False) as writer:
            temporary = Path(writer.name); position = 0
            while True:
                chunk = reader.read(1024*1024)
                if not chunk: break
                old_hash.update(chunk); modified = bytearray(chunk)
                if position <= OFFSET and OFFSET+len(OLD) <= position+len(chunk):
                    offset = OFFSET-position
                    if modified[offset:offset+len(OLD)] != OLD: raise ValueError('unexpected video metadata')
                    modified[offset:offset+len(OLD)] = NEW
                new_hash.update(modified); writer.write(modified); position += len(chunk)
        if position != SIZE or old_hash.hexdigest() != WINDOWS_SHA or new_hash.hexdigest() != LINUX_SHA:
            raise ValueError('resource content changed or belongs to another version')
        os.link(temporary, out)
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print('Reconstructed sharedassets1.resource; local research only.')


if __name__ == '__main__': main()
