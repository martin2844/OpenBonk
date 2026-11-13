#!/usr/bin/env python3
"""Experimentally build a local game-data-embedded APK from a Unity export.

The export, intermediates and APK contain local game-derived data. This does
not build the data-separated distribution APK or establish feature parity.
"""
import argparse
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys


ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / 'tools'


def stages(base, editor, lua, output, runner, application_id, prepare_only=False, include_rewired=False):
    project = output / 'workspace'
    plugins = output / 'managed-plugins'
    common = ['--project', str(project)]
    unity = ['--unity', str(editor)]
    wrapped = ['--runner', runner] if runner else []
    result = [
        ('managed plugins', 'prepare-managed-plugins.py', ['--project', str(base), *unity, '--output', str(plugins), *wrapped]),
        ('workspace', 'assemble-unity-workspace.py', ['--base-project', str(base), '--output', str(project), '--allow-guid-remap']),
        ('workspace plugins', 'prepare-unity-workspace.py', [*common, '--plugins', str(plugins), '--lua-library', str(lua), '--remove-duplicate-package-dlls']),
        ('texture path collisions', 'resolve-unity-texture-case-collisions.py', [*common, '--manifest', str(output / 'texture-renames.json')]),
        ('Unity import', 'import-unity-workspace.py', [*common, *unity, '--log', str(output / 'import.log'), *wrapped]),
        ('package script references', 'remap-unity-package-scripts.py', [*common, '--backup', str(output / 'script-backup')]),
        ('characters', 'prepare-unity-characters.py', [*common, *unity, '--log', str(output / 'characters.log'), '--backup', str(output / 'character-backup'), *wrapped]),
        ('post-processing', 'prepare-unity-postprocessing.py', [*common, *unity, '--log', str(output / 'postprocessing.log'), '--backup', str(output / 'postprocessing-backup'), *wrapped]),
        ('skyboxes', 'prepare-unity-skyboxes.py', [*common, '--backup', str(output / 'skybox-backup')]),
        ('localization', 'rebuild-unity-localization.py', [*common, *unity, '--build-root', str(output / 'localization'), *wrapped]),
        ('Android Addressables', 'prepare-android-addressables.py', [*common, '--backup', str(output / 'addressables-backup')]),
        ('APK', 'build-unity-runtime.py', [*common, *unity, '--plugins', str(plugins), '--build-root', str(output / 'runtime'), '--embedded', '--application-id', application_id, *(['--include-rewired'] if include_rewired else []), *wrapped]),
    ]
    return result[:-1] if prepare_only else result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--base-project', required=True, type=Path, help='local AssetRipper 2.0.0 ExportedProject')
    p.add_argument('--unity', required=True, type=Path, help='Unity 2023.2.22f1 Editor executable')
    p.add_argument('--lua-library', required=True, type=Path, help='locally built ARM64 Lua bridge')
    p.add_argument('--output', required=True, type=Path, help='new local directory for workspace, backups and output')
    p.add_argument('--application-id', required=True, help='Android application ID for an isolated test package')
    p.add_argument('--runner', help='optional single executable used to launch Unity and Mono')
    p.add_argument('--include-rewired', action='store_true', help='experimentally include local Rewired plugins in the runtime')
    p.add_argument('--prepare-only', action='store_true', help='prepare the isolated Unity workspace without building the embedded APK')
    p.add_argument('--check', action='store_true', help='check initial inputs only without writing output')
    a = p.parse_args()
    base = a.base_project.expanduser().resolve(strict=True)
    editor = a.unity.expanduser().resolve(strict=True)
    lua = a.lua_library.expanduser().absolute()
    if lua.is_symlink() or not lua.is_file():
        p.error('Lua bridge must be a regular file')
    lua = lua.resolve()
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        p.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (base, ROOT)):
        p.error('output must be separate from the export and source checkout')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII):
        p.error('invalid Android application ID')
    if a.runner and not shutil.which(a.runner):
        p.error('configured runner not found')
    if not editor.is_file() or not os.access(editor, os.X_OK):
        p.error('Unity editor executable is not available')
    for name in ('Assets', 'Packages', 'ProjectSettings'):
        if not (base / name).is_dir():
            p.error('base project is missing ' + name)
    commands = stages(base, editor, lua, output, a.runner, a.application_id, a.prepare_only, a.include_rewired)
    # These stages can validate real source inputs before any output exists;
    # later stages depend on outputs created during the build.
    for index in (0, 1):
        _, script, args = commands[index]
        preflight = list(args)
        preflight[preflight.index('--output') + 1] = str(output)
        subprocess.run([sys.executable, str(TOOLS / script), *preflight, '--check'], check=True)
    if a.check:
        print('Initial inputs checked; subsequent stages and APK were not verified.', flush=True)
        return
    output.mkdir(mode=0o700)
    for number, (label, script, args) in enumerate(commands, 1):
        print(f'[{number}/{len(commands)}] {label}', flush=True)
        try:
            subprocess.run([sys.executable, str(TOOLS / script), *args], check=True)
        except subprocess.CalledProcessError as error:
            raise SystemExit(f'Stage failed: {label} (status {error.returncode}); local output and backups retained at {output}') from error
    if a.prepare_only:
        print('Local Unity workspace prepared:', output / 'workspace', flush=True)
    else:
        print('Local embedded APK created:', output / 'runtime/android/Megabonk-arm64-launcher-test.apk', flush=True)
    print('Contains local game data; this is not a data-separated release APK.', flush=True)


if __name__ == '__main__':
    main()
