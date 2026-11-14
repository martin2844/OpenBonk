#!/usr/bin/env python3
"""Build the private seven-stage research launcher from local runtime inputs.

The OBBs, Steam ZIP, plans and APK remain private: templates still contain game
data and several conversions require visual review. This does not produce a
redistributable beta or construct the initial Unity runtime/Gradle export.
Requires UnityPy and its texture dependencies in the active Python environment.
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
import zipfile

ROOT = Path(__file__).resolve().parent.parent
TOOLS = ROOT / 'tools'
JAVA = ROOT / 'analysis/data-conversion/android'
FIXTURES = ROOT / 'tools/texture-native-comparison/com/openbonk/dataprep'
STEAM_INPUTS = TOOLS / 'launcher-steam-inputs.json'


def validate_steam_inputs(steam_data, steam_zip=None, platform='linux'):
    """Check the selected platform's data and optional ZIP before conversion."""
    if platform not in ('linux', 'windows'): raise ValueError('invalid Steam platform')
    inventory = json.loads(STEAM_INPUTS.read_text(encoding='utf-8'))['platforms'][platform]
    folder = 'StandaloneLinux64' if platform == 'linux' else 'StandaloneWindows64'
    if not (steam_data / 'StreamingAssets/aa' / folder).is_dir():
        raise ValueError('research chain requires ' + platform + ' Steam data for the selected platform')
    if not isinstance(inventory, dict) or not inventory: raise ValueError('invalid Steam input inventory')
    expected = {}; total = 0
    for name in inventory:
        parts = name.split('/')
        if any(not part or part in ('.', '..') or '\\' in part or ':' in part for part in parts): raise ValueError('invalid input inventory path')
        path = steam_data.joinpath(*parts)
        if not path.is_file() or not path.resolve().is_relative_to(steam_data): raise ValueError('required Steam input is missing for ' + platform)
        size = path.stat().st_size; total += size
        if size <= 0 or total > 1024 * 1024 * 1024: raise ValueError('Steam inputs exceed size limit')
        with path.open('rb') as stream:
            expected[name] = (size, hashlib.file_digest(stream, 'sha256').hexdigest())
    if steam_zip is None: return
    with zipfile.ZipFile(steam_zip) as archive:
        infos = archive.infolist(); names = [i.filename for i in infos]
        if len(names) > 20000 or len(names) != len(set(names)): raise ValueError('invalid Steam ZIP entries')
        for item in infos:
            if item.is_dir(): continue
            if any(not part or part in ('.', '..') or '\\' in part or ':' in part for part in item.filename.split('/')):
                raise ValueError('unsafe Steam ZIP entry')
        root = None
        for name, (size, digest) in expected.items():
            matches = [i for i in infos if i.filename == name or i.filename == 'Megabonk_Data/' + name or i.filename.endswith('/Megabonk_Data/' + name)]
            if len(matches) != 1: raise ValueError('Steam ZIP is missing or duplicates a required ' + platform + ' input')
            item = matches[0]; prefix = item.filename[:-len(name)]
            if root is not None and root != prefix: raise ValueError('Steam ZIP has conflicting game data roots')
            root = prefix
            if item.file_size != size: raise ValueError('Steam ZIP input differs from local ' + platform + ' data')
            with archive.open(item) as stream:
                if hashlib.file_digest(stream, 'sha256').hexdigest() != digest: raise ValueError('Steam ZIP input differs from local ' + platform + ' data')


