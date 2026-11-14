#!/usr/bin/env python3
"""Restore the beta's Android instancing setting for selected materials.

Only a marked, isolated workspace is modified. Original materials and backups
contain game data and remain local. Rendering and device performance still
require validation.
"""
import argparse
import json
import os
from pathlib import Path
import re
import tempfile


MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
FLAG = re.compile(rb'^  m_EnableInstancingVariants: ([01])$', re.MULTILINE)
STANDARD = b'  m_Shader: {fileID: 46, guid: 0000000000000000f000000000000000, type: 0}'
TOON = b'  m_Shader: {fileID: 4800000, guid: 1a1e6dd8cc2999049a34c8ba0497b2fb, type: 3}'
WARNING = b'  m_Shader: {fileID: 4800000, guid: 6409dc852732daa40b743510bfbf1f72, type: 3}'
FLASH = b'  m_Shader: {fileID: 4800000, guid: 7be6901591daf10469b84b8886dff6e9, type: 3}'
SHADERS = {name: STANDARD for name in (
    'Black', 'm_Tree_Transparent', 'm_archFaded', 'm_fenceFaded', 'm_fence2Faded', 'm_ladderFaded', 'm_lightPostFaded',
    'm_crypt_EdgesNew', 'm_crypt_FadedPillar', 'm_crypt_Floor', 'm_crypt_FloorPressurePlate',
    'm_crypt_Floor_Pillar', 'm_crypt_Frame', 'm_crypt_Walls', 'm_crypt_WallsBossRoom')}
SHADERS.update({name: TOON for name in (
    'm_boss', 'm_bossArrow', 'm_bossCoffin', 'm_bossPortal', 'm_chest', 'm_chestOpen', 'm_desertGrave',
    'm_encounter', 'm_encounterCharge', 'm_encounterGravestone', 'm_encounterGreed', 'm_frog1', 'm_frog2',
    'm_frog3', 'm_keyCrypt', 'm_player', 'm_portal', 'm_portalArrow', 'm_pot', 'm_unknown')})
SHADERS.update({'WarningField': WARNING, 'DamageFlash': FLASH})
NAMES = tuple(SHADERS)


def render(data, name):
    name_line = b'  m_Name: ' + name.encode('ascii')
    if data.splitlines().count(name_line) != 1 or data.splitlines().count(SHADERS[name]) != 1:
        raise ValueError('unsupported material or shader: ' + name)
    matches = list(FLAG.finditer(data))
    if len(matches) != 1: raise ValueError('unsupported instancing flag: ' + name)
    match = matches[0]
    return data[:match.start(1)] + b'0' + data[match.end(1):]


def materials(project):
    originals, updates = {}, {}
    directory = project / 'Assets/Material'
    if not directory.is_dir() or not directory.resolve().is_relative_to(project.resolve()):
        raise ValueError('material directory must be inside the workspace')
    for name in NAMES:
        path = directory / (name + '.mat')
        if path.is_symlink() or not path.is_file(): raise ValueError('material missing: ' + name)
        originals[path] = path.read_bytes()
        updates[path] = render(originals[path], name)
    return originals, updates


def verify(project):
    originals, updates = materials(project)
    if originals != updates: raise ValueError('materials require Android instancing preparation')


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
    try:
        originals, updates = materials(project)
    except (OSError, ValueError) as error:
        p.error(str(error))
    changed = [path for path in originals if originals[path] != updates[path]]
    if a.check:
        if changed: p.error('materials require Android instancing preparation')
        print('Material instancing settings are prepared.'); return
    if not changed:
        print('Material instancing settings are already prepared.'); return
    if not a.backup: p.error('a new backup directory is required')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir():
        p.error('backup must be a new directory outside the workspace')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project): p.error('backup must be outside the workspace')
    backup.mkdir(mode=0o700)
    for path in changed: (backup / path.name).write_bytes(originals[path])
    applied = []; temporary = None
    try:
        for path in changed:
            with tempfile.NamedTemporaryFile('wb', dir=path.parent, prefix='.openbonk-material-', delete=False) as stream:
                temporary = Path(stream.name); stream.write(updates[path]); stream.flush(); os.fsync(stream.fileno())
            os.chmod(temporary, path.stat().st_mode & 0o777)
            os.replace(temporary, path); temporary = None; applied.append(path)
    except BaseException:
        for path in applied: path.write_bytes(originals[path])
        raise
    finally:
        if temporary is not None: temporary.unlink(missing_ok=True)
    print(f'Prepared {len(changed)} materials; original assets are backed up locally.')


if __name__ == '__main__':
    main()
