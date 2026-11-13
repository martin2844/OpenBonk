#!/usr/bin/env python3
"""Install locally generated plugins in an isolated OpenBonk Unity workspace.

Backs up every replaced or removed file within that workspace. This is a
preparation stage, not a complete APK build or a source of game assets.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil


ROOT = Path(__file__).resolve().parent.parent
OVERLAY = ROOT / 'unity/OpenBonk'
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
GUID = re.compile(rb'^guid:\s*([0-9a-fA-F]{32})\s*$', re.MULTILINE)
PLUGINS = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')
# These names duplicate Unity packages in the 1.3.14 Steam export. The
# established reconstructed workspace uses the packages instead of these DLLs.
PACKAGE_DLLS = ('Newtonsoft.Json.dll', 'Unity.AI.Navigation.dll', 'Unity.Addressables.dll',
    'Unity.Localization.dll', 'Unity.Mathematics.dll', 'Unity.Postprocessing.Runtime.dll',
    'Unity.ProBuilder.KdTree.dll', 'Unity.ProBuilder.Poly2Tri.dll', 'Unity.ProBuilder.dll',
    'Unity.ResourceManager.dll', 'Unity.Splines.dll', 'Unity.TextMeshPro.dll',
    'Unity.VisualScripting.Core.dll', 'UnityEngine.UI.dll')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path, help='workspace created by assemble-unity-workspace.py')
    parser.add_argument('--plugins', required=True, type=Path, help='output of prepare-managed-plugins.py')
    parser.add_argument('--lua-library', required=True, type=Path, help='locally built ARM64 libmegabonk_lua.so')
    parser.add_argument('--remove-duplicate-package-dlls', action='store_true', help='back up and remove known Unity package duplicates')
    parser.add_argument('--check', action='store_true', help='validate inputs without writing files')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    plugins = args.plugins.expanduser().resolve(strict=True)
    lua_path = args.lua_library.expanduser().absolute()
    if lua_path.is_symlink():
        parser.error('Lua library must not be a symbolic link')
    lua = lua_path.resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        parser.error('project must be an isolated assembled workspace')
    if (project / 'Temp/UnityLockfile').exists():
        parser.error('workspace has a Unity lock file; close Unity and inspect it before retrying')
    backup = project / '.openbonk-original-plugins'
    if backup.exists() or backup.is_symlink():
        parser.error('backup directory already exists; preserve or restore it before retrying')
    if not lua.is_file() or lua.is_symlink():
        parser.error('local Lua library is missing')
    for name in PLUGINS:
        if not (plugins / name).is_file() or (plugins / name).is_symlink():
            parser.error('local managed plugin is missing: ' + name)
    targets = {Path('Assets/Plugins') / name: plugins / name for name in PLUGINS}
    targets[Path('Assets/Plugins/Android/com.rlabrecque.steamworks.net.dll')] = plugins / PLUGINS[2]
    targets[Path('Assets/Plugins/Android/arm64-v8a/libmegabonk_lua.so')] = lua
    for binary in list(targets):
        meta = Path(str(binary) + '.meta')
        reference = OVERLAY / meta
        if meta.parts[2] == 'Android' and not reference.is_file():
            parser.error('source metadata missing: ' + str(meta))
        if (project / meta).exists() and reference.is_file():
            original, expected = GUID.search((project / meta).read_bytes()), GUID.search(reference.read_bytes())
            if not original or not expected or original.group(1).lower() != expected.group(1).lower():
                parser.error('plugin metadata GUID does not match: ' + str(meta))
        if not (project / meta).exists() and reference.is_file():
            targets[meta] = OVERLAY / meta
    removed = []
    if args.remove_duplicate_package_dlls:
        for name in PACKAGE_DLLS:
            for suffix in ('', '.meta'):
                relative = Path('Assets/Plugins') / (name + suffix)
                if (project / relative).exists():
                    removed.append(relative)
    changes = list(targets) + removed
    if len(changes) != len(set(changes)):
        parser.error('overlapping plugin operations')
    for relative in changes:
        path = project / relative
        if not path.parent.resolve().is_relative_to(project) or path.is_symlink():
            parser.error('plugin paths must remain inside the workspace without symbolic links')
        if path.exists() and not path.is_file():
            parser.error('plugin destination is not a regular file: ' + str(relative))
    print(f'Prepared inputs found; {len(removed)} duplicate files would be backed up and removed.', flush=True)
    if args.check:
        print('Check only; workspace unchanged.')
        return
    backup.mkdir(mode=0o700)
    existing = []
    applied = []
    try:
        for relative in changes:
            path = project / relative
            if path.is_file():
                saved = backup / relative
                saved.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(path, saved)
                with saved.open('rb') as f:
                    digest = hashlib.file_digest(f, 'sha256').hexdigest()
                existing.append({'path': relative.as_posix(), 'sha256': digest})
        saved_paths = {row['path'] for row in existing}
        created = [str(path) for path in targets if path.as_posix() not in saved_paths]
        (backup / 'manifest.json').write_text(json.dumps({'schema': 1, 'original_files': existing, 'created_files': created}, indent=2) + '\n')
        for relative, source in targets.items():
            destination = project / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            applied.append(relative)
            shutil.copyfile(source, destination)
        for relative in removed:
            applied.append(relative)
            (project / relative).unlink()
    except BaseException:
        saved_paths = {row['path'] for row in existing}
        for relative in reversed(applied):
            destination = project / relative
            saved = backup / relative
            if relative.as_posix() in saved_paths:
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(saved, destination)
            elif destination.is_file() and not destination.is_symlink():
                destination.unlink()
        shutil.rmtree(backup)
        raise
    print('Prepared isolated workspace. Original files are backed up locally in', backup.name)
    print('Game-derived and third-party inputs remain local; the APK is not built yet.')


if __name__ == '__main__':
    main()
