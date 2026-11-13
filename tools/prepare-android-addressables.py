#!/usr/bin/env python3
"""Back up exported Steam Addressables files that conflict with Android builds.

Only works in an assembled local workspace. Export the Steam localization
bundles and rebuild Android Addressables first; this does not replace them.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}


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
    # Require successful local reconstruction, not merely a raw export.
    generated = project / 'Library/com.unity.addressables/aa/Android'
    if not all((generated / name).is_file() for name in ('catalog.json', 'settings.json')):
        parser.error('build Android Addressables before removing the exported catalogs')
    sources = []
    for generated_file in sorted(generated.rglob('*')):
        if not generated_file.is_file():
            continue
        if generated_file.is_symlink() or not generated_file.resolve().is_relative_to(generated.resolve()):
            parser.error('generated Addressables must remain inside their directory')
        relative = generated_file.relative_to(generated)
        source = base / relative
        if source.exists() or source.is_symlink():
            if source.is_symlink() or not source.is_file() or not source.resolve().is_relative_to(base.resolve()):
                parser.error('exported Addressables must be regular local files')
            sources.append(source)
            meta = source.with_name(source.name + '.meta')
            if meta.exists() or meta.is_symlink():
                if meta.is_symlink() or not meta.is_file() or not meta.resolve().is_relative_to(base.resolve()):
                    parser.error('exported Addressables metadata must be a regular local file')
                sources.append(meta)
    if not sources:
        parser.error('no conflicting exported Addressables files found')
    print(f'{len(sources)} conflicting exported files and metadata can be backed up locally.', flush=True)
    if args.check:
        print('Check only; workspace unchanged.')
        return
    backup.mkdir(mode=0o700)
    saved = []
    removed = []
    try:
        for source in sources:
            relative = source.relative_to(base)
            destination = backup / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
            with destination.open('rb') as f:
                digest = hashlib.file_digest(f, 'sha256').hexdigest()
            with source.open('rb') as f:
                if digest != hashlib.file_digest(f, 'sha256').hexdigest():
                    raise RuntimeError('backup verification failed: ' + source.name)
            saved.append({'path': relative.as_posix(), 'sha256': digest})
        (backup / 'manifest.json').write_text(json.dumps({'schema': 1, 'files': saved}, indent=2) + '\n')
        for source in sources:
            source.unlink()
            removed.append(source)
    except BaseException:
        for source in removed:
            shutil.copy2(backup / source.relative_to(base), source)
        raise
    print('Conflicting exported files backed up; Android build can use generated files.')


if __name__ == '__main__':
    main()
