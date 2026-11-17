#!/usr/bin/env python3
"""Build a private research launcher from a local Steam installation.

This exports and prepares an isolated Unity workspace, builds a runtime and
Gradle project, then runs the research reconstruction chain. Outputs contain
game data and are not redistributable. Gameplay fidelity is not established.
"""
import argparse
import importlib
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


def check_conversion_imports():
    """Load conversion modules now, including their native dependencies."""
    research_chain.check_conversion_imports()


def check_online_apk(aapt2, apk, application_id, runner=None):
    """Require the permissions needed by the launcher online features."""
    command = ([runner] if runner else []) + [str(aapt2), 'dump', 'permissions', str(apk)]
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=60, check=True)
    except (OSError, subprocess.SubprocessError) as error:
        raise ValueError('could not inspect final APK permissions') from error
    lines = result.stdout.splitlines()
    if f'package: {application_id}' not in lines:
        raise ValueError('final APK package does not match the requested application ID')
    permissions = set()
    for line in lines:
        if line.startswith('uses-permission:'):
            match = re.match(r"^uses-permission: name='([^']+)'(?:\s|$)", line)
            if not match:
                raise ValueError('could not parse final APK permission')
            permissions.add(match.group(1))
    for required in ('android.permission.INTERNET', 'android.permission.REQUEST_INSTALL_PACKAGES'):
        if required not in permissions:
            raise ValueError('final APK missing required permission: ' + required)


