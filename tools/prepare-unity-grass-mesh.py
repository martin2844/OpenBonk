#!/usr/bin/env python3
"""Keep the original grass mesh readable for the Android batched fallback.

Only an isolated workspace is modified. The original game-derived mesh and
the backup remain local. This does not verify device rendering.
"""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
MESH = Path('Assets/Mesh/GrassQuads.asset')
FLAG = re.compile(rb'^  m_IsReadable: ([01])$', re.MULTILINE)


def render(data):
    if (len(re.findall(rb'^  m_Name: GrassQuads$', data, re.MULTILINE)) != 1 or
            len(re.findall(rb'^    m_VertexCount: 8$', data, re.MULTILINE)) != 1 or
            len(re.findall(rb'^  m_IndexBuffer: [0-9a-fA-F]+$', data, re.MULTILINE)) != 1):
        raise ValueError('unsupported GrassQuads asset layout')
    matches = list(FLAG.finditer(data))
    if len(matches) != 1: raise ValueError('unsupported GrassQuads readability flag')
    match = matches[0]
    return data[:match.start(1)] + b'1' + data[match.end(1):]


def verify(project):
    path = project / MESH
    if path.is_symlink() or not path.is_file(): raise ValueError('GrassQuads asset missing')
    data = path.read_bytes()
    if render(data) != data: raise ValueError('GrassQuads is not readable; run prepare-unity-grass-mesh.py')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--backup', type=Path, help='new backup file outside the workspace')
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    path = project / MESH
    if path.is_symlink() or not path.is_file(): p.error('GrassQuads asset missing')
    original = path.read_bytes(); updated = render(original)
    if a.check:
        if updated != original: p.error('GrassQuads is not readable')
        print('GrassQuads is configured as readable.'); return
    if updated == original:
        print('GrassQuads is already configured as readable.'); return
    if not a.backup: p.error('a new backup file is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be new and outside the workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project): p.error('backup must be outside the workspace')
    with backup.open('xb') as stream: stream.write(original)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-grass-', delete=False) as stream:
            temporary = Path(stream.name); stream.write(updated); stream.flush(); os.fsync(stream.fileno())
        os.chmod(temporary, path.stat().st_mode & 0o777)
        os.replace(temporary, path); temporary = None
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print('GrassQuads made readable; previous asset retained in the local backup.')


if __name__ == '__main__': main()
