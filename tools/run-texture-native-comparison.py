#!/usr/bin/env python3
"""Compare private texture mip fixtures on an explicitly chosen ARM device.

The fixture contains game data. The tool transfers it only to a temporary
directory on the selected authorized device and removes that directory.
"""
import argparse
import re
import secrets
import shutil
import struct
import subprocess
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / 'tools/texture-native-comparison/com/openbonk/dataprep/SteamConvertedTextures.java'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('library', 'fixtures', 'sdk', 'java-home'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--device', required=True)
    p.add_argument('--runner')
    args = p.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9._:-]+', args.device): p.error('invalid device serial')
    library, fixtures = (x.expanduser().absolute() for x in (args.library, args.fixtures))
    if any(x.is_symlink() or not x.is_file() for x in (library, fixtures)): p.error('library and fixtures must be regular files')
    if not 12 <= fixtures.stat().st_size <= 1024 * 1024 * 1024: p.error('invalid fixture size')
    with fixtures.open('rb') as stream:
        header = stream.read(12)
    count = struct.unpack('>I', header[8:])[0]
    if header[:8] != b'OBTX0001' or not 0 < count <= 50000: p.error('invalid fixture header')
    sdk, jdk = (x.expanduser().resolve(strict=True) for x in (args.sdk, args.java_home))
    javac, java, d8 = jdk / 'bin/javac', jdk / 'bin/java', sdk / 'build-tools/34.0.0/lib/d8.jar'
    adb = shutil.which('adb')
    runner = shutil.which(args.runner) if args.runner else None
    if not adb or not all(x.is_file() for x in (javac, java, d8, SOURCE)) or (args.runner and not runner): p.error('adb, compiler, build tools or runner missing')
    adb_cmd = [adb, '-s', args.device]
    state = subprocess.run(adb_cmd + ['get-state'], capture_output=True, text=True)
    if state.returncode or state.stdout.strip() != 'device': p.error('device is not connected and authorized')
    prefix = [runner] if runner else []
    # Some compatibility runners isolate /tmp across separate processes.
    with tempfile.TemporaryDirectory(prefix='.openbonk-native-texture-', dir=fixtures.parent) as tmp:
        root = Path(tmp); classes = root / 'classes'; dex = root / 'dex'; classes.mkdir(); dex.mkdir()
        subprocess.run(prefix + [str(javac), '--release', '8', '-d', str(classes), str(SOURCE)], check=True)
        package = classes / 'com/openbonk/dataprep'
        subprocess.run(prefix + [str(java), '-cp', str(d8), 'com.android.tools.r8.D8', '--min-api', '26', '--output', str(dex),
                                 str(package / 'SteamConvertedTextures.class'), str(package / 'SteamConvertedTextures$Counts.class')], check=True)
        remote = '/data/local/tmp/openbonk-texture-compare-' + secrets.token_hex(12)
        created = False
        try:
            subprocess.run(adb_cmd + ['shell', 'mkdir', remote], check=True, capture_output=True)
            created = True
            for local, name in ((dex / 'classes.dex', 'classes.dex'), (library, 'library.so'), (fixtures, 'fixtures.bin')):
                subprocess.run(adb_cmd + ['push', str(local), remote + '/' + name], check=True, capture_output=True)
            command = ('CLASSPATH=' + remote + '/classes.dex app_process /system/bin com.openbonk.dataprep.SteamConvertedTextures '
                       + remote + '/library.so ' + remote + '/fixtures.bin')
            result = subprocess.run(adb_cmd + ['shell', command], check=True, capture_output=True, text=True)
            lines = result.stdout.splitlines()
            if not lines or lines[-1] != f'total: {count}/{count} exact, 0 failed':
                print(result.stdout, end='')
                raise SystemExit('Native texture output did not match all reference mips.')
            print(result.stdout, end='')
        finally:
            if created:
                cleanup = subprocess.run(adb_cmd + ['shell', 'rm', '-rf', remote], capture_output=True)
                if cleanup.returncode: print('Device temporary directory could not be removed:', remote)


if __name__ == '__main__': main()
