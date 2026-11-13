#!/usr/bin/env python3
"""Export a local Steam installation and build an experimental local APK.

The APK embeds game-derived assets and is not the data-separated release.
The game, export, intermediate files and output must remain local.
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


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--game', type=Path, required=True)
    p.add_argument('--assetripper', type=Path, required=True)
    p.add_argument('--unity', type=Path, required=True)
    p.add_argument('--lua-library', type=Path, help='existing ARM64 Lua bridge; omit to build it with Unity’s NDK')
    p.add_argument('--output', type=Path, required=True, help='new directory outside the game and checkout')
    p.add_argument('--application-id', required=True, help='application ID for an isolated test package')
    p.add_argument('--runner', help='optional executable used to run AssetRipper, Unity and Mono')
    p.add_argument('--check', action='store_true', help='check initial inputs only; do not export or build')
    a = p.parse_args()
    game = a.game.expanduser().resolve(strict=True)
    ripper = a.assetripper.expanduser().resolve(strict=True)
    unity = a.unity.expanduser().resolve(strict=True)
    lua = a.lua_library.expanduser().absolute() if a.lua_library else None
    if lua is not None:
        if lua.is_symlink() or not lua.is_file():
            p.error('Lua bridge must be a regular file')
        lua = lua.resolve()
    out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('output must be a new path in an existing directory')
    out = out.parent.resolve() / out.name
    if any(out.is_relative_to(path) or path.is_relative_to(out) for path in (game, ROOT)):
        p.error('output must be separate from the game and source checkout')
    if not unity.is_file() or not os.access(unity, os.X_OK):
        p.error('Unity editor executable is not available')
    ndk = unity.parent / 'Data/PlaybackEngines/AndroidPlayer/NDK'
    compiler = ndk / 'toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android23-clang'
    if lua is None and (not compiler.is_file() or not os.access(compiler, os.X_OK)):
        p.error('Unity Android NDK compiler is required to build the Lua bridge')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII):
        p.error('invalid Android application ID')
    if a.runner and not shutil.which(a.runner):
        p.error('configured runner not found')
    runner = ['--runner', a.runner] if a.runner else []
    export = [sys.executable, str(TOOLS / 'export-steam-unity-project.py'), '--game', str(game),
              '--assetripper', str(ripper), '--output', str(out / 'export'), *runner]
    preflight = list(export)
    preflight[preflight.index('--output') + 1] = str(out)
    subprocess.run([*preflight, '--check'], check=True)
    if a.check:
        print('Initial inputs checked; Unity export and build were not verified.', flush=True)
        return
    out.mkdir(mode=0o700)
    if lua is None:
        lua_dir = out / 'lua'
        env = os.environ.copy()
        env.update({'ANDROID_NDK_HOME': str(ndk), 'OPENBONK_LUA_OUTPUT': str(lua_dir)})
        command = ([a.runner] if a.runner else []) + ['bash', str(ROOT / 'native/lua/build.sh')]
        print('Building the Lua bridge locally', flush=True)
        try:
            subprocess.run(command, env=env, check=True)
        except subprocess.CalledProcessError as error:
            raise SystemExit(f'Lua build failed (status {error.returncode}); local output retained at {out}') from error
        lua = lua_dir / 'libmegabonk_lua.so'
    build = [sys.executable, str(TOOLS / 'build-local-unity-apk.py'), '--base-project', str(out / 'export/ExportedProject'),
             '--unity', str(unity), '--lua-library', str(lua), '--output', str(out / 'build'),
             '--application-id', a.application_id, *runner]
    for label, command in (('Steam export', export), ('local APK', build)):
        print('Starting ' + label, flush=True)
        try:
            subprocess.run(command, check=True)
        except subprocess.CalledProcessError as error:
            raise SystemExit(f'{label} failed (status {error.returncode}); local output retained at {out}') from error
    print('Local build completed. Game-derived files must remain local.', flush=True)


if __name__ == '__main__':
    main()
