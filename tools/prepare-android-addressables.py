#!/usr/bin/env python3
"""Back up exported Steam Addressables catalogs that conflict with Android builds.

Only works in an assembled local workspace. Export the Steam localization
bundles and rebuild Android Addressables first; this does not replace them.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
FILES = ('catalog.json', 'catalog.json.meta', 'settings.json', 'settings.json.meta')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path)
    parser.add_argument('--backup', required=True, type=Path, help='new local directory outside the workspace')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        parser.error('project must be an isolated assembled workspace')
    if (project / 'Temp/UnityLockfile').exists():
        parser.error('workspace has a Unity lock; close Unity before modifying it')
    backup = args.backup.expanduser().absolute()
    if backup.exists() or backup.is_symlink() or not backup.parent.is_dir():
        parser.error('backup must be a new path in an existing directory')
    backup = backup.parent.resolve() / backup.name
    if backup.is_relative_to(project) or project.is_relative_to(backup):
        parser.error('backup must be outside the workspace')
    base = project / 'Assets/StreamingAssets/aa'
    if not base.is_dir() or not base.resolve().is_relative_to(project):
        parser.error('Addressables directory is missing or outside workspace')
    sources = [base / name for name in FILES]
    if any(p.is_symlink() or not p.is_file() for p in sources):
        parser.error('expected exported catalog, settings and both metadata files')
    # Require successful local reconstruction, not merely a raw export.
    generated = project / 'Library/com.unity.addressables/aa/Android'
    if not all((generated / name).is_file() for name in ('catalog.json', 'settings.json')):
        parser.error('build Android Addressables before removing the exported catalogs')
    print('Exported Steam catalogs can be backed up locally.', flush=True)
    if args.check:
        print('Check only; workspace unchanged.')
        return
    backup.mkdir(mode=0o700)
    saved = []
    removed = []
    try:
        for source in sources:
            destination = backup / source.name
            shutil.copy2(source, destination)
            with destination.open('rb') as f:
                digest = hashlib.file_digest(f, 'sha256').hexdigest()
            with source.open('rb') as f:
                if digest != hashlib.file_digest(f, 'sha256').hexdigest():
                    raise RuntimeError('backup verification failed: ' + source.name)
            saved.append({'name': source.name, 'sha256': digest})
        (backup / 'manifest.json').write_text(json.dumps({'schema': 1, 'files': saved}, indent=2) + '\n')
        for source in sources:
            source.unlink()
            removed.append(source)
    except BaseException:
        for source in removed:
            shutil.copy2(backup / source.name, source)
        raise
    print('Original catalogs and metadata backed up; Android build can use generated catalogs.')


if __name__ == '__main__':
    main()
