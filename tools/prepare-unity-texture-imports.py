#!/usr/bin/env python3
"""Preserve selected Android texture pixels in an isolated Unity workspace.

The original local PNG importer metadata is backed up before the Unity editor
changes platform settings. Logs and backups may contain game data.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
DEFINES = b'-define:OPENBONK_NO_REWIRED\n-define:OPENBONK_BETA_NO_PROBES\n'


def png_metadata(assets):
    result = []
    for directory, names, files in os.walk(assets, followlinks=False):
        if any((Path(directory) / name).is_symlink() for name in names):
            raise ValueError('symbolic link in Assets')
        # Android library resources are packaged by Gradle, not TextureImporter.
        names[:] = [name for name in names if not name.endswith('.androidlib')]
        for name in files:
            path = Path(directory) / name
            if path.suffix.lower() != '.png': continue
            meta = Path(str(path) + '.meta')
            if path.is_symlink() or not path.is_file() or meta.is_symlink() or not meta.is_file():
                raise ValueError('PNG or its metadata is not a regular file: ' + str(path.relative_to(assets)))
            result.append(meta)
    if not result: raise ValueError('workspace has no PNG textures')
    return sorted(result)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True)
    p.add_argument('--unity', type=Path, required=True)
    p.add_argument('--log', type=Path, required=True)
    p.add_argument('--backup', type=Path, required=True)
    p.add_argument('--runner')
    p.add_argument('--check', action='store_true')
    a = p.parse_args(); project = a.project.expanduser().resolve(strict=True); editor = a.unity.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if marker.is_symlink() or not marker.is_file() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if not editor.is_file() or not os.access(editor, os.X_OK): p.error('Unity editor executable is not available')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines(): p.error('workspace requires Unity 2023.2.22f1')
    if not (project / 'Library/PackageCache').is_dir(): p.error('workspace must first be imported by Unity')
    editor_script = project / 'Assets/Editor/PreserveItemIconPixels.cs'
    if editor_script.is_symlink() or not editor_script.is_file(): p.error('texture preparation script is missing')
    rsp = project / 'Assets/csc.rsp'; meta = Path(str(rsp) + '.meta')
    if any(x.exists() or x.is_symlink() for x in (rsp, meta, project / 'Temp/UnityLockfile')): p.error('workspace has a response file, metadata or Unity lock')
    log = a.log.expanduser().absolute(); backup = a.backup.expanduser().absolute()
    if any(x.exists() or x.is_symlink() or not x.parent.is_dir() for x in (log, backup)): p.error('log and backup must be new paths')
    log = log.parent.resolve() / log.name; backup = backup.parent.resolve() / backup.name
    if (log.is_relative_to(project) or backup.is_relative_to(project) or project.is_relative_to(backup) or
            log == backup or log.is_relative_to(backup) or backup.is_relative_to(log)):
        p.error('log and backup must be separate from workspace and each other')
    try: metas = png_metadata(project / 'Assets')
    except ValueError as error: p.error(str(error))
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner: p.error('configured Unity runner not found')
    print(f'Texture preparation prerequisites found; {len(metas)} PNG metadata files will be backed up.', flush=True)
    if a.check:
        print('Check only; no files were written.'); return
    backup.mkdir(mode=0o700)
    for path in metas:
        dest = backup / path.relative_to(project / 'Assets'); dest.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(path, dest)
    env = os.environ.copy(); env.update({'OPENBONK_NO_REWIRED_CODE': '1', 'OPENBONK_NO_REWIRED_PLUGINS': '1'})
    cmd = ([runner] if runner else []) + [str(editor), '-batchmode', '-nographics', '-quit', '-projectPath', str(project),
           '-executeMethod', 'PreserveItemIconPixels.Apply', '-logFile', str(log)]
    fd = os.open(rsp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb') as f: f.write(DEFINES)
        status = subprocess.run(cmd, env=env).returncode
    finally:
        if rsp.is_file() and not rsp.is_symlink() and rsp.read_bytes() == DEFINES:
            rsp.unlink()
            if meta.is_file() and not meta.is_symlink(): meta.unlink()
    if status: raise SystemExit(f'Unity exited with status {status}; metadata backup and log retained at {backup} and {log}')
    if not log.is_file() or 'PreserveItemIconPixels: checked 261 content icons;' not in log.read_text(errors='replace'):
        raise SystemExit(f'Unity did not confirm texture preparation; inspect {log}; metadata backup retained at {backup}')
    print('Prepared Android texture import settings; verify the compiled output separately.', flush=True)


if __name__ == '__main__': main()
