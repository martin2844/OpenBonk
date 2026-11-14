#!/usr/bin/env python3
"""Restore the tested Android outline setting on the two Desert edge materials.

Only a marked, isolated workspace is modified. Game-derived materials and
backups stay local. This visual tradeoff does not establish PC parity.
"""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
NAMES = ('m_1_TerrainDesertEdge', 'm_3_TerrainDesertEdge')
SHADER = b'  m_Shader: {fileID: 4800000, guid: 0cba6c73e0961ed478ab9fd73c09052a, type: 3}'
FLOATS = re.compile(rb'(?ms)^    m_Floats:\n(.*?)(?=^    m_Colors:)')
SUPPRESS = re.compile(rb'^      _SuppressOutline: ([^\r\n]+)$', re.MULTILINE)


def render(data, name):
    if data.splitlines().count(b'  m_Name: ' + name.encode('ascii')) != 1 or data.splitlines().count(SHADER) != 1:
        raise ValueError('unexpected Desert edge material: ' + name)
    blocks = list(FLOATS.finditer(data))
    if len(blocks) != 1: raise ValueError('unexpected material float block: ' + name)
    block = blocks[0]; body = block.group(1); matches = list(SUPPRESS.finditer(body))
    if len(matches) > 1 or (matches and matches[0].group(1) not in (b'0', b'1')):
        raise ValueError('unexpected outline setting: ' + name)
    if matches:
        match = matches[0]; body = body[:match.start(1)] + b'1' + body[match.end(1):]
    else:
        body += b'      _SuppressOutline: 1\n'
    return data[:block.start(1)] + body + data[block.end(1):]


def materials(project):
    directory = project / 'Assets/Material'
    if not directory.is_dir() or not directory.resolve().is_relative_to(project.resolve()):
        raise ValueError('material directory must be inside the workspace')
    originals, updates = {}, {}
    for name in NAMES:
        path = directory / (name + '.mat')
        if path.is_symlink() or not path.is_file(): raise ValueError('material missing: ' + name)
        originals[path] = path.read_bytes(); updates[path] = render(originals[path], name)
    return originals, updates


def verify(project):
    originals, updates = materials(project)
    if originals != updates: raise ValueError('Desert edge materials require Android outline preparation')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--backup', type=Path, help='new backup directory outside the workspace')
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    try: originals, updates = materials(project)
    except (OSError, ValueError) as error: p.error(str(error))
    changed = [path for path in originals if originals[path] != updates[path]]
    if a.check:
        if changed: p.error('Desert edge materials require Android outline preparation')
        print('Desert edge materials are prepared.'); return
    if not changed:
        print('Desert edge materials are already prepared.'); return
    if not a.backup: p.error('a new backup directory is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be a new directory outside the workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project): p.error('backup must be outside the workspace')
    backup.mkdir(mode=0o700)
    for path in changed: (backup / path.name).write_bytes(originals[path])
    applied = []; temporary = None
    try:
        for path in changed:
            with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-edge-', delete=False) as stream:
                temporary = Path(stream.name); stream.write(updates[path]); stream.flush(); os.fsync(stream.fileno())
            os.chmod(temporary, path.stat().st_mode & 0o777)
            os.replace(temporary, path); temporary = None; applied.append(path)
    except BaseException:
        for path in applied: path.write_bytes(originals[path])
        raise
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print(f'Prepared {len(changed)} Desert edge materials; originals are backed up locally.')


if __name__ == '__main__': main()
