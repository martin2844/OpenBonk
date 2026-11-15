#!/usr/bin/env python3
"""Restore the beta's RootFx prefab scale in a marked local Unity workspace.

Only the root transform is changed. Game-derived files and backups stay local.
The attack scripts can alter scale at runtime; this does not prove visual parity.
"""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
PREFAB = Path('Assets/GameObject/RootFx.prefab')
HEADER = re.compile(rb'(?m)^--- !u!(\d+) &(\d+)\r?\n')
SCALE = re.compile(rb'(?m)^  m_LocalScale: \{x: ([46]), y: \1, z: \1\}$')


def render(data):
    headers = list(HEADER.finditer(data))
    sections = [(h.group(1), h.group(2), h.start(), headers[i + 1].start() if i + 1 < len(headers) else len(data))
                for i, h in enumerate(headers)]
    roots = [(identity, start, end) for kind, identity, start, end in sections
             if kind == b'1' and b'  m_Name: RootFx' in data[start:end].splitlines()]
    if len(roots) != 1:
        raise ValueError('expected exactly one RootFx GameObject')
    root_id, root_start, root_end = roots[0]
    transforms = [(identity, start, end) for kind, identity, start, end in sections
                  if kind == b'4' and b'  m_GameObject: {fileID: ' + root_id + b'}' in data[start:end].splitlines()]
    if len(transforms) != 1:
        raise ValueError('expected exactly one RootFx transform')
    transform_id, start, end = transforms[0]
    root, transform = data[root_start:root_end], data[start:end]
    if (b'  - component: {fileID: ' + transform_id + b'}' not in root.splitlines() or
            b'  m_Father: {fileID: 0}' not in transform.splitlines()):
        raise ValueError('RootFx transform is not the prefab root')
    scales = list(SCALE.finditer(transform))
    if len(scales) != 1:
        raise ValueError('unexpected RootFx root scale')
    match = scales[0]
    return data[:start + match.start()] + b'  m_LocalScale: {x: 4, y: 4, z: 4}' + data[start + match.end():]


def prefab(project):
    path = project / PREFAB
    if path.is_symlink() or not path.is_file() or not path.resolve().is_relative_to(project.resolve()):
        raise ValueError('RootFx prefab missing or outside workspace')
    return path


def verify(project):
    path = prefab(project)
    original = path.read_bytes()
    if render(original) != original:
        raise ValueError('RootFx prefab requires beta scale preparation')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--backup', type=Path, help='new backup file outside the workspace')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        parser.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists():
        parser.error('Unity workspace is locked')
    try:
        path = prefab(project)
        original = path.read_bytes()
        changed = render(original)
    except (OSError, ValueError) as error:
        parser.error(str(error))
    if args.check:
        if changed != original:
            parser.error('RootFx prefab requires beta scale preparation')
        print('RootFx prefab is prepared.')
        return
    if changed == original:
        print('RootFx prefab is already prepared.')
        return
    if not args.backup:
        parser.error('a new backup file is required')
    backup = args.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir():
        parser.error('backup must be a new file outside the workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project):
        parser.error('backup must be outside the workspace')
    fd = os.open(backup, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(fd, 'wb') as stream:
        stream.write(original)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-rootfx-', delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(changed)
        os.chmod(temporary, path.stat().st_mode & 0o777)
        os.replace(temporary, path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
    print('Prepared RootFx prefab; original backed up locally.')


if __name__ == '__main__':
    main()
