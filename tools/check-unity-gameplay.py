#!/usr/bin/env python3
"""Run selected local gameplay checks in an isolated prepared Unity workspace.

These component checks do not test a built APK, rendering, a physical controller,
or an interactive game. The Unity project and logs can contain game data.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess


ROOT = Path(__file__).resolve().parent.parent
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
DEFINES = b'-define:OPENBONK_NO_REWIRED\n-define:OPENBONK_BETA_NO_PROBES\n'
CHECKS = (
    ('AndroidInputBindingCheck', 'passed', False),
    ('AndroidMenuSwapCheck', 'PASS', False),
    ('ChestSkipPortCheck', 'passed', False),
    ('ChestNavigationBindingCheck', 'PASSED', True),
    ('ChallengeShrinePlayCheck', 'PASS', True),
    ('BossHealthBarPlayCheck', 'PASS', True),
)


def check_result(name, marker, status, log):
    if status or not log.is_file() or name + ' ' + marker not in log.read_text(errors='replace'):
        raise RuntimeError(name + ' failed or did not report success; inspect ' + str(log))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True, help='prepared isolated Unity workspace')
    p.add_argument('--unity', type=Path, required=True, help='Unity 2023.2.22f1 executable')
    p.add_argument('--output', type=Path, required=True, help='new directory for private Unity logs')
    p.add_argument('--runner', help='optional executable used to run Unity, e.g. steam-run')
    p.add_argument('--check', action='store_true', help='validate initial inputs only')
    a = p.parse_args()
    project, editor = a.project.resolve(strict=True), a.unity.resolve(strict=True)
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (project, ROOT)): p.error('output must be separate from workspace and checkout')
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines(): p.error('Unity 2023.2.22f1 workspace required')
    if not editor.is_file() or not os.access(editor, os.X_OK): p.error('Unity executable is unavailable')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner: p.error('runner unavailable')
    rsp = project / 'Assets/csc.rsp'; meta = Path(str(rsp) + '.meta')
    if any(path.exists() or path.is_symlink() for path in (rsp, meta, project / 'Temp/UnityLockfile')): p.error('workspace has a response file or Unity lock')
    if a.check:
        print('Initial inputs checked; Unity gameplay checks have not run.')
        return
    output.mkdir(mode=0o700)
    env = os.environ.copy()
    env.update(OPENBONK_NO_REWIRED_CODE='1', OPENBONK_NO_REWIRED_PLUGINS='1', OPENBONK_NO_REWIRED_SCENE='1')
    fd = os.open(rsp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb') as stream: stream.write(DEFINES)
        for name, marker, play in CHECKS:
            print('Checking ' + name, flush=True)
            log = output / (name + '.log')
            cmd = ([runner] if runner else []) + [str(editor), '-batchmode', '-nographics', '-projectPath', str(project),
                                                  '-executeMethod', name + '.Run', '-logFile', str(log)]
            if not play: cmd.append('-quit')
            try:
                status = subprocess.run(cmd, env=env, timeout=240).returncode
            except subprocess.TimeoutExpired as error:
                raise RuntimeError(name + ' timed out; inspect ' + str(log)) from error
            check_result(name, marker, status, log)
            print('PASS ' + name, flush=True)
    finally:
        if rsp.is_file() and not rsp.is_symlink() and rsp.read_bytes() == DEFINES:
            rsp.unlink()
            if meta.is_file() and not meta.is_symlink(): meta.unlink()
    print('Six Unity component checks passed. Android gameplay remains unverified.')


if __name__ == '__main__': main()