def check_apk_version(aapt2, apk, code, name, runner=None):
    if code is None and name is None: return
    command = ([runner] if runner else []) + [str(aapt2), 'dump', 'badging', str(apk)]
    try:
        result = subprocess.run(command, capture_output=True, text=True, timeout=60, check=True)
    except (OSError, subprocess.SubprocessError) as error:
        raise ValueError('could not inspect final APK version') from error
    lines = [line for line in result.stdout.splitlines() if line.startswith('package: ')]
    if len(lines) != 1: raise ValueError('could not parse final APK version')
    fields = dict(re.findall(r"\b(versionCode|versionName)='([^']*)'", lines[0]))
    if (code is not None and fields.get('versionCode') != str(code)) or (name is not None and fields.get('versionName') != name):
        raise ValueError('final APK version does not match requested metadata')


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
    if getattr(a, 'normal_block_fallback', False): chain += ['--normal-block-fallback']
    if getattr(a, 'normal_block_extra', False): chain += ['--normal-block-extra']
    if getattr(a, 'color_block_fallback', False): chain += ['--color-block-fallback']
    if getattr(a, 'emission_block_fallback', False): chain += ['--emission-block-fallback']
    if getattr(a, 'bullseye_block_fallback', False): chain += ['--bullseye-block-fallback']
    if getattr(a, 'animation_curve_fallback', False): chain += ['--animation-curve-fallback']
    if getattr(a, 'animation_curve_relaxed', False): chain += ['--animation-curve-relaxed']
    if getattr(a, 'animation_curve_extended', False): chain += ['--animation-curve-extended']
    if getattr(a, 'animation_curve_hybrid', False): chain += ['--animation-curve-hybrid']
    if getattr(a, 'animation_curve_reordered', False): chain += ['--animation-curve-reordered']
    if getattr(a, 'reordered_animation_bindings', False): chain += ['--reordered-animation-bindings']
    if getattr(a, 'neighboring_animation_keys', False): chain += ['--neighboring-animation-keys']
    if a.font_fallback: chain += ['--font-fallback']
    if a.resource_fallback: chain += ['--resource-fallback']
    if getattr(a, 'nodraw_fallback', False): chain += ['--nodraw-fallback']
    if getattr(a, 'atlas_fallback', False): chain += ['--atlas-fallback']
    if getattr(a, 'lightmap_fallback', False): chain += ['--lightmap-fallback']
    if getattr(a, 'emission_fallback', False): chain += ['--emission-fallback']
    if getattr(a, 'fox_fallback', False): chain += ['--fox-fallback']
    if getattr(a, 'bounded_fallback', False): chain += ['--bounded-fallback']
    if getattr(a, 'shadow_fallback', False): chain += ['--shadow-fallback']
    if getattr(a, 'emoji_fallback', False): chain += ['--emoji-fallback']
    if getattr(a, 'partial_rgba', False): chain += ['--partial-rgba']
    if getattr(a, 'partial_normal_rgba', False): chain += ['--partial-normal-rgba']
    if getattr(a, 'localization_cab', False): chain += ['--localization-cab']
    if getattr(a, 'localization_small', False): chain += ['--localization-small']
    if getattr(a, 'partial_etc2', False): chain += ['--partial-etc2']
    if getattr(a, 'partial_etc2_small', False): chain += ['--partial-etc2-small']
    if getattr(a, 'partial_etc2_tiny', False): chain += ['--partial-etc2-tiny']
    if getattr(a, 'partial_etc2_aliases', False): chain += ['--partial-etc2-aliases']
    if getattr(a, 'partial_etc2_fast', False): chain += ['--partial-etc2-fast']
    if getattr(a, 'partial_etc2_blend', False): chain += ['--partial-etc2-blend']
    if getattr(a, 'partial_etc2_byte', False): chain += ['--partial-etc2-byte']
    if getattr(a, 'partial_etc2_rgba', False): chain += ['--partial-etc2-rgba']
    if getattr(a, 'partial_etc2_extra', False): chain += ['--partial-etc2-extra']
    if getattr(a, 'color_block_extra', False): chain += ['--color-block-extra']
    if getattr(a, 'small_color_block_fallback', False): chain += ['--small-color-block-fallback']
    if getattr(a, 'small_color_block_aliases', False): chain += ['--small-color-block-aliases']
    if getattr(a, 'lightmap_block_fallback', False): chain += ['--lightmap-block-fallback']
    if getattr(a, 'transparent_atlas_scrub', False): chain += ['--transparent-atlas-scrub']
    if shared0: chain += ['--shared0-fonts']
    if getattr(a, 'gradle_online', False): chain += ['--gradle-online']
    for attr in ('version_code', 'version_name'):
        value = getattr(a, attr, None)
        if value is not None: chain += ['--' + attr.replace('_', '-'), value]
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
        if getattr(a, 'normal_block_fallback', False): windows_chain += ['--normal-block-fallback']
        if getattr(a, 'normal_block_extra', False): windows_chain += ['--normal-block-extra']
        if getattr(a, 'color_block_fallback', False): windows_chain += ['--color-block-fallback']
        if getattr(a, 'emission_block_fallback', False): windows_chain += ['--emission-block-fallback']
        if getattr(a, 'bullseye_block_fallback', False): windows_chain += ['--bullseye-block-fallback']
        if getattr(a, 'animation_curve_fallback', False): windows_chain += ['--animation-curve-fallback']
        if getattr(a, 'animation_curve_relaxed', False): windows_chain += ['--animation-curve-relaxed']
        if getattr(a, 'animation_curve_extended', False): windows_chain += ['--animation-curve-extended']
        if getattr(a, 'animation_curve_hybrid', False): windows_chain += ['--animation-curve-hybrid']
        if getattr(a, 'animation_curve_reordered', False): windows_chain += ['--animation-curve-reordered']
        if getattr(a, 'reordered_animation_bindings', False): windows_chain += ['--reordered-animation-bindings']
        if getattr(a, 'neighboring_animation_keys', False): windows_chain += ['--neighboring-animation-keys']
        if a.font_fallback: windows_chain += ['--font-fallback']
        if a.resource_fallback: windows_chain += ['--resource-fallback']
        if getattr(a, 'nodraw_fallback', False): windows_chain += ['--nodraw-fallback']
        if getattr(a, 'atlas_fallback', False): windows_chain += ['--atlas-fallback']
        if getattr(a, 'lightmap_fallback', False): windows_chain += ['--lightmap-fallback']
        if getattr(a, 'emission_fallback', False): windows_chain += ['--emission-fallback']
        if getattr(a, 'fox_fallback', False): windows_chain += ['--fox-fallback']
        if getattr(a, 'bounded_fallback', False): windows_chain += ['--bounded-fallback']
        if getattr(a, 'shadow_fallback', False): windows_chain += ['--shadow-fallback']
        if getattr(a, 'emoji_fallback', False): windows_chain += ['--emoji-fallback']
        if getattr(a, 'partial_rgba', False): windows_chain += ['--partial-rgba']
        if getattr(a, 'partial_normal_rgba', False): windows_chain += ['--partial-normal-rgba']
        if getattr(a, 'localization_cab', False): windows_chain += ['--localization-cab']
        if getattr(a, 'localization_small', False): windows_chain += ['--localization-small']
        if getattr(a, 'partial_etc2', False): windows_chain += ['--partial-etc2']
        if getattr(a, 'partial_etc2_small', False): windows_chain += ['--partial-etc2-small']
        if getattr(a, 'partial_etc2_tiny', False): windows_chain += ['--partial-etc2-tiny']
        if getattr(a, 'partial_etc2_aliases', False): windows_chain += ['--partial-etc2-aliases']
        if getattr(a, 'partial_etc2_fast', False): windows_chain += ['--partial-etc2-fast']
        if getattr(a, 'partial_etc2_blend', False): windows_chain += ['--partial-etc2-blend']
        if getattr(a, 'partial_etc2_byte', False): windows_chain += ['--partial-etc2-byte']
        if getattr(a, 'partial_etc2_rgba', False): windows_chain += ['--partial-etc2-rgba']
        if getattr(a, 'partial_etc2_extra', False): windows_chain += ['--partial-etc2-extra']
        if getattr(a, 'color_block_extra', False): windows_chain += ['--color-block-extra']
        if getattr(a, 'small_color_block_fallback', False): windows_chain += ['--small-color-block-fallback']
        if getattr(a, 'small_color_block_aliases', False): windows_chain += ['--small-color-block-aliases']
        if getattr(a, 'lightmap_block_fallback', False): windows_chain += ['--lightmap-block-fallback']
        if getattr(a, 'transparent_atlas_scrub', False): windows_chain += ['--transparent-atlas-scrub']
        if shared0: windows_chain += ['--shared0-fonts']
        if getattr(a, 'gradle_online', False): windows_chain += ['--gradle-online']
        for attr in ('version_code', 'version_name'):
            value = getattr(a, attr, None)
            if value is not None: windows_chain += ['--' + attr.replace('_', '-'), value]
        if a.gradle_user_home: windows_chain += ['--gradle-user-home', a.gradle_user_home]
        dual_args = SimpleNamespace(output=linux_research, steam_data=a.game / 'Megabonk_Data',
                                    unity_editor=a.unity.parent, runtime_obb=runtime_obb,
                                    runtime_apk=runtime_apk,
                                    gradle_project=gradle_project, unity_project=workspace,
                                    json_jar=a.json_jar, application_id=a.application_id, runner=a.runner,
                                    host_library=a.host_library, android_library=a.android_library, decoder=a.decoder,
                                    steam_zip=a.steam_zip, gradle_user_home=a.gradle_user_home, platform='linux', quality_textures=a.quality_textures, normal_fallback=a.normal_fallback, partial_rgba=getattr(a, 'partial_rgba', False), localization_cab=getattr(a, 'localization_cab', False), gradle_online=getattr(a, 'gradle_online', False), version_code=getattr(a, 'version_code', None), version_name=getattr(a, 'version_name', None))
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
    p.add_argument('--version-code', type=int, help='override the Android package version code')
    p.add_argument('--version-name', help='override the Android package version name')
    p.add_argument('--runner', help='optional wrapper for AssetRipper, Unity and native tools')
    p.add_argument('--gradle-online', action='store_true', help='allow Gradle to download missing dependencies')
    p.add_argument('--include-rewired', action='store_true', help='experimentally include local Rewired plugins in both Unity builds')
    p.add_argument('--quality-textures', action='store_true', help='experimentally use higher-quality ETC2 for selected textures')
    p.add_argument('--normal-fallback', action='store_true', help='experimentally recover RGB normal maps that pass every mip direction check')
    p.add_argument('--normal-block-fallback', action='store_true', help='experimentally substitute individually bounded normal-map blocks; requires --normal-fallback and --partial-etc2-byte')
    p.add_argument('--normal-block-extra', action='store_true', help='try extra ETC2 metrics for bounded normal blocks; requires --normal-block-fallback and --partial-etc2-extra')
    p.add_argument('--color-block-fallback', action='store_true', help='experimentally substitute bounded blocks in eight color textures; requires --partial-etc2-byte')
    p.add_argument('--emission-block-fallback', action='store_true', help='also substitute bounded emission blocks; requires --color-block-fallback and --partial-etc2-aliases')
    p.add_argument('--bullseye-block-fallback', action='store_true', help='also substitute bounded bullseye blocks; requires --color-block-fallback and --partial-etc2-small')
    p.add_argument('--animation-curve-fallback', action='store_true', help='experimentally substitute tightly bounded Steam animation curves')
    p.add_argument('--animation-curve-relaxed', action='store_true', help='use a 1e-4 curve bound; requires --animation-curve-fallback')
    p.add_argument('--animation-curve-extended', action='store_true', help='use a 1e-3 curve bound; requires --animation-curve-fallback')
    p.add_argument('--animation-curve-hybrid', action='store_true', help='also try per-curve substitutions; requires --animation-curve-fallback')
    p.add_argument('--animation-curve-reordered', action='store_true', help='also substitute bounded reordered curves; requires --animation-curve-hybrid and --reordered-animation-bindings')
    p.add_argument('--reordered-animation-bindings', action='store_true', help='match exact bytes in reordered Transform animation curves')
    p.add_argument('--neighboring-animation-keys', action='store_true', help='match exact bytes from adjacent keys in the same Transform channel; requires --reordered-animation-bindings')
    p.add_argument('--font-fallback', action='store_true', help='experimentally reconstruct a font atlas; requires --quality-textures')
    p.add_argument('--resource-fallback', action='store_true', help='experimentally reconstruct selected resource textures; requires --quality-textures')
    p.add_argument('--nodraw-fallback', action='store_true', help='also reconstruct the NoDraw debug grid; requires --resource-fallback')
    p.add_argument('--atlas-fallback', action='store_true', help='experimentally reconstruct two alpha atlases')
    p.add_argument('--lightmap-fallback', action='store_true', help='reconstruct six alpha lightmaps; requires --quality-textures')
    p.add_argument('--emission-fallback', action='store_true', help='experimentally reconstruct two emission textures')
    p.add_argument('--fox-fallback', action='store_true', help='reconstruct Fox texture; requires --shared0-fonts and --quality-textures')
    p.add_argument('--bounded-fallback', action='store_true', help='try bounded same-name texture conversions; requires --quality-textures')
    p.add_argument('--shadow-fallback', action='store_true', help='experimentally reconstruct a uniquely matching shadow; requires --quality-textures')
    p.add_argument('--emoji-fallback', action='store_true', help='experimentally reconstruct two emoji atlases; requires --quality-textures')
    p.add_argument('--partial-rgba', action='store_true', help='experimentally reconstruct exact partial RGBA texture bytes')
    p.add_argument('--partial-normal-rgba', action='store_true', help='match exact bytes in WaterNormal using DXT5nm unpacking; requires --partial-rgba')
    p.add_argument('--localization-cab', action='store_true', help='experimentally reconstruct Android localization bundles; requires --partial-rgba')
    p.add_argument('--localization-small', action='store_true', help='also match 12-byte localization fragments; requires --localization-cab')
    p.add_argument('--partial-etc2', action='store_true', help='also reconstruct exact partial ETC2 bytes; requires --partial-rgba and --quality-textures')
    p.add_argument('--partial-etc2-small', action='store_true', help='also attempt smaller ETC2 textures; requires --partial-etc2')
    p.add_argument('--partial-etc2-tiny', action='store_true', help='also match exact bytes in tiny ETC2 textures; requires --partial-etc2-small and --partial-etc2-byte')
    p.add_argument('--partial-etc2-aliases', action='store_true', help='also match unambiguous ETC2 aliases; requires --partial-etc2-tiny')
    p.add_argument('--partial-etc2-fast', action='store_true', help='experimentally select the fast encoder where it recovers more exact bytes; requires --partial-etc2')
    p.add_argument('--partial-etc2-blend', action='store_true', help='experimentally choose the encoder per output block; requires --partial-etc2-fast')
    p.add_argument('--partial-etc2-byte', action='store_true', help='experimentally choose the encoder per output byte; requires --partial-etc2-blend')
    p.add_argument('--partial-etc2-rgba', action='store_true', help='also use RGBA error metric to recover exact ETC2 bytes; requires --partial-etc2-byte')
    p.add_argument('--partial-etc2-extra', action='store_true', help='try additional ETC2 metrics; requires --partial-etc2-rgba')
    p.add_argument('--color-block-extra', action='store_true', help='try extra ETC2 metrics for bounded color blocks; requires --color-block-fallback and --partial-etc2-extra')
    p.add_argument('--small-color-block-fallback', action='store_true', help='experimentally substitute bounded small color blocks; requires --quality-textures, --partial-etc2-tiny and --partial-etc2-extra')
    p.add_argument('--small-color-block-aliases', action='store_true', help='also reconstruct unambiguous renamed small colors; requires --small-color-block-fallback and --partial-etc2-aliases')
    p.add_argument('--lightmap-block-fallback', action='store_true', help='experimentally replace bounded blocks in two LDR lightmaps; requires --small-color-block-fallback and --partial-etc2-aliases')
    p.add_argument('--transparent-atlas-scrub', action='store_true', help='experimentally clear RGB in fully transparent atlas interiors')
    p.add_argument('--shared0-fonts', action='store_true', help='experimentally relocate sharedassets0 and reconstruct selected fonts and the default skybox')
    p.add_argument('--check', action='store_true', help='check only initial inputs')
    a = p.parse_args(); output = a.output.expanduser().absolute()
    if a.localization_cab and not a.partial_rgba: p.error('--localization-cab requires --partial-rgba')
    if a.partial_normal_rgba and not a.partial_rgba: p.error('--partial-normal-rgba requires --partial-rgba')
    if a.localization_small and not a.localization_cab: p.error('--localization-small requires --localization-cab')
    if a.normal_block_fallback and (not a.normal_fallback or not a.partial_etc2_byte): p.error('--normal-block-fallback requires --normal-fallback and --partial-etc2-byte')
    if a.normal_block_extra and not (a.normal_block_fallback and a.partial_etc2_extra): p.error('--normal-block-extra requires --normal-block-fallback and --partial-etc2-extra')
    if a.color_block_fallback and not a.partial_etc2_byte: p.error('--color-block-fallback requires --partial-etc2-byte')
    if a.emission_block_fallback and (not a.color_block_fallback or not a.partial_etc2_aliases): p.error('--emission-block-fallback requires --color-block-fallback and --partial-etc2-aliases')
    if a.bullseye_block_fallback and (not a.color_block_fallback or not a.partial_etc2_small): p.error('--bullseye-block-fallback requires --color-block-fallback and --partial-etc2-small')
    if a.version_code is not None and not 1 <= a.version_code <= 2100000000: p.error('invalid Android version code')
    if a.version_name is not None and not re.fullmatch(r'[A-Za-z0-9][A-Za-z0-9._+\-]{0,99}', a.version_name, re.ASCII): p.error('invalid Android version name')
    if a.animation_curve_relaxed and not a.animation_curve_fallback: p.error('--animation-curve-relaxed requires --animation-curve-fallback')
    if a.animation_curve_extended and (not a.animation_curve_fallback or a.animation_curve_relaxed): p.error('--animation-curve-extended requires --animation-curve-fallback and excludes --animation-curve-relaxed')
    if a.animation_curve_hybrid and not a.animation_curve_fallback: p.error('--animation-curve-hybrid requires --animation-curve-fallback')
    if a.animation_curve_reordered and not (a.animation_curve_hybrid and a.reordered_animation_bindings): p.error('--animation-curve-reordered requires --animation-curve-hybrid and --reordered-animation-bindings')
    if a.neighboring_animation_keys and not a.reordered_animation_bindings: p.error('--neighboring-animation-keys requires --reordered-animation-bindings')
    if a.font_fallback and not a.quality_textures: p.error('--font-fallback requires --quality-textures')
    if a.resource_fallback and not a.quality_textures: p.error('--resource-fallback requires --quality-textures')
    if a.nodraw_fallback and not a.resource_fallback: p.error('--nodraw-fallback requires --resource-fallback')
    if a.atlas_fallback and a.transparent_atlas_scrub: p.error('--atlas-fallback and --transparent-atlas-scrub cannot be combined')
    if a.lightmap_fallback and not a.quality_textures: p.error('--lightmap-fallback requires --quality-textures')
    if a.fox_fallback and (not a.shared0_fonts or not a.quality_textures): p.error('--fox-fallback requires --shared0-fonts and --quality-textures')
    if a.bounded_fallback and not a.quality_textures: p.error('--bounded-fallback requires --quality-textures')
    if a.shadow_fallback and not a.quality_textures: p.error('--shadow-fallback requires --quality-textures')
    if a.emoji_fallback and not a.quality_textures: p.error('--emoji-fallback requires --quality-textures')
    if a.partial_etc2 and (not a.partial_rgba or not a.quality_textures): p.error('--partial-etc2 requires --partial-rgba and --quality-textures')
    if a.partial_etc2_small and not a.partial_etc2: p.error('--partial-etc2-small requires --partial-etc2')
    if a.partial_etc2_tiny and (not a.partial_etc2_small or not a.partial_etc2_byte): p.error('--partial-etc2-tiny requires --partial-etc2-small and --partial-etc2-byte')
    if a.partial_etc2_aliases and not a.partial_etc2_tiny: p.error('--partial-etc2-aliases requires --partial-etc2-tiny')
    if a.partial_etc2_fast and not a.partial_etc2: p.error('--partial-etc2-fast requires --partial-etc2')
    if a.partial_etc2_blend and not a.partial_etc2_fast: p.error('--partial-etc2-blend requires --partial-etc2-fast')
    if a.partial_etc2_byte and not a.partial_etc2_blend: p.error('--partial-etc2-byte requires --partial-etc2-blend')
    if a.partial_etc2_rgba and not a.partial_etc2_byte: p.error('--partial-etc2-rgba requires --partial-etc2-byte')
    if a.partial_etc2_extra and not a.partial_etc2_rgba: p.error('--partial-etc2-extra requires --partial-etc2-rgba')
    if a.color_block_extra and not (a.color_block_fallback and a.partial_etc2_extra): p.error('--color-block-extra requires --color-block-fallback and --partial-etc2-extra')
    if a.small_color_block_fallback and not (a.quality_textures and a.partial_etc2_tiny and a.partial_etc2_extra): p.error('--small-color-block-fallback requires --quality-textures, --partial-etc2-tiny and --partial-etc2-extra')
    if a.small_color_block_aliases and not (a.small_color_block_fallback and a.partial_etc2_aliases): p.error('--small-color-block-aliases requires --small-color-block-fallback and --partial-etc2-aliases')
    if a.lightmap_block_fallback and not (a.small_color_block_fallback and a.partial_etc2_aliases): p.error('--lightmap-block-fallback requires --small-color-block-fallback and --partial-etc2-aliases')
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
        try:
            setattr(a, name, value.expanduser().resolve(strict=True))
        except OSError as error:
            p.error(f"cannot access --{name.replace('_', '-')}: {error}")
    if a.windows_game:
        free = shutil.disk_usage(a.output.parent).free
        if free < 35 * 1024**3:
            print(f'Warning: output filesystem has {free / 1024**3:.1f} GiB free; '
                  'the dual-platform build may need at least 35 GiB. See the build quickstart.', file=sys.stderr)
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
    try:
        with zipfile.ZipFile(a.json_jar) as jar:
            for name in ('JSONObject', 'JSONArray', 'JSONTokener', 'JSONException'):
                if not jar.read('org/json/' + name + '.class'):
                    raise ValueError('empty JSON class')
    except (OSError, ValueError, KeyError, zipfile.BadZipFile, RuntimeError):
        p.error('JSON JAR must contain readable org.json classes; see the build quickstart')
    android = a.unity.parent / 'Data/PlaybackEngines/AndroidPlayer'
    for name in ('java', 'javac'):
        tool = android / 'OpenJDK/bin' / name
        if not tool.is_file() or not os.access(tool, os.X_OK):
            p.error('Unity Android JDK requires executable ' + name)
    aapt2 = android / 'SDK/build-tools/34.0.0/aapt2'
    if not aapt2.is_file() or not os.access(aapt2, os.X_OK) or any(c.isspace() for c in str(aapt2)):
        p.error('Unity Android build-tools 34.0.0 executable aapt2 is required at a path without spaces')
    if a.android_library is None:
        compiler = android / 'NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        if not compiler.is_file() or not os.access(compiler, os.X_OK):
            p.error('Unity Android ARM64 NDK C++ compiler is required to build the texture converter')
    if a.host_library is None and not shutil.which('g++'):
        p.error('g++ is required to build the host texture converter')
    if a.decoder is None and not shutil.which(os.environ.get('CXX', 'g++')):
        p.error('C++ compiler specified by CXX (or g++) is required to build the Crunch decoder')
    try:
        check_conversion_imports()
    except ValueError as error:
        p.error(str(error) + '; install tools/requirements-conversion.txt and ensure native libraries are available to Python')
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
    try:
        subprocess.run(preflight, check=True)
    except subprocess.CalledProcessError as error:
        p.error(f'Unity/export preflight failed (status {error.returncode}); see the error above')
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
    apk = a.output / result / 'gradle/launcher/build/outputs/apk/release/launcher-release.apk'
    try:
        check_online_apk(aapt2, apk, a.application_id, a.runner)
        check_apk_version(aapt2, apk, a.version_code, a.version_name, a.runner)
    except ValueError as error:
        raise SystemExit(f'{error}; local outputs and log retained at {a.output}') from error
    print('Private research APK created:', apk, flush=True)
    print('The APK, ZIP and intermediate files contain game data and must not be distributed.', flush=True)


if __name__ == '__main__': main()