def commands(a):
    """Create ordered commands; paths inside the new output are private."""
    out, steam = a.output, a.steam_data
    native = out / 'native'; plans = out / 'plans'; results = out / 'canonical'
    jdk = a.unity_editor / 'Data/PlaybackEngines/AndroidPlayer/OpenJDK'
    android = a.unity_editor / 'Data/PlaybackEngines/AndroidPlayer'
    hostlib = a.host_library or native / 'libopenbonk_texture.so'
    armlib = a.android_library or native / 'libopenbonk_texture_arm64.so'
    decoder = a.decoder or native / 'crunch-decoder'
    steamzip = a.steam_zip or out / 'steam.zip'
    python = sys.executable
    wrapped = ['--runner', a.runner] if a.runner else []
    quality = getattr(a, 'quality_textures', False)
    normal_fallback = getattr(a, 'normal_fallback', False)
    prefix = [a.runner] if a.runner else []
    steps = []
    def py(label, name, *args): steps.append((label, [python, str(TOOLS / name), *map(str, args)]))
    def java(label, cls, *args):
        cp = os.pathsep.join((str(out / 'classes'), str(a.json_jar)))
        steps.append((label, [*prefix, str(jdk / 'bin/java'), '-cp', cp, 'com.openbonk.dataprep.' + cls, *map(str, args)]))
    def rebind(label, plan, original, canonical, dest):
        py(label, 'rebind-audio-plan.py', '--plan', plan, '--original-template', original, '--canonical-template', canonical, '--output', dest)
    def toolplan(label, script, source_obb, dest, *extra):
        py(label, script, 'generate', '--steam-data', steam, '--obb', source_obb, '--output', dest, *extra)
    if a.steam_zip is None: py('Steam ZIP', 'create-launcher-steam-zip.py', steam, steamzip, '--platform', a.platform)
    if a.host_library is None:
        if quality: py('host texture converter', 'build-etc2-quality-jni.py', '--target', 'host', '--java-home', jdk, '--output', hostlib, *wrapped)
        else: py('host texture converter', 'build-host-texture-library.py', '--java-home', jdk, '--output', hostlib, *wrapped)
    if a.android_library is None:
        if quality: py('Android texture converter', 'build-etc2-quality-jni.py', '--target', 'android', '--ndk', android / 'NDK', '--output', armlib, *wrapped)
        else: py('Android texture converter', 'build-android-texture-library.py', '--ndk', android / 'NDK', '--output', armlib, *wrapped)
    if a.decoder is None: py('Crunch decoder', 'build-crunch-decoder.py', decoder)
    py('individual Steam audio', 'experimental-steam-audio.py', '--steam-data', steam, '--obb', a.runtime_obb, '--output', out / 'individual-audio.obb')
    py('packed Steam audio', 'experimental-packed-steam-audio.py', '--steam-data', steam, '--obb', out / 'individual-audio.obb', '--output', out / 'packed-audio.obb')
    py('converted textures', 'experimental-converted-textures.py', '--steam-data', steam, '--obb', out / 'packed-audio.obb', '--output', out / 'texture-study', *(['--quality-library', hostlib] if quality else []), *(['--normal-fallback'] if normal_fallback else []), *(['--font-fallback'] if getattr(a, 'font_fallback', False) else []), *(['--resource-fallback'] if getattr(a, 'resource_fallback', False) else []), *(['--shadow-fallback'] if getattr(a, 'shadow_fallback', False) else []))
    py('converted cubemaps', 'experimental-converted-cubemaps.py', '--steam-data', steam, '--obb', out / 'texture-study/converted.obb', '--decoder', decoder, '--output', out / 'cubemap-study', *wrapped)
    toolplan('individual audio plan', 'experimental-steam-audio-plan.py', out / 'cubemap-study/converted.obb', plans / 'audio')
    toolplan('packed audio plan', 'experimental-packed-audio-plan.py', plans / 'audio/template.obb', plans / 'packed')
    toolplan('texture plan', 'experimental-converted-texture-plan.py', plans / 'packed/template.obb', plans / 'texture', '--manifest', out / 'texture-study/report.json', *(['--quality-library', hostlib] if quality else []))
    toolplan('mesh plan', 'experimental-mesh-buffer-plan.py', plans / 'texture/template.obb', plans / 'mesh')
    toolplan('cubemap plan', 'experimental-cubemap-plan.py', plans / 'mesh/template.obb', plans / 'cubemap', '--manifest', out / 'cubemap-study/report.json', '--decoder', decoder, *wrapped)
    toolplan('animation plan', 'experimental-animation-fragment-plan.py', plans / 'cubemap/template.obb', plans / 'animation')
    toolplan('exact object plan', 'experimental-exact-object-plan.py', plans / 'animation/template.obb', plans / 'objects')
    classes = ['SteamPlannedMeshes.java', 'SteamPlannedTextures.java', 'SteamPackedAudio.java', 'SteamGeneratedAudio.java',
               'SteamPlannedObjectsCheck.java', 'SteamPlannedAnimationsCheck.java', 'SteamPlannedCubemapsCheck.java',
               'SteamPlannedMeshesCheck.java', 'SteamPlannedTexturesCheck.java', 'SteamPackedAudioCheck.java', 'SteamGeneratedAudioCheck.java']
    steps.append(('compile preparation checks', [*prefix, str(jdk / 'bin/javac'), '--release', '8', '-cp', str(a.json_jar), '-d', str(out / 'classes'),
                  str(FIXTURES / 'SteamConvertedTextures.java'), str(FIXTURES / 'SteamCubemapResources.java'), *[str(JAVA / name) for name in classes]]))
    obj = results / 'after-objects.obb'; anim = results / 'after-animation.obb'; cube = results / 'after-cubemap.obb'
    mesh = results / 'after-mesh.obb'; texture = results / 'after-texture.obb'; packed = results / 'after-packed.obb'; final = results / 'final.obb'
    java('restore and verify objects', 'SteamPlannedObjectsCheck', plans / 'objects/template.obb', steamzip, plans / 'objects/plan.json', obj, plans / 'animation/template.obb', 'pass')
    rebind('bind animation plan', plans / 'animation/plan.json', plans / 'animation/template.obb', obj, results / 'animation-plan.json')
    java('restore and verify animations', 'SteamPlannedAnimationsCheck', obj, steamzip, results / 'animation-plan.json', anim, plans / 'cubemap/template.obb', 'pass')
    rebind('bind cubemap plan', plans / 'cubemap/plan.json', plans / 'cubemap/template.obb', anim, results / 'cubemap-plan.json')
    java('restore and verify cubemaps', 'SteamPlannedCubemapsCheck', hostlib, anim, steamzip, results / 'cubemap-plan.json', cube, plans / 'mesh/template.obb', 'pass')
    rebind('bind mesh plan', plans / 'mesh/plan.json', plans / 'mesh/template.obb', cube, results / 'mesh-plan.json')
    java('restore and verify meshes', 'SteamPlannedMeshesCheck', cube, steamzip, results / 'mesh-plan.json', mesh, plans / 'texture/template.obb', 'pass')
    rebind('bind texture plan', plans / 'texture/plan.json', plans / 'texture/template.obb', mesh, results / 'texture-plan.json')
    java('restore and verify textures', 'SteamPlannedTexturesCheck', hostlib, mesh, steamzip, results / 'texture-plan.json', texture, plans / 'packed/template.obb', 'pass')
    rebind('bind packed audio plan', plans / 'packed/plan.json', plans / 'packed/template.obb', texture, results / 'packed-plan.json')
    java('restore and verify packed audio', 'SteamPackedAudioCheck', texture, steamzip, results / 'packed-plan.json', packed, plans / 'audio/template.obb', 'pass')
    rebind('bind audio plan', plans / 'audio/plan.json', plans / 'audio/template.obb', packed, results / 'audio-plan.json')
    java('restore and verify individual audio', 'SteamGeneratedAudioCheck', packed, steamzip, results / 'audio-plan.json', final, out / 'cubemap-study/converted.obb', 'pass')
    build = ['--gradle-project', a.gradle_project, '--unity-project', a.unity_project, '--runtime-apk', a.runtime_apk,
             '--audio-template', plans / 'objects/template.obb', '--audio-plan', results / 'audio-plan.json',
             '--packed-plan', results / 'packed-plan.json', '--intermediate-obb', packed,
             '--texture-plan', results / 'texture-plan.json', '--texture-intermediate-obb', texture, '--texture-library', armlib,
             '--mesh-plan', results / 'mesh-plan.json', '--mesh-intermediate-obb', mesh,
             '--cubemap-plan', results / 'cubemap-plan.json', '--cubemap-intermediate-obb', cube,
             '--animation-plan', results / 'animation-plan.json', '--animation-intermediate-obb', anim,
             '--object-plan', plans / 'objects/plan.json', '--object-intermediate-obb', obj,
             '--expected-obb', final, '--unity-editor', a.unity_editor, '--output', out / 'launcher', '--application-id', a.application_id, *wrapped]
    if a.gradle_user_home: build += ['--gradle-user-home', a.gradle_user_home]
    py('research launcher APK', 'build-generated-audio-launcher.py', *build)
    return steps


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'runtime-obb', 'runtime-apk', 'gradle-project', 'unity-project', 'unity-editor', 'json-jar', 'output'):
        p.add_argument('--' + name, required=True, type=Path)
    for name in ('steam-zip', 'host-library', 'android-library', 'decoder', 'gradle-user-home'):
        p.add_argument('--' + name, type=Path)
    p.add_argument('--application-id', required=True)
    p.add_argument('--platform', choices=('linux', 'windows'), default='linux', help='Steam data platform (Windows support is experimental)')
    p.add_argument('--quality-textures', action='store_true', help='experimentally use the high-quality ETC2 encoder for selected textures')
    p.add_argument('--normal-fallback', action='store_true', help='experimentally recover RGB normal maps that pass every mip direction check')
    p.add_argument('--font-fallback', action='store_true', help='experimentally reconstruct a font atlas; requires --quality-textures')
    p.add_argument('--resource-fallback', action='store_true', help='experimentally reconstruct selected resource textures; requires --quality-textures')
    p.add_argument('--shadow-fallback', action='store_true', help='experimentally select a unique shadow texture; requires --quality-textures')
    p.add_argument('--runner', help='optional wrapper for Unity JDK and native tools')
    p.add_argument('--check', action='store_true', help='validate initial inputs only')
    a = p.parse_args()
    if a.font_fallback and not a.quality_textures: p.error('--font-fallback requires --quality-textures')
    if a.resource_fallback and not a.quality_textures: p.error('--resource-fallback requires --quality-textures')
    if a.shadow_fallback and not a.quality_textures: p.error('--shadow-fallback requires --quality-textures')
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    a.output = output.parent.resolve() / output.name
    for name in ('steam_data', 'runtime_obb', 'runtime_apk', 'gradle_project', 'unity_project', 'unity_editor', 'json_jar',
                 'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home'):
        path = getattr(a, name)
        if path is None: continue
        if path.expanduser().is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, path.expanduser().resolve(strict=True))
    inputs = [getattr(a, n) for n in ('steam_data', 'runtime_obb', 'runtime_apk', 'gradle_project', 'unity_project', 'unity_editor', 'json_jar',
                                      'steam_zip', 'host_library', 'android_library', 'decoder', 'gradle_user_home') if getattr(a, n) is not None]
    if any(a.output.is_relative_to(x) or x.is_relative_to(a.output) for x in (ROOT, *inputs)): p.error('output must be separate from checkout and inputs')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII): p.error('invalid application ID')
    if a.runner and not shutil.which(a.runner): p.error('runner not found')
    for name in ('steam_data', 'gradle_project', 'unity_project', 'unity_editor'):
        if not getattr(a, name).is_dir(): p.error(name + ' must be a directory')
    for name in ('runtime_obb', 'runtime_apk', 'json_jar', 'steam_zip', 'host_library', 'android_library', 'decoder'):
        value = getattr(a, name)
        if value is not None and not value.is_file(): p.error(name + ' must be a file')
    if not zipfile.is_zipfile(a.runtime_obb) or not zipfile.is_zipfile(a.runtime_apk) or not zipfile.is_zipfile(a.json_jar): p.error('runtime and JSON inputs must be ZIP archives')
    if a.steam_zip and not zipfile.is_zipfile(a.steam_zip): p.error('Steam ZIP is invalid')
    try: validate_steam_inputs(a.steam_data, a.steam_zip, a.platform)
    except (OSError, ValueError, KeyError, zipfile.BadZipFile) as error: p.error(str(error))
    if not (a.unity_editor / 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java').is_file(): p.error('Unity Android JDK missing')
    if not (a.unity_editor / 'Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/javac').is_file(): p.error('Unity Android compiler missing')
    if importlib.util.find_spec('UnityPy') is None: p.error('UnityPy missing from active Python environment')
    steps = commands(a)
    print(f'Initial inputs checked; {len(steps)} local research stages planned.', flush=True)
    if a.check:
        print('Check only; source compatibility, conversion and APK build remain unverified.')
        return
    a.output.mkdir(mode=0o700)
    for name in ('native', 'plans', 'canonical', 'classes'): (a.output / name).mkdir(mode=0o700)
    with (a.output / 'pipeline.log').open('x', encoding='utf-8') as log:
        for i, (label, command) in enumerate(steps, 1):
            print(f'[{i}/{len(steps)}] {label}', flush=True)
            log.write(f'[{i}/{len(steps)}] {label}\n'); log.flush()
            status = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT).returncode
            if status: raise SystemExit(f'Stage failed: {label} (status {status}); local outputs and log retained at {a.output}')
    print('Private research APK created:', a.output / 'launcher/gradle/launcher/build/outputs/apk/release/launcher-release.apk', flush=True)
    print('This APK and its intermediates contain game-derived data and must not be distributed.', flush=True)


if __name__ == '__main__': main()
