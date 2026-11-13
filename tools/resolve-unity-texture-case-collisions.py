#!/usr/bin/env python3
"""Resolve case-only exported texture filename collisions in a local workspace.

Unity treats asset paths case-insensitively. Rename the noncanonical copies and
their metadata together so serialized GUID references remain unchanged.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
GUID = re.compile(rb'(?m)^guid:\s*([0-9a-f]{32})\s*$')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--manifest', required=True, type=Path, help='new local manifest outside the workspace')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        parser.error('project must be an isolated assembled workspace')
    if (project / 'Temp/UnityLockfile').exists():
        parser.error('workspace has a Unity lock; close Unity first')
    manifest = args.manifest.expanduser().absolute()
    if manifest.exists() or manifest.is_symlink() or not manifest.parent.is_dir():
        parser.error('manifest must be a new path in an existing directory')
    manifest = manifest.parent.resolve() / manifest.name
    if manifest.is_relative_to(project):
        parser.error('manifest must be outside the workspace')
    assets = project / 'Assets'
    groups = {}
    for path in assets.rglob('*'):
        if path.is_symlink():
            parser.error('asset paths must not contain symbolic links')
        if path.is_file() and path.suffix != '.meta':
            groups.setdefault(path.relative_to(assets).as_posix().casefold(), []).append(path)
    existing = set(groups)
    changes = []
    for paths in groups.values():
        if len(paths) < 2:
            continue
        if any(p.parent != paths[0].parent or p.parent != assets / 'Texture2D' or p.suffix.lower() != '.png' for p in paths):
            parser.error('unsupported asset path collision; inspect the project manually')
        for source in sorted(paths, key=lambda p: p.name)[1:]:
            meta = Path(str(source) + '.meta')
            if meta.is_symlink() or not meta.is_file():
                parser.error('colliding texture is missing regular metadata')
            match = GUID.search(meta.read_bytes())
            if not match:
                parser.error('invalid texture GUID in metadata')
            guid = match.group(1).decode()
            target = source.with_name(source.stem + '__openbonk_' + guid[:8] + source.suffix)
            target_meta = Path(str(target) + '.meta')
            key = target.relative_to(assets).as_posix().casefold()
            if key in existing or target.exists() or target.is_symlink() or target_meta.exists() or target_meta.is_symlink():
                parser.error('deterministic renamed path already exists')
            existing.add(key)
            with source.open('rb') as f:
                sha = hashlib.file_digest(f, 'sha256').hexdigest()
            changes.append((source, meta, target, target_meta, guid, sha))
    if not changes:
        parser.error('no supported texture case collisions found')
    print(f'{len(changes)} textures and their metadata can be renamed locally.', flush=True)
    if args.check:
        print('Check only; workspace unchanged.')
        return
    rows = [{'from': a.relative_to(project).as_posix(), 'to': c.relative_to(project).as_posix(),
             'guid': guid, 'sha256': sha} for a, b, c, d, guid, sha in changes]
    # Exclusive creation avoids overwriting another report, even on failure.
    with manifest.open('x', encoding='utf-8') as f:
        json.dump({'schema': 1, 'renames': rows}, f, indent=2)
        f.write('\n')
    moved = []
    try:
        for source, meta, target, target_meta, _, _ in changes:
            source.rename(target)
            moved.append((source, target))
            meta.rename(target_meta)
            moved.append((meta, target_meta))
    except BaseException:
        for original, renamed in reversed(moved):
            renamed.rename(original)
        raise
    print('Renamed local textures with GUIDs preserved; original export was not modified.')


if __name__ == '__main__':
    main()
