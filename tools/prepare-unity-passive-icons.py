#!/usr/bin/env python3
"""Correct two ambiguous passive icon references in an isolated Unity export.

The selected PNG hashes describe the known local export, not redistributable
assets. The original asset files and the backup remain local.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
ICONS = {
    'Bullseye': (0, 'd1b2238ef95186e9d12b1d4ec4af0a6be21548684f71f8dade57bda6cb1ac74a', 'f0046587aa51d5b7c8e7dca740ed7172b058d55b374b40c8e7c7b6b55c74d586'),
    'Stonks': (18, '51f499814803ccd3b6f8083f061c999ed4b46b8e0fe3492cfbe10217646f5f85', '19b55d7f8cf8bbdd479306f87938629384eaed8d564e1adfe56d713a2754929a'),
}
GUID = re.compile(rb'^guid: ([0-9a-f]{32})$', re.MULTILINE)
ICON = re.compile(rb'^  icon: \{fileID: 2800000, guid: ([0-9a-f]{32}), type: 3\}$', re.MULTILINE)


def regular(path):
    if path.is_symlink() or not path.is_file(): raise ValueError('missing regular file: ' + str(path))
    return path.read_bytes()


def prepare(project):
    assets = project / 'Assets'
    if assets.is_symlink() or not assets.is_dir(): raise ValueError('invalid Assets directory')
    originals, updates = {}, {}
    for name, (passive, old_hash, new_hash) in ICONS.items():
        guids = []
        for suffix, expected in (('', old_hash), ('_0', new_hash)):
            png = assets / 'Texture2D' / (name + suffix + '.png')
            if hashlib.sha256(regular(png)).hexdigest() != expected: raise ValueError('unexpected texture: ' + name + suffix)
            matches = GUID.findall(regular(Path(str(png) + '.meta')))
            if len(matches) != 1: raise ValueError('invalid texture GUID: ' + name + suffix)
            guids.append(matches[0])
        if guids[0] == guids[1]: raise ValueError('ambiguous texture GUIDs: ' + name)
        path = assets / 'MonoBehaviour' / (name + '.asset')
        data = regular(path)
        if data.splitlines().count(b'  m_Name: ' + name.encode()) != 1 or data.splitlines().count(b'  ePassive: ' + str(passive).encode()) != 1:
            raise ValueError('unexpected passive asset: ' + name)
        matches = list(ICON.finditer(data))
        if len(matches) != 1 or matches[0].group(1) not in guids: raise ValueError('unexpected passive icon: ' + name)
        match = matches[0]; originals[path] = data
        updates[path] = data[:match.start(1)] + guids[1] + data[match.end(1):]
    return originals, updates


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--backup', type=Path)
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if marker.is_symlink() or not marker.is_file() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if (project / 'Temp/UnityLockfile').exists(): p.error('Unity workspace is locked')
    try: originals, updates = prepare(project)
    except (OSError, ValueError) as error: p.error(str(error))
    changed = [path for path in originals if originals[path] != updates[path]]
    if a.check:
        if changed: p.error('passive icon references require preparation')
        print('Passive icon references are prepared.'); return
    if not changed:
        print('Passive icon references are already prepared.'); return
    if not a.backup: p.error('a new backup directory is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir(): p.error('backup must be a new directory')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project) or project.is_relative_to(backup): p.error('backup must be outside the workspace')
    backup.mkdir(mode=0o700)
    for path in changed: (backup / path.name).write_bytes(originals[path])
    applied = []; temporary = None
    try:
        for path in changed:
            with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-icon-', delete=False) as stream:
                temporary = Path(stream.name); stream.write(updates[path]); stream.flush(); os.fsync(stream.fileno())
            os.chmod(temporary, path.stat().st_mode & 0o777)
            os.replace(temporary, path); temporary = None; applied.append(path)
    except BaseException:
        for path in applied: path.write_bytes(originals[path])
        raise
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print(f'Prepared {len(changed)} passive icon references; original assets are backed up locally.')


if __name__ == '__main__': main()
