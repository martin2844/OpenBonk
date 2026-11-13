#!/usr/bin/env python3
"""Restore Android skybox shader references in an isolated Unity workspace.

Original materials and metadata are backed up locally before any changes.
Cubemap references and all other material settings remain unchanged.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
GUID = re.compile(rb'(?m)^guid:\s*([0-9a-f]{32})\s*$')
SHADER = re.compile(rb'(?m)^(  m_Shader: \{fileID: 4800000, guid: )([0-9a-f]{32})(, type: 3\})$')
NAMES = ('Desert1', 'Desert2', 'Desert3', 'Forest1', 'Forest2', 'Forest3', 'Graveyard', 'Hell')


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
    def shader_guid(name):
        path = project / 'Assets/Shader' / name
        meta = Path(str(path) + '.meta')
        if any(x.is_symlink() or not x.is_file() or not x.resolve().is_relative_to(project) for x in (path, meta)):
            p.error('required local shader or metadata is missing: ' + name)
        match = GUID.search(meta.read_bytes())
        if not match:
            p.error('invalid local shader GUID: ' + name)
        return match.group(1)
    source_guid = shader_guid('Skybox_Cubemap Extended.shader')
    target_guid = shader_guid('Native_Cubemap_Skybox.shader')
    if source_guid == target_guid:
        p.error('skybox shader GUIDs are not distinct')
    updates = {}
    for name in NAMES:
        asset = project / 'Assets/Material' / ('Skybox_' + name + '.mat')
        meta = Path(str(asset) + '.meta')
        if any(x.is_symlink() or not x.is_file() or not x.resolve().is_relative_to(project) for x in (asset, meta)):
            p.error('missing regular skybox material or metadata: ' + name)
        data = asset.read_bytes()
        matches = list(SHADER.finditer(data))
        if len(matches) != 1 or matches[0].group(2) != source_guid:
            p.error('unexpected skybox shader reference: ' + name)
        updates[asset] = SHADER.sub(lambda m: m.group(1) + target_guid + m.group(3), data)
    print('Eight local skybox materials can be restored.', flush=True)
    if a.check:
        print('Check only; workspace unchanged.')
        return
    backup.mkdir(mode=0o700)
    saved, applied = [], []
    try:
        for asset in updates:
            for path in (asset, Path(str(asset) + '.meta')):
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
        for asset, data in updates.items():
            applied.append(asset)
            asset.write_bytes(data)
    except BaseException:
        for asset in reversed(applied):
            shutil.copy2(backup / asset.relative_to(project), asset)
        raise
    print('Skybox shader references restored; originals remain in the local backup.')


if __name__ == '__main__':
    main()
