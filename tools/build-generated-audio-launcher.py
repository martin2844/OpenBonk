#!/usr/bin/env python3
"""Build a local-only Android launcher prototype for a generated audio plan.

The template and resulting APK contain game-derived data and must stay local.
This prototype is not a release build and uses Unity's debug signing setup.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import zipfile


ROOT = Path(__file__).resolve().parent.parent
JAVA = ROOT / 'analysis/data-conversion/android'


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def entry_digest(archive, name):
    with archive.open(name) as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def verify(template, plan_path, expected, runtime, gradle):
    plan = json.loads(plan_path.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-steam-audio-offsets' or plan.get('template_sha256') != digest(template):
        raise ValueError('audio plan or template differs')
    rows = {row['entry']: row for row in plan['records']}
    if not rows or len(rows) != len(plan['records']):
        raise ValueError('audio plan contains duplicate or missing records')
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(expected) as result, zipfile.ZipFile(runtime) as player:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist():
            raise ValueError('template and expected archive entries differ')
        if not set(rows).issubset(names):
            raise ValueError('audio plan references missing entries')
        guid = base.read('unity_obb_guid')
        if len(guid) != 36 or guid != result.read('unity_obb_guid') or guid != player.read('assets/unity_obb_guid'):
            raise ValueError('runtime and OBB expansion identifiers differ')
        for name in names:
            if name in rows:
                row = rows[name]
                with base.open(name) as stream:
                    size = 0
                    while block := stream.read(65536):
                        size += len(block)
                        if any(block):
                            raise ValueError('audio template is not blank')
                if size != row['size'] or entry_digest(result, name) != row['sha256']:
                    raise ValueError('expected audio differs from plan')
            elif entry_digest(base, name) != entry_digest(result, name):
                raise ValueError('unrelated expected OBB entry differs')
        for name in ('ScriptingAssemblies.json', 'RuntimeInitializeOnLoads.json', 'Managed/Metadata/global-metadata.dat'):
            file = gradle / 'unityLibrary/src/main/assets/bin/Data' / name
            if not file.is_file() or digest(file) != entry_digest(player, 'assets/bin/Data/' + name):
                raise ValueError('Gradle project and runtime code metadata differ: ' + name)
        if 'lib/arm64-v8a/libil2cpp.so' not in player.namelist():
            raise ValueError('runtime lacks ARM64 IL2CPP library')
    return guid


def property_value(value):
    result = []
    for c in str(value):
        if c in '\\ =:#!': result.append('\\' + c)
        elif ord(c) < 32 or ord(c) > 126:
            raw = c.encode('utf-16-be')
            result.extend('\\u%04x' % int.from_bytes(raw[i:i + 2], 'big') for i in range(0, len(raw), 2))
        else: result.append(c)
    return ''.join(result)


def replace_once(path, pattern, replacement, dotall=False):
    text = path.read_text(encoding='utf-8')
    text, count = re.subn(pattern, lambda _: replacement, text, flags=re.MULTILINE | (re.DOTALL if dotall else 0))
    if count != 1:
        raise ValueError('unexpected Gradle project structure: ' + path.name)
    path.write_text(text, encoding='utf-8')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('gradle-project', 'unity-project', 'runtime-apk', 'audio-template', 'audio-plan', 'expected-obb', 'unity-editor', 'output'):
        p.add_argument('--' + name, type=Path, required=True)
    p.add_argument('--application-id', required=True)
    p.add_argument('--runner', help='optional executable such as steam-run')
    p.add_argument('--gradle-user-home', type=Path, help='existing local Gradle cache')
    p.add_argument('--check', action='store_true')
    a = p.parse_args()
    gradle, project, runtime, template, plan, expected, editor = [x.expanduser().resolve(strict=True) for x in
        (a.gradle_project, a.unity_project, a.runtime_apk, a.audio_template, a.audio_plan, a.expected_obb, a.unity_editor)]
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (ROOT, gradle, project, runtime, template, plan, expected)):
        p.error('output must be separate from source and inputs')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII): p.error('invalid application ID')
    if a.runner and not shutil.which(a.runner): p.error('configured runner not found')
    android = editor / 'Data/PlaybackEngines/AndroidPlayer'
    java, gradle_jar = android / 'OpenJDK/bin/java', android / 'Tools/gradle/lib/gradle-launcher-7.6.jar'
    for path in (java, gradle_jar, android / 'SDK/build-tools/34.0.0/aapt2', gradle / 'settings.gradle', gradle / 'unityLibrary/build.gradle', gradle / 'launcher/build.gradle'):
        if not path.is_file(): p.error('required build input is missing: ' + path.name)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}:
        p.error('Unity project must be an isolated workspace')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines():
        p.error('Unity project requires version 2023.2.22f1')
    if a.gradle_user_home and not a.gradle_user_home.expanduser().resolve(strict=True).is_dir(): p.error('Gradle cache must be a directory')
    for path in gradle.rglob('*'):
        if path.is_symlink(): p.error('Gradle project must not contain symbolic links')
    guid = verify(template, plan, expected, runtime, gradle)
    print('Local prototype prerequisites verified; output contains game data.', flush=True)
    if a.check:
        print('Check only; no APK was built.')
        return
    output.mkdir(mode=0o700)
    work = output / 'gradle'
    shutil.copytree(gradle, work, ignore=shutil.ignore_patterns('build', '.gradle', 'symbols'))
    library = work / 'unityLibrary'
    source = library / 'src/main/java'
    launcher = source / 'com/megabonk/launcher'
    dataprep = source / 'com/openbonk/dataprep'
    dataprep.mkdir(parents=True, exist_ok=True)
    for file in JAVA.glob('Steam*.java'): shutil.copy2(file, dataprep / file.name)
    shutil.copy2(JAVA / 'LauncherDiagnosticGenerator.java', launcher / 'LauncherDiagnosticGenerator.java')
    assets = library / 'src/main/assets/openbonk-diagnostic'
    assets.mkdir()
    (assets / 'marker').write_bytes(b'OPENBONK_PRIVATE_DIAGNOSTIC_V1')
    (assets / 'generated-audio-mode').write_bytes(b'OPENBONK_GENERATED_AUDIO_RESEARCH_V1')
    shutil.copyfile(template, assets / 'generated-audio-template.obb')
    shutil.copyfile(plan, assets / 'generated-audio-plan.json')
    (assets / 'generated-audio-plan.sha256').write_text(digest(plan), encoding='ascii')
    (library / 'src/main/assets/unity_obb_guid').write_bytes(guid)
    result_hash = digest(expected)
    (launcher / 'SplitExpectedData.java').write_text('package com.megabonk.launcher;\nfinal class SplitExpectedData { static final String SHA256 = "' + result_hash + '", LEGACY_SHA256 = "", SECOND_LEGACY_SHA256 = "", THIRD_LEGACY_SHA256 = "", FOURTH_LEGACY_SHA256 = "", FIFTH_LEGACY_SHA256 = "", SIXTH_LEGACY_SHA256 = ""; }\n', encoding='ascii')
    replace_once(work / 'launcher/build.gradle', r'applicationId\s+"[^"\n]+"', 'applicationId "' + a.application_id + '"')
    replace_once(library / 'build.gradle', r'    afterEvaluate \{\n.*?\n    \}\n    sourceSets', '    sourceSets', dotall=True)
    ndk = str(android / 'NDK')
    if any(ord(c) < 32 for c in ndk): raise ValueError('invalid NDK path')
    for module in ('launcher', 'unityLibrary'):
        replace_once(work / module / 'build.gradle', r'^[ \t]*ndkPath[ \t]+[^\n]+$', '    ndkPath "' + ndk.replace('\\', '\\\\').replace('"', '\\"') + '"')
    (work / 'local.properties').write_text('sdk.dir=' + property_value(android / 'SDK') + '\n', encoding='ascii')
    properties = work / 'gradle.properties'
    replace_once(properties, r'^unityProjectPath=.*$', 'unityProjectPath=' + property_value(project))
    replace_once(properties, r'^android\.aapt2FromMavenOverride=.*$', 'android.aapt2FromMavenOverride=' + property_value(android / 'SDK/build-tools/34.0.0/aapt2'))
    jni = library / 'src/main/jniLibs/arm64-v8a/libil2cpp.so'
    if jni.exists() or jni.is_symlink(): raise ValueError('unexpected existing IL2CPP library')
    with zipfile.ZipFile(runtime) as apk, jni.open('xb') as target, apk.open('lib/arm64-v8a/libil2cpp.so') as source_file:
        shutil.copyfileobj(source_file, target)
    env = os.environ.copy()
    env['JAVA_HOME'] = str(android / 'OpenJDK')
    env['PATH'] = str(android / 'OpenJDK/bin') + os.pathsep + env.get('PATH', '')
    if a.gradle_user_home: env['GRADLE_USER_HOME'] = str(a.gradle_user_home.expanduser().resolve(strict=True))
    command = ([a.runner] if a.runner else []) + [str(java), '-classpath', str(gradle_jar), 'org.gradle.launcher.GradleMain', '--offline', '--no-daemon', ':launcher:assembleRelease']
    with (output / 'gradle.log').open('x', encoding='utf-8') as log:
        status = subprocess.run(command, cwd=work, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
    if status: raise SystemExit(f'Gradle failed ({status}); local output and log retained at {output}')
    apk = work / 'launcher/build/outputs/apk/release/launcher-release.apk'
    if not apk.is_file() or not apk.stat().st_size: raise SystemExit('Gradle did not produce the expected APK')
    print('Local research APK created:', apk, flush=True)
    print('It contains game-derived data and must not be distributed.', flush=True)


if __name__ == '__main__':
    main()
