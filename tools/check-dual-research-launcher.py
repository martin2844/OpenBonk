#!/usr/bin/env python3
"""Verify both import paths using the plans packaged in a private research APK.

Inputs and temporary files contain game data. This checks byte-for-byte OBB
reconstruction, not Android UI, rendering, controllers, mods or distribution.
"""
import argparse
import os
from pathlib import Path, PurePosixPath
import shutil
import stat
import subprocess
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/openbonk-diagnostic/'
MAX_ASSET = 1024 * 1024 * 1024
MAX_TOTAL = 2 * MAX_ASSET


def diagnostic_entries(archive):
    result, names, total = [], set(), 0
    for info in archive.infolist():
        if not info.filename.startswith(PREFIX) or info.is_dir():
            continue
        name = info.filename[len(PREFIX):]
        path = PurePosixPath(name)
        if (not name or '\\' in name or path.is_absolute() or
                any(part in ('', '.', '..') for part in name.split('/')) or
                name in names or stat.S_ISLNK(info.external_attr >> 16)):
            raise ValueError('unsafe or duplicate diagnostic asset')
        names.add(name)
        total += info.file_size
        if info.file_size > MAX_ASSET or total > MAX_TOTAL or len(names) > 200:
            raise ValueError('diagnostic assets exceed extraction limits')
        result.append((info, path))
    if not {'dual-platform', 'generated-audio-mode', 'windows/generated-audio-mode'} <= names:
        raise ValueError('APK is missing dual-platform diagnostic assets')
    return result


def sources():
    data = ROOT / 'analysis/data-conversion/android'
    fixture = ROOT / 'tools/generated-audio-integration-check'
    launcher = ROOT / 'unity/OpenBonk/Assets/AndroidSplitLauncher'
    return (sorted(p for p in data.glob('*.java') if not p.name.startswith('Arm')) +
            [fixture / 'android/content/Context.java', fixture / 'android/content/res/AssetManager.java',
             fixture / 'com/megabonk/launcher/DualResearchIntegrationCheck.java'] +
            [launcher / name for name in ('LauncherPreparedSteamImport.java', 'SplitFileRecovery.java', 'LauncherDataBundle.java')])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('apk', 'linux-zip', 'windows-zip', 'expected-obb', 'host-library', 'json-jar', 'java-home', 'workdir'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--runner', help='optional executable used to run Java, e.g. steam-run')
    p.add_argument('--check', action='store_true', help='check initial inputs only')
    a = p.parse_args()
    for key in ('apk', 'linux_zip', 'windows_zip', 'expected_obb', 'host_library', 'json_jar', 'java_home', 'workdir'):
        try: setattr(a, key, getattr(a, key).expanduser().resolve(strict=True))
        except OSError: p.error(key + ' does not exist')
    java, javac = a.java_home / 'bin/java', a.java_home / 'bin/javac'
    inputs = [a.apk, a.linux_zip, a.windows_zip, a.expected_obb, a.host_library, a.json_jar, java, javac]
    src = sources()
    if not a.workdir.is_dir() or not src or any(not f.is_file() for f in inputs + src):
        p.error('required files or work directory are missing')
    if a.host_library.name != 'libopenbonk_texture.so':
        p.error('host library must be named libopenbonk_texture.so')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner: p.error('runner not found')
    try:
        with zipfile.ZipFile(a.apk) as archive: diagnostic_entries(archive)
        with zipfile.ZipFile(a.json_jar) as archive:
            if 'org/json/JSONObject.class' not in archive.namelist(): raise ValueError('JSON JAR is missing JSONObject')
    except (OSError, ValueError, zipfile.BadZipFile) as error:
        p.error(str(error))
    if a.check:
        print('Initial inputs checked; Java compilation and OBB reconstruction remain unverified.')
        return
    command = [runner] if runner else []
    with tempfile.TemporaryDirectory(prefix='.openbonk-dual-check-', dir=a.workdir) as temp:
        root = Path(temp); classes = root / 'classes'; classes.mkdir()
        assets = root / 'assets'; assets.mkdir()
        with zipfile.ZipFile(a.apk) as archive:
            for info, path in diagnostic_entries(archive):
                target = assets / 'openbonk-diagnostic' / path
                target.parent.mkdir(parents=True, exist_ok=True)
                with archive.open(info) as source, target.open('xb') as output: shutil.copyfileobj(source, output, 1024 * 1024)
        subprocess.run(command + [str(javac), '--release', '8', '-cp', str(a.json_jar), '-d', str(classes), *map(str, src)], check=True)
        subprocess.run(command + [str(java), '-Djava.library.path=' + str(a.host_library.parent),
                       '-cp', os.pathsep.join((str(classes), str(a.json_jar))),
                       'com.megabonk.launcher.DualResearchIntegrationCheck', str(assets), str(root / 'cache'),
                       str(a.linux_zip), str(a.windows_zip), str(a.expected_obb)], check=True)
    print('Temporary verification files removed; interactive behavior was not tested.')


if __name__ == '__main__': main()
