#!/usr/bin/env python3
"""Verify a private research build's seven reconstruction stages on Android.

The research directory, Steam ZIP and device temporary files contain game data.
The script removes its temporary device directory, but does not test the
launcher, rendered output, controls, audio playback or gameplay.
"""
import argparse
from pathlib import Path
import re
import secrets
import shlex
import shutil
import subprocess
import tempfile


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('research', 'steam-zip', 'library', 'sdk', 'java-home'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--device', required=True)
    p.add_argument('--runner')
    a = p.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._:-]+', a.device): p.error('invalid device serial')
    for name in ('research', 'steam_zip', 'library', 'sdk', 'java_home'):
        path = getattr(a, name).expanduser()
        if path.is_symlink(): p.error(name + ' must not be a symbolic link')
        setattr(a, name, path.resolve(strict=True))
    root = a.research
    java, d8 = a.java_home / 'bin/java', a.sdk / 'build-tools/34.0.0/lib/d8.jar'
    classes = sorted((root / 'classes').rglob('*.class'))
    stages = [
        ('objects', 'SteamPlannedObjectsCheck', root / 'plans/objects/plan.json', 'after-objects.obb', False),
        ('animation', 'SteamPlannedAnimationsCheck', root / 'canonical/animation-plan.json', 'after-animation.obb', False),
        ('cubemap', 'SteamPlannedCubemapsCheck', root / 'canonical/cubemap-plan.json', 'after-cubemap.obb', True),
        ('mesh', 'SteamPlannedMeshesCheck', root / 'canonical/mesh-plan.json', 'after-mesh.obb', False),
        ('texture', 'SteamPlannedTexturesCheck', root / 'canonical/texture-plan.json', 'after-texture.obb', True),
        ('packed audio', 'SteamPackedAudioCheck', root / 'canonical/packed-plan.json', 'after-packed.obb', False),
        ('audio', 'SteamGeneratedAudioCheck', root / 'canonical/audio-plan.json', 'final.obb', False),
    ]
    files = [java, d8, a.steam_zip, a.library, root / 'plans/objects/template.obb']
    files += [path for _, _, plan, expected, _ in stages for path in (plan, root / 'canonical' / expected)]
    if not classes or any(not path.is_file() or path.is_symlink() for path in files + classes): p.error('research build or tool inputs are incomplete')
    adb = shutil.which('adb'); runner = shutil.which(a.runner) if a.runner else None
    if not adb or (a.runner and not runner): p.error('adb or runner missing')
    adb_cmd = [adb, '-s', a.device]
    state = subprocess.run(adb_cmd + ['get-state'], capture_output=True, text=True)
    if state.returncode or state.stdout.strip() != 'device': p.error('device is not connected and authorized')
    remote = '/data/local/tmp/openbonk-chain-' + secrets.token_hex(12)
    def shell(*args): subprocess.run(adb_cmd + ['shell', shlex.join(args)], check=True)
    def push(path, name):
        subprocess.run(adb_cmd + ['push', str(path), remote + '/' + name], check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    # Some runners isolate /tmp across invocations, so use a sibling of inputs.
    with tempfile.TemporaryDirectory(prefix='.openbonk-chain-dex-', dir=root.parent) as temp:
        subprocess.run(([runner] if runner else []) + [str(java), '-cp', str(d8), 'com.android.tools.r8.D8',
                       '--min-api', '26', '--output', temp, *map(str, classes)], check=True)
        shell('mkdir', remote)
        try:
            push(Path(temp) / 'classes.dex', 'classes.dex')
            push(a.steam_zip, 'steam.zip'); push(a.library, 'library.so')
            push(root / 'plans/objects/template.obb', 'input.obb')
            for label, cls, plan, expected, native in stages:
                print('Checking ' + label, flush=True)
                push(plan, 'plan.json'); push(root / 'canonical' / expected, 'expected.obb')
                args = ([remote + '/library.so'] if native else []) + [remote + '/input.obb', remote + '/steam.zip',
                        remote + '/plan.json', remote + '/output.obb', remote + '/expected.obb', 'pass']
                shell('env', 'CLASSPATH=' + remote + '/classes.dex', 'app_process', '/system/bin', 'com.openbonk.dataprep.' + cls, *args)
                shell('rm', remote + '/input.obb', remote + '/expected.obb', remote + '/plan.json')
                shell('mv', remote + '/output.obb', remote + '/input.obb')
        finally:
            if subprocess.run(adb_cmd + ['shell', 'rm', '-rf', remote], capture_output=True).returncode:
                print('Could not remove device temporary directory:', remote, flush=True)
    print('Seven stages matched their expected archive entries; interactive gameplay was not tested.')


if __name__ == '__main__': main()
