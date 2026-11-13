#!/usr/bin/env python3
"""Build a private research launcher from a local Steam installation.

This exports and prepares an isolated Unity workspace, builds a runtime and
Gradle project, then runs the research reconstruction chain. Outputs contain
game data and are not redistributable. Gameplay fidelity is not established.
"""
import argparse
import importlib.util
from pathlib import Path
import re
import shutil
import subprocess
import sys
import zipfile

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / 'tools'


def commands(a):
    out = a.output; prepared = out / 'steam/build'; workspace = prepared / 'workspace'; plugins = prepared / 'managed-plugins'
    wrapped = ['--runner', a.runner] if a.runner else []
    python = sys.executable
    def tool(name, *values): return [python, str(TOOLS / name), *map(str, values)]
    steam = ['--game', a.game, '--assetripper', a.assetripper, '--unity', a.unity, '--output', out / 'steam',
             '--application-id', a.application_id, '--prepare-only', *wrapped]
    if a.lua_library: steam += ['--lua-library', a.lua_library]
    common = ['--project', workspace, '--unity', a.unity, '--plugins', plugins, '--application-id', a.application_id,
              *(['--include-rewired'] if a.include_rewired else []), *wrapped]
    runtime = [*common, '--build-root', out / 'runtime']
    gradle = [*common, '--build-root', out / 'gradle', '--gradle-project']
    chain = ['--steam-data', a.game / 'Megabonk_Data', '--runtime-obb', out / 'runtime/android/Megabonk-arm64-runtime-research.main.obb',
             '--runtime-apk', out / 'runtime/android/Megabonk-arm64-runtime-research.apk',
             '--gradle-project', out / 'gradle/android/OpenBonk-gradle-project', '--unity-project', workspace,
             '--unity-editor', a.unity.parent, '--json-jar', a.json_jar, '--application-id', a.application_id,
             '--output', out / 'research', *wrapped]
    for attr in ('steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home'):
        value = getattr(a, attr)
        if value: chain += ['--' + attr.replace('_', '-'), value]
    return [('export and prepare Unity', tool('build-local-from-steam.py', *steam)),
            ('build Android runtime', tool('build-unity-runtime.py', *runtime)),
            ('export Android Gradle project', tool('build-unity-runtime.py', *gradle)),
            ('build research launcher', tool('build-local-research-chain.py', *chain))]


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('game', 'assetripper', 'unity', 'json-jar', 'output'): p.add_argument('--' + name, required=True, type=Path)
    for name in ('lua-library', 'steam-zip', 'host-library', 'android-library', 'decoder', 'gradle-user-home'): p.add_argument('--' + name, type=Path)
    p.add_argument('--application-id', required=True)
    p.add_argument('--runner', help='optional wrapper for AssetRipper, Unity and native tools')
    p.add_argument('--include-rewired', action='store_true', help='experimentally include local Rewired plugins in both Unity builds')
    p.add_argument('--check', action='store_true', help='check only initial inputs')
    a = p.parse_args(); output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    a.output = output.parent.resolve() / output.name
    for name in ('game', 'assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home'):
        value = getattr(a, name)
        if value is None: continue
        if value.expanduser().is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, value.expanduser().resolve(strict=True))
    inputs = [getattr(a, n) for n in ('game', 'assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home') if getattr(a, n) is not None]
    if any(a.output.is_relative_to(x) or x.is_relative_to(a.output) for x in (ROOT, *inputs)): p.error('output must be separate from source and inputs')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII): p.error('invalid application ID')
    if a.runner and not shutil.which(a.runner): p.error('runner not found')
    if not a.game.is_dir() or not (a.game / 'Megabonk_Data/globalgamemanagers.assets').is_file(): p.error('Steam game data missing')
    for name in ('assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder'):
        value = getattr(a, name)
        if value is not None and not value.is_file(): p.error(name + ' must be a file')
    if a.gradle_user_home and not a.gradle_user_home.is_dir(): p.error('Gradle cache must be a directory')
    if not zipfile.is_zipfile(a.json_jar) or (a.steam_zip and not zipfile.is_zipfile(a.steam_zip)): p.error('invalid ZIP input')
    if importlib.util.find_spec('UnityPy') is None: p.error('UnityPy missing from active Python environment')
    steps = commands(a)
    # The output's parent already exists, so the nested preflight can run
    # before creating the outer output directory.
    preflight = list(steps[0][1]); preflight[preflight.index('--output') + 1] = str(a.output); preflight.append('--check')
    subprocess.run(preflight, check=True)
    if a.check:
        print('Initial inputs checked; export, Unity builds and research launcher remain unverified.', flush=True)
        return
    a.output.mkdir(mode=0o700)
    with (a.output / 'build.log').open('x', encoding='utf-8') as log:
        for i, (label, command) in enumerate(steps, 1):
            print(f'[{i}/{len(steps)}] {label}', flush=True); log.write(f'[{i}/{len(steps)}] {label}\n'); log.flush()
            status = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT).returncode
            if status: raise SystemExit(f'Stage failed: {label} (status {status}); local outputs and log retained at {a.output}')
    print('Private research APK created:', a.output / 'research/launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk', flush=True)
    print('The APK, ZIP and intermediate files contain game data and must not be distributed.', flush=True)


if __name__ == '__main__': main()
