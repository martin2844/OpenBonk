#!/usr/bin/env python3
"""Import a prepared isolated workspace with the OpenBonk build defines."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
DEFINES = b'-define:OPENBONK_NO_REWIRED\n-define:OPENBONK_BETA_NO_PROBES\n'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True)
    p.add_argument('--unity', type=Path, required=True)
    p.add_argument('--log', type=Path, required=True, help='new local log file outside workspace')
    p.add_argument('--runner', help='optional executable used to launch Unity, e.g. steam-run')
    p.add_argument('--check', action='store_true')
    a = p.parse_args()
    project = a.project.expanduser().resolve(strict=True)
    editor = a.unity.expanduser().resolve(strict=True)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        p.error('project must be an isolated assembled workspace')
    if not editor.is_file() or not os.access(editor, os.X_OK):
        p.error('Unity editor executable is not available')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines():
        p.error('workspace requires Unity 2023.2.22f1')
    log = a.log.expanduser().absolute()
    if log.exists() or log.is_symlink() or not log.parent.is_dir():
        p.error('log must be a new file in an existing directory')
    log = log.parent.resolve() / log.name
    if log.is_relative_to(project):
        p.error('log must be outside the workspace')
    if not (project / '.openbonk-original-plugins/manifest.json').is_file():
        p.error('prepare-unity-workspace.py must be run first')
    rsp = project / 'Assets/csc.rsp'
    meta = Path(str(rsp) + '.meta')
    if any(x.exists() or x.is_symlink() for x in (rsp, meta, project / 'Temp/UnityLockfile')):
        p.error('workspace has a response file, metadata or Unity lock')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner:
        p.error('configured Unity runner not found')
    print('Workspace import prerequisites found.', flush=True)
    if a.check:
        print('Check only; no files were written.')
        return
    env = os.environ.copy()
    env.update({'OPENBONK_NO_REWIRED_CODE': '1', 'OPENBONK_NO_REWIRED_PLUGINS': '1'})
    cmd = ([runner] if runner else []) + [str(editor), '-batchmode', '-nographics', '-quit',
           '-projectPath', str(project), '-logFile', str(log)]
    fd = os.open(rsp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb') as f:
            f.write(DEFINES)
        status = subprocess.run(cmd, env=env).returncode
    finally:
        if rsp.is_file() and not rsp.is_symlink() and rsp.read_bytes() == DEFINES:
            rsp.unlink()
            if meta.is_file() and not meta.is_symlink():
                meta.unlink()
    if status:
        raise SystemExit(f'Unity exited with status {status}; inspect the local log: {log}')
    if not log.is_file():
        raise SystemExit('Unity did not create a log')
    log_text = log.read_text(errors='replace')
    if 'Exiting batchmode successfully now!' not in log_text or re.search(r'(?m)^.*\berror CS\d+:', log_text):
        raise SystemExit(f'Unity did not confirm a clean script import; inspect {log}')
    if not (project / 'Library/PackageCache').is_dir():
        raise SystemExit('Unity did not create its local package cache')
    print('Unity imported the isolated workspace; further preparation is still required.')


if __name__ == '__main__':
    main()
