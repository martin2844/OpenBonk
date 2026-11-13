#!/usr/bin/env python3
"""Prepare local Android managed plugin inputs from an exported Unity project.

Uses the selected Unity editor's Mono and Mono.Cecil. Outputs contain derived
third-party code and must remain local. This does not build the OpenBonk APK.
"""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parent.parent
NAMES = ('Rewired_Core.dll', 'Coffee.UIParticle.dll', 'com.rlabrecque.steamworks.net.dll')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path, help='local exported Unity project')
    parser.add_argument('--unity', required=True, type=Path, help='Unity 2023.2.22f1 Editor executable')
    parser.add_argument('--output', required=True, type=Path, help='new directory for local plugin inputs')
    parser.add_argument('--runner', help='optional single executable used to run Unity Mono, such as steam-run')
    parser.add_argument('--check', action='store_true', help='validate prerequisites without writing output')
    args = parser.parse_args()
    project = args.project.expanduser().resolve(strict=True)
    editor = args.unity.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    plugins = project / 'Assets/Plugins'
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines():
        parser.error('project requires Unity 2023.2.22f1')
    if not editor.is_file() or not os.access(editor, os.X_OK):
        parser.error('Unity editor executable not found')
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(project) or project.is_relative_to(output):
        parser.error('output must be separate from the project')
    mono_root = editor.parent / 'Data/MonoBleedingEdge'
    mono, lib = mono_root / 'bin/mono', mono_root / 'lib/mono/4.5'
    compiler = lib / 'mcs.exe'
    cecil = mono_root / 'lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756/Mono.Cecil.dll'
    for path in (mono, compiler, cecil, *(plugins / name for name in NAMES),
                 ROOT / 'analysis/il2cpp/NormalizeAndroidManagedAssembly.cs',
                 ROOT / 'analysis/il2cpp/CreateAndroidSteamworksStub.cs'):
        if not path.is_file() or path.is_symlink():
            parser.error('required regular input is missing: ' + path.name)
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('configured Mono runner not found')
    print('Managed plugin prerequisites found.', flush=True)
    if args.check:
        print('Check only; no plugins were generated.')
        return
    output.mkdir(mode=0o700)
    try:
        env = os.environ.copy()
        env['MONO_PATH'] = os.pathsep.join((str(lib), str(cecil.parent)))
        env['MONO_CFG_DIR'] = str(mono_root / 'etc')
        prefix = ([runner] if runner else []) + [str(mono)]
        with tempfile.TemporaryDirectory(prefix='.openbonk-managed-', dir=output) as tmp:
            work = Path(tmp)
            for name, source in (('normalize.exe', 'NormalizeAndroidManagedAssembly.cs'),
                                 ('steamworks.exe', 'CreateAndroidSteamworksStub.cs')):
                subprocess.run([*prefix, str(compiler), '-nologo', '-r:' + str(cecil),
                                '-out:' + str(work / name), str(ROOT / 'analysis/il2cpp' / source)],
                               env=env, check=True)
            for name in NAMES:
                tool = 'steamworks.exe' if name == 'com.rlabrecque.steamworks.net.dll' else 'normalize.exe'
                extra = [] if tool == 'steamworks.exe' else [str(lib)]
                subprocess.run([*prefix, str(work / tool), str(plugins / name), str(output / name), *extra],
                               env=env, check=True)
                with (output / name).open('rb') as f:
                    print(name, hashlib.file_digest(f, 'sha256').hexdigest(), flush=True)
    except BaseException:
        shutil.rmtree(output)
        raise
    print('Prepared local plugin inputs; these are not a final APK.')


if __name__ == '__main__':
    main()
