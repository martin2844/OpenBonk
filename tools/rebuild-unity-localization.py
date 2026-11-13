#!/usr/bin/env python3
"""Experimentally restore Android localization from local Steam bundles.

Run only on an isolated workspace produced by assemble-unity-workspace.py.
The generated localization files contain game data and must remain local.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
DEFINES = b'-define:OPENBONK_NO_REWIRED\n-define:OPENBONK_BETA_NO_PROBES\n'
LOCALES = ('localization-locales_assets_all.bundle', 3499,
           'f88c3d3b09b758b239ae08525cb3dd65b343f4ce966bad0c7852aba74e5e4a59')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', type=Path, required=True)
    parser.add_argument('--unity', type=Path, required=True)
    parser.add_argument('--build-root', type=Path, required=True, help='new local directory for generated data and logs')
    parser.add_argument('--runner', help='optional single executable used to launch Unity, such as steam-run')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    editor = args.unity.expanduser().resolve(strict=True)
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
    if output.is_relative_to(project) or project.is_relative_to(output):
        parser.error('build root must be separate from the project')
    rsp = project / 'Assets/csc.rsp'
    meta = Path(str(rsp) + '.meta')
    if any(p.exists() or p.is_symlink() for p in (rsp, meta, project / 'Temp/UnityLockfile')):
        parser.error('workspace has a response file, metadata or Unity lock; inspect before retrying')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('configured Unity runner not found')
    manifest = json.loads((Path(__file__).resolve().parent / 'steam-linux-locale-inputs.json').read_text())
    expected = {Path(name).name: (item['size'], item['sha256']) for name, item in manifest.items()}
    expected[LOCALES[0]] = LOCALES[1:]
    bundle_dir = project / 'Assets/StreamingAssets/aa/StandaloneLinux64'
    bundles = list(bundle_dir.glob('*.bundle'))
    if {p.name for p in bundles} != set(expected):
        parser.error('local Steam localization bundle set does not match the supported version')
    for path in bundles:
        if path.is_symlink() or not path.is_file():
            parser.error('bundle must be a regular file: ' + path.name)
        with path.open('rb') as f:
            digest = hashlib.file_digest(f, 'sha256').hexdigest()
        if (path.stat().st_size, digest) != expected[path.name]:
            parser.error('bundle does not match supported version: ' + path.name)
    print('Localization prerequisites found; game data remains local.', flush=True)
    if args.check:
        print('Check only; no files were written.')
        return
    output.mkdir(mode=0o700)
    env = os.environ.copy()
    env.update({'OPENBONK_BUILD_ROOT': str(output), 'OPENBONK_NO_REWIRED_CODE': '1',
                'OPENBONK_NO_REWIRED_PLUGINS': '1'})
    fd = os.open(rsp, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    try:
        with os.fdopen(fd, 'wb') as f:
            f.write(DEFINES)
        for method, log_name in (('LocalizationBundleExport.ExportLinuxLocalizationBundles', 'localization-export.log'),
                                 ('RebuildAndroidLocalization.Build', 'localization-rebuild.log')):
            log = output / log_name
            cmd = ([runner] if runner else []) + [str(editor), '-batchmode', '-nographics', '-quit',
                   '-projectPath', str(project), '-executeMethod', method, '-logFile', str(log)]
            status = subprocess.run(cmd, env=env).returncode
            if status:
                raise SystemExit(f'Unity exited with status {status}; inspect the local log: {log}')
            if method.startswith('LocalizationBundleExport.'):
                data_path = output / 'android/localization-content.json'
                if not data_path.is_file() or not data_path.stat().st_size:
                    raise SystemExit(f'Unity did not produce localization data; inspect {log}')
                data = json.loads(data_path.read_text())
                if not data.get('locales') or not data.get('tables'):
                    raise SystemExit(f'Unity produced incomplete localization data; inspect {log}')
            elif not log.is_file() or 'Android Addressables built.' not in log.read_text(errors='replace'):
                raise SystemExit(f'Unity did not confirm the localization rebuild; inspect {log}')
    finally:
        if rsp.is_file() and not rsp.is_symlink() and rsp.read_bytes() == DEFINES:
            rsp.unlink()
            if meta.is_file() and not meta.is_symlink():
                meta.unlink()
    print('Unity reported successful localization rebuild. Validate the resulting build separately.')


if __name__ == '__main__':
    main()
