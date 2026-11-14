#!/usr/bin/env python3
"""Build a private research launcher from a local Steam installation.

This exports and prepares an isolated Unity workspace, builds a runtime and
Gradle project, then runs the research reconstruction chain. Outputs contain
game data and are not redistributable. Gameplay fidelity is not established.
"""
import argparse
import importlib.util
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
from types import SimpleNamespace
import zipfile

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / 'tools'
spec = importlib.util.spec_from_file_location('local_research_chain', TOOLS / 'build-local-research-chain.py')
research_chain = importlib.util.module_from_spec(spec)
spec.loader.exec_module(research_chain)


def output_allows_execution(parent):
    """Check whether generated Gradle helper scripts can be executed here."""
    fd, name = tempfile.mkstemp(prefix='.openbonk-exec-check-', dir=parent)
    try:
        os.fchmod(fd, 0o700)
        return os.access(name, os.X_OK)
    finally:
        os.close(fd)
        os.unlink(name)


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
    original_obb = out / 'runtime/android/Megabonk-arm64-runtime-research.main.obb'
    original_apk = out / 'runtime/android/Megabonk-arm64-runtime-research.apk'
    original_gradle = out / 'gradle/android/OpenBonk-gradle-project'
    shared0 = getattr(a, 'shared0_fonts', False)
    runtime_obb = out / 'relocated/runtime.obb' if shared0 else original_obb
    runtime_apk = out / 'relocated/runtime.apk' if shared0 else original_apk
    gradle_project = out / 'relocated/gradle' if shared0 else original_gradle
    chain = ['--steam-data', a.game / 'Megabonk_Data', '--runtime-obb', runtime_obb,
             '--runtime-apk', runtime_apk,
             '--gradle-project', gradle_project, '--unity-project', workspace,
             '--unity-editor', a.unity.parent, '--json-jar', a.json_jar, '--application-id', a.application_id,
             '--output', out / 'research', *wrapped]
    if a.quality_textures: chain += ['--quality-textures']
    if a.normal_fallback: chain += ['--normal-fallback']
    if a.font_fallback: chain += ['--font-fallback']
    if a.resource_fallback: chain += ['--resource-fallback']
    if getattr(a, 'shadow_fallback', False): chain += ['--shadow-fallback']
    if getattr(a, 'partial_rgba', False): chain += ['--partial-rgba']
    if getattr(a, 'partial_etc2', False): chain += ['--partial-etc2']
    if shared0: chain += ['--shared0-fonts']
    if getattr(a, 'gradle_online', False): chain += ['--gradle-online']
    for attr in ('steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home'):
        value = getattr(a, attr)
        if value: chain += ['--' + attr.replace('_', '-'), value]
    steps = [('export and prepare Unity', tool('build-local-from-steam.py', *steam)),
             ('build Android runtime', tool('build-unity-runtime.py', *runtime)),
             ('export Android Gradle project', tool('build-unity-runtime.py', *gradle))]
    if shared0:
        steps.append(('relocate sharedassets0', tool('relocate-unity-shared0.py', '--runtime-apk', original_apk,
                     '--runtime-obb', original_obb, '--gradle-project', original_gradle, '--output', out / 'relocated')))
    steps.append(('build Linux research launcher', tool('build-local-research-chain.py', *chain)))
    if a.windows_game:
        linux_research = out / 'research'; windows_research = out / 'windows-research'
        windows_chain = ['--steam-data', a.windows_game / 'Megabonk_Data', '--platform', 'windows',
                         '--runtime-obb', runtime_obb, '--runtime-apk', runtime_apk,
                         '--gradle-project', gradle_project, '--unity-project', workspace,
                         '--unity-editor', a.unity.parent, '--json-jar', a.json_jar, '--application-id', a.application_id,
                         '--output', windows_research, '--host-library', a.host_library or linux_research / 'native/libopenbonk_texture.so',
                         '--android-library', a.android_library or linux_research / 'native/libopenbonk_texture_arm64.so',
                         '--decoder', a.decoder or linux_research / 'native/crunch-decoder', *wrapped]
        if a.windows_steam_zip: windows_chain += ['--steam-zip', a.windows_steam_zip]
        if a.quality_textures: windows_chain += ['--quality-textures']
        if a.normal_fallback: windows_chain += ['--normal-fallback']
        if a.font_fallback: windows_chain += ['--font-fallback']
        if a.resource_fallback: windows_chain += ['--resource-fallback']
        if getattr(a, 'shadow_fallback', False): windows_chain += ['--shadow-fallback']
        if getattr(a, 'partial_rgba', False): windows_chain += ['--partial-rgba']
        if getattr(a, 'partial_etc2', False): windows_chain += ['--partial-etc2']
        if shared0: windows_chain += ['--shared0-fonts']
        if getattr(a, 'gradle_online', False): windows_chain += ['--gradle-online']
        if a.gradle_user_home: windows_chain += ['--gradle-user-home', a.gradle_user_home]
        dual_args = SimpleNamespace(output=linux_research, steam_data=a.game / 'Megabonk_Data',
                                    unity_editor=a.unity.parent, runtime_obb=runtime_obb,
                                    runtime_apk=runtime_apk,
                                    gradle_project=gradle_project, unity_project=workspace,
                                    json_jar=a.json_jar, application_id=a.application_id, runner=a.runner,
                                    host_library=a.host_library, android_library=a.android_library, decoder=a.decoder,
                                    steam_zip=a.steam_zip, gradle_user_home=a.gradle_user_home, platform='linux', quality_textures=a.quality_textures, normal_fallback=a.normal_fallback, partial_rgba=getattr(a, 'partial_rgba', False), gradle_online=getattr(a, 'gradle_online', False))
        dual = research_chain.commands(dual_args)[-1][1]
        dual[dual.index('--output') + 1] = str(out / 'dual-launcher')
        dual += ['--windows-research', str(windows_research)]
        steps.extend([('build Windows research launcher', tool('build-local-research-chain.py', *windows_chain)),
                      ('build dual-platform research launcher', dual)])
    return steps


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('game', 'assetripper', 'unity', 'json-jar', 'output'): p.add_argument('--' + name, required=True, type=Path)
    for name in ('lua-library', 'steam-zip', 'host-library', 'android-library', 'decoder', 'gradle-user-home', 'windows-game', 'windows-steam-zip'): p.add_argument('--' + name, type=Path)
    p.add_argument('--application-id', required=True)
    p.add_argument('--runner', help='optional wrapper for AssetRipper, Unity and native tools')
    p.add_argument('--gradle-online', action='store_true', help='allow Gradle to download missing dependencies')
    p.add_argument('--include-rewired', action='store_true', help='experimentally include local Rewired plugins in both Unity builds')
    p.add_argument('--quality-textures', action='store_true', help='experimentally use higher-quality ETC2 for selected textures')
    p.add_argument('--normal-fallback', action='store_true', help='experimentally recover RGB normal maps that pass every mip direction check')
    p.add_argument('--font-fallback', action='store_true', help='experimentally reconstruct a font atlas; requires --quality-textures')
    p.add_argument('--resource-fallback', action='store_true', help='experimentally reconstruct selected resource textures; requires --quality-textures')
    p.add_argument('--shadow-fallback', action='store_true', help='experimentally reconstruct a uniquely matching shadow; requires --quality-textures')
    p.add_argument('--partial-rgba', action='store_true', help='experimentally reconstruct exact partial RGBA texture bytes')
    p.add_argument('--partial-etc2', action='store_true', help='also reconstruct exact partial ETC2 bytes; requires --partial-rgba and --quality-textures')
    p.add_argument('--shared0-fonts', action='store_true', help='experimentally relocate sharedassets0 and reconstruct selected fonts')
    p.add_argument('--check', action='store_true', help='check only initial inputs')
    a = p.parse_args(); output = a.output.expanduser().absolute()
    if a.font_fallback and not a.quality_textures: p.error('--font-fallback requires --quality-textures')
    if a.resource_fallback and not a.quality_textures: p.error('--resource-fallback requires --quality-textures')
    if a.shadow_fallback and not a.quality_textures: p.error('--shadow-fallback requires --quality-textures')
    if a.partial_etc2 and (not a.partial_rgba or not a.quality_textures): p.error('--partial-etc2 requires --partial-rgba and --quality-textures')
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    a.output = output.parent.resolve() / output.name
    try:
        if not output_allows_execution(a.output.parent):
            p.error('output filesystem must allow executing generated Gradle scripts; choose a mount without noexec')
    except OSError as error:
        p.error('cannot check output filesystem: ' + str(error))
    if a.windows_steam_zip and not a.windows_game: p.error('Windows ZIP requires --windows-game')
    for name in ('game', 'assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home', 'windows_game', 'windows_steam_zip'):
        value = getattr(a, name)
        if value is None: continue
        if value.expanduser().is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, value.expanduser().resolve(strict=True))
    inputs = [getattr(a, n) for n in ('game', 'assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home', 'windows_game', 'windows_steam_zip') if getattr(a, n) is not None]
    if any(a.output.is_relative_to(x) or x.is_relative_to(a.output) for x in (ROOT, *inputs)): p.error('output must be separate from source and inputs')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII): p.error('invalid application ID')
    if a.runner and not shutil.which(a.runner): p.error('runner not found')
    if not a.game.is_dir() or not (a.game / 'Megabonk_Data/globalgamemanagers.assets').is_file(): p.error('Steam game data missing')
    for name in ('assetripper', 'unity', 'json_jar', 'lua_library', 'steam_zip', 'host_library', 'android_library', 'decoder', 'windows_steam_zip'):
        value = getattr(a, name)
        if value is not None and not value.is_file(): p.error(name + ' must be a file')
    if a.gradle_user_home and not a.gradle_user_home.is_dir(): p.error('Gradle cache must be a directory')
    if not zipfile.is_zipfile(a.json_jar) or (a.steam_zip and not zipfile.is_zipfile(a.steam_zip)) or (a.windows_steam_zip and not zipfile.is_zipfile(a.windows_steam_zip)): p.error('invalid ZIP input')
    if importlib.util.find_spec('UnityPy') is None: p.error('UnityPy missing from active Python environment')
    try:
        research_chain.validate_steam_inputs((a.game / 'Megabonk_Data').resolve(), a.steam_zip)
        if a.windows_game:
            if not a.windows_game.is_dir(): raise ValueError('Windows game directory is missing')
            research_chain.validate_steam_inputs((a.windows_game / 'Megabonk_Data').resolve(strict=True), a.windows_steam_zip, 'windows')
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error: p.error(str(error))
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
    result = 'dual-launcher' if a.windows_game else 'research/launcher'
    print('Private research APK created:', a.output / result / 'gradle/launcher/build/outputs/apk/release/launcher-release.apk', flush=True)
    print('The APK, ZIP and intermediate files contain game data and must not be distributed.', flush=True)


if __name__ == '__main__': main()
