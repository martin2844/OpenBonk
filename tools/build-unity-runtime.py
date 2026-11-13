#!/usr/bin/env python3
"""Build the experimental ARM64 Unity runtime in an isolated local workspace.

This is one stage of OpenBonk's build, not the final launcher APK. It requires
an already assembled local Unity project and managed third-party plugins.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys


PLUGINS = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
DEFINES = b'-define:OPENBONK_NO_REWIRED\n-define:OPENBONK_BETA_NO_PROBES\n'
REWIRED_DEFINES = b'-define:OPENBONK_BETA_NO_PROBES\n'
spec = importlib.util.spec_from_file_location('prepare_android_input', Path(__file__).resolve().parent / 'prepare-android-input.py')
android_input = importlib.util.module_from_spec(spec)
spec.loader.exec_module(android_input)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True, help='workspace created by assemble-unity-workspace.py')
    parser.add_argument('--unity', type=Path, required=True, help='Unity 2023.2.22f1 Editor executable')
    parser.add_argument('--plugins', type=Path, required=True, help='local directory with the three managed DLL inputs')
    parser.add_argument('--build-root', type=Path, required=True, help='new local directory for outputs and logs')
    parser.add_argument('--runner', help='optional single executable used to launch Unity, such as steam-run')
    parser.add_argument('--include-rewired', action='store_true', help='experimental: include locally generated Rewired plugins; controller behavior is not yet verified')
    output_mode = parser.add_mutually_exclusive_group()
    output_mode.add_argument('--gradle-project', action='store_true', help='export a local Gradle project instead of an APK/OBB')
    output_mode.add_argument('--embedded', action='store_true', help='build a local APK containing the exported game data')
    parser.add_argument('--application-id', help='optional Android application ID for an isolated local test')
    parser.add_argument('--check', action='store_true', help='validate prerequisites without writing files')
    args = parser.parse_args()
    if args.application_id and not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', args.application_id, flags=re.ASCII):
        parser.error('invalid Android application ID')
    project = args.project.expanduser().resolve(strict=True)
    editor = args.unity.expanduser().resolve(strict=True)
    plugins = args.plugins.expanduser().resolve(strict=True)
    output = args.build_root.expanduser().absolute()
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != MARKER:
        parser.error('project must be an isolated workspace created by assemble-unity-workspace.py')
    if not editor.is_file() or not os.access(editor, os.X_OK):
        parser.error('Unity editor executable is not available')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines():
        parser.error('workspace requires Unity 2023.2.22f1')
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('build root must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(project) or project.is_relative_to(output) or output.is_relative_to(plugins):
        parser.error('build root must be separate from the project and plugin inputs')
    rsp = project / 'Assets/csc.rsp'
    rsp_meta = Path(str(rsp) + '.meta')
    if rsp.exists() or rsp.is_symlink() or rsp_meta.exists() or rsp_meta.is_symlink():
        parser.error('workspace already contains Assets/csc.rsp or its metadata')
    lock = project / 'Temp/UnityLockfile'
    if lock.exists():
        parser.error('Unity workspace has a lock file; close Unity and inspect it before retrying')
    try:
        android_input.verify(project)
    except (OSError, ValueError) as error:
        parser.error(str(error))
    for name in PLUGINS:
        if not (plugins / name).is_file() or (plugins / name).is_symlink():
            parser.error('missing regular plugin input: ' + name)
    aapt2 = editor.parent / 'Data/PlaybackEngines/AndroidPlayer/SDK/build-tools/34.0.0/aapt2'
    if not aapt2.is_file() or any(c.isspace() for c in str(aapt2)):
        parser.error('Unity Android build-tools 34.0.0 aapt2 is required at a path without spaces')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('configured Unity runner not found')
    print('Runtime build prerequisites found. Third-party inputs remain local.', flush=True)
    if args.check:
        print('Check only; no build was run.')
        return
    output.mkdir(mode=0o700)
    managed = output / 'android/managed-plugins'
    managed.mkdir(parents=True)
    for name in PLUGINS:
        shutil.copyfile(plugins / name, managed / name)
    env = os.environ.copy()
    env.update({'OPENBONK_BUILD_ROOT': str(output), 'OPENBONK_NO_REWIRED_CODE': '0' if args.include_rewired else '1',
                'OPENBONK_NO_REWIRED_PLUGINS': '0' if args.include_rewired else '1', 'OPENBONK_NO_REWIRED_SCENE': '1',
                'OPENBONK_ANDROID_TARGET_SDK': '35',
                'OPENBONK_AAPT2_OVERRIDE': str(aapt2)})
    defines = REWIRED_DEFINES if args.include_rewired else DEFINES
    if args.application_id:
        env['OPENBONK_RUNTIME_APPLICATION_ID'] = args.application_id
    else:
        env.pop('OPENBONK_RUNTIME_APPLICATION_ID', None)
    log = output / 'runtime-build.log'
    method = ('NativeAndroidBuild.ExportArm64RuntimeGradleProject' if args.gradle_project else
              'NativeAndroidBuild.BuildArm64EmbeddedLauncherApk' if args.embedded else
              'NativeAndroidBuild.BuildArm64RuntimeResearchApk')
    cmd = ([runner] if runner else []) + [str(editor), '-batchmode', '-nographics', '-quit',
           '-projectPath', str(project), '-executeMethod', method,
           '-logFile', str(log)]
    fd = os.open(rsp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb') as f:
            f.write(defines)
        status = subprocess.run(cmd, env=env).returncode
    finally:
        if rsp.is_file() and not rsp.is_symlink() and rsp.read_bytes() == defines:
            rsp.unlink()
            # This metadata was absent before the build and can be generated
            # by Unity when importing our temporary response file.
            if rsp_meta.is_file() and not rsp_meta.is_symlink():
                rsp_meta.unlink()
    if status:
        raise SystemExit(f'Unity exited with status {status}; inspect the local log: {log}')
    if not log.is_file():
        raise SystemExit(f'Unity did not create its build log: {log}')
    if 'There is no active LocalizationSettings.' in log.read_text(errors='replace'):
        raise SystemExit(f'Unity opened a scene without localization settings; inspect the local log: {log}')
    if args.gradle_project:
        gradle = output / 'android/OpenBonk-gradle-project'
        for relative in ('settings.gradle', 'launcher/build.gradle', 'unityLibrary/build.gradle'):
            path = gradle / relative
            if not path.is_file() or not path.stat().st_size:
                raise SystemExit(f'Unity did not create the expected Gradle file: {relative}; inspect {log}')
        print('Local Gradle project created. It contains game data and is not a final APK.')
        return
    names = (('Megabonk-arm64-launcher-test.apk',) if args.embedded else
             ('Megabonk-arm64-runtime-research.apk', 'Megabonk-arm64-runtime-research.main.obb'))
    for name in names:
        path = output / 'android' / name
        if not path.is_file() or path.stat().st_size == 0:
            raise SystemExit(f'Unity did not create the expected output: {name}; inspect {log}')
        with path.open('rb') as f:
            print(name, hashlib.file_digest(f, 'sha256').hexdigest())
    if args.embedded:
        print('Local APK created with game data embedded. It is not a redistributable build.')
    else:
        print('Local runtime and OBB created. They are not the final launcher APK.')


if __name__ == '__main__':
    main()
