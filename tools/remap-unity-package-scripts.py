#!/usr/bin/env python3
"""Remap exported package DLL script references to Unity package scripts.

For a prepared, imported, isolated Unity workspace only. The Steam export and
all overwritten local files are preserved separately. This mapping was compared
with the previous working project; gameplay equivalence is not established.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
GUID = re.compile(rb'(?m)^guid:\s*([0-9a-f]{32})\s*$')
SCRIPT = re.compile(rb'(m_Script: \{fileID: )(-?\d+)(, guid: )([0-9a-f]{32})(, type: \d+\})')
PACKAGE_DLLS = ('Newtonsoft.Json.dll', 'Unity.AI.Navigation.dll', 'Unity.Addressables.dll',
    'Unity.Localization.dll', 'Unity.Mathematics.dll', 'Unity.Postprocessing.Runtime.dll',
    'Unity.ProBuilder.KdTree.dll', 'Unity.ProBuilder.Poly2Tri.dll', 'Unity.ProBuilder.dll',
    'Unity.ResourceManager.dll', 'Unity.Splines.dll', 'Unity.TextMeshPro.dll',
    'Unity.VisualScripting.Core.dll', 'UnityEngine.UI.dll')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', required=True, type=Path)
    p.add_argument('--backup', required=True, type=Path, help='new local directory outside the workspace')
    p.add_argument('--check', action='store_true')
    a = p.parse_args()
    project = a.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        p.error('project must be an isolated assembled workspace')
    if (project / 'Temp/UnityLockfile').exists():
        p.error('workspace has a Unity lock; close Unity first')
    backup = a.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir():
        p.error('backup must be a new path in an existing directory')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project) or project.is_relative_to(backup):
        p.error('backup must be outside the workspace')
    source_map = json.loads((Path(__file__).resolve().parent / 'unity-package-script-map.json').read_text())
    plugin_backup = project / '.openbonk-original-plugins/Assets/Plugins'
    source_guids = {}
    for dll in PACKAGE_DLLS:
        meta = plugin_backup / (dll + '.meta')
        if not meta.is_file() or meta.is_symlink():
            p.error('missing original package DLL metadata: ' + dll)
        match = GUID.search(meta.read_bytes())
        if not match or match.group(1) in source_guids:
            p.error('invalid or duplicate package DLL GUID: ' + dll)
        source_guids[match.group(1)] = dll
    cache = project / 'Library/PackageCache'
    if not cache.is_dir():
        p.error('Unity packages are not imported; import the workspace first')
    target_guids = {guid.encode() for mapping in source_map.values() for guid in mapping.values()}
    found = {}
    for root in (project / 'Assets', cache):
        for meta in root.rglob('*.meta'):
            if meta.is_symlink() or not meta.is_file():
                continue
            m = GUID.search(meta.read_bytes())
            if m and m.group(1) in target_guids:
                found.setdefault(m.group(1), []).append(meta)
    if set(found) != target_guids or any(len(paths) != 1 for paths in found.values()):
        p.error('target Unity package script GUIDs are missing or duplicated')
    updates = {}
    counts = [0]
    for path in (project / 'Assets').rglob('*'):
        if not path.is_file() or path.suffix not in ('.asset', '.prefab', '.unity', '.mat'):
            continue
        if path.is_symlink() or not path.resolve().is_relative_to(project):
            p.error('asset paths must remain inside workspace without symlinks')
        data = path.read_bytes()
        def replace(m):
            name = source_guids.get(m.group(4))
            if name is None:
                return m.group(0)
            target = source_map.get(name, {}).get(m.group(2).decode())
            if target is None:
                raise ValueError('unmapped package script in ' + str(path.relative_to(project)) + ': ' + name + ' fileID ' + m.group(2).decode())
            counts[0] += 1
            return m.group(1) + b'11500000' + m.group(3) + target.encode() + m.group(5)
        try:
            changed = SCRIPT.sub(replace, data)
        except ValueError as error:
            p.error(str(error))
        if changed != data:
            updates[path] = changed
    if not updates:
        p.error('no package script references found')
    print(f'{counts[0]} references in {len(updates)} local files can be remapped.', flush=True)
    if a.check:
        print('Check only; workspace unchanged.')
        return
    backup.mkdir(mode=0o700)
    saved = []
    applied = []
    try:
        for path in updates:
            relative = path.relative_to(project)
            dest = backup / relative
            dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, dest)
            with dest.open('rb') as f:
                digest = hashlib.file_digest(f, 'sha256').hexdigest()
            with path.open('rb') as f:
                if digest != hashlib.file_digest(f, 'sha256').hexdigest():
                    raise RuntimeError('backup verification failed: ' + str(relative))
            saved.append({'path': relative.as_posix(), 'sha256': digest})
        (backup / 'manifest.json').write_text(json.dumps({'schema': 1, 'files': saved}, indent=2) + '\n')
        for path, data in updates.items():
            applied.append(path)
            path.write_bytes(data)
    except BaseException:
        for path in reversed(applied):
            shutil.copy2(backup / path.relative_to(project), path)
        raise
    print('Package script references remapped; local originals are backed up.')


if __name__ == '__main__':
    main()
