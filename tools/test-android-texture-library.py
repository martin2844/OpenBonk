#!/usr/bin/env python3
"""Run synthetic ARM64 texture-converter checks on an explicitly chosen device.

Only temporary synthetic data is sent to the device, in a uniquely named
/data/local/tmp directory. No app installation or personal profile is touched.
"""
import argparse
import os
from pathlib import Path
import re
import secrets
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parent.parent
FIXTURE = ROOT / 'tools/fixtures/texture-native-smoke'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--library', required=True, type=Path)
    p.add_argument('--device', required=True, help='explicit adb device serial')
    p.add_argument('--sdk', required=True, type=Path, help='Android SDK directory')
    p.add_argument('--java-home', required=True, type=Path, help='JDK directory')
    p.add_argument('--runner', help='optional executable used to run the JDK, e.g. steam-run')
    p.add_argument('--check', action='store_true')
    a = p.parse_args()
    library = a.library.expanduser().absolute()
    if library.is_symlink() or not library.is_file():
        p.error('library must be a regular local file')
    sdk = a.sdk.expanduser().resolve(strict=True)
    jdk = a.java_home.expanduser().resolve(strict=True)
    javac, java = jdk / 'bin/javac', jdk / 'bin/java'
    d8 = sdk / 'build-tools/34.0.0/lib/d8.jar'
    adb = shutil.which('adb')
    if not adb or not all(x.is_file() for x in (javac, java, d8)) or not os.access(javac, os.X_OK) or not os.access(java, os.X_OK):
        p.error('adb, JDK and Android build-tools 34.0.0 are required')
    if not re.fullmatch(r'[A-Za-z0-9._:-]+', a.device):
        p.error('invalid adb serial')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner:
        p.error('configured JDK runner not found')
    prefix = [runner] if runner else []
    adb_cmd = [adb, '-s', a.device]
    result = subprocess.run(adb_cmd + ['get-state'], capture_output=True, text=True)
    if result.returncode or result.stdout.strip() != 'device':
        p.error('selected device is not connected and authorized')
    expected = (FIXTURE / 'expected.txt').read_text().splitlines()
    if len(expected) != 20:
        p.error('synthetic reference fixture is incomplete')
    print('Android texture test prerequisites found.', flush=True)
    if a.check:
        print('Check only; no files were written.')
        return
    with tempfile.TemporaryDirectory(prefix='openbonk-texture-smoke-') as temp:
        temp = Path(temp)
        classes, dex = temp / 'classes', temp / 'dex'
        classes.mkdir(); dex.mkdir()
        source = FIXTURE / 'com/openbonk/dataprep/SteamConvertedTextures.java'
        subprocess.run(prefix + [str(javac), '--release', '8', '-d', str(classes), str(source)], check=True)
        cls = classes / 'com/openbonk/dataprep/SteamConvertedTextures.class'
        subprocess.run(prefix + [str(java), '-cp', str(d8), 'com.android.tools.r8.D8', '--min-api', '26',
                       '--output', str(dex), str(cls)], check=True)
        remote = '/data/local/tmp/openbonk-texture-smoke-' + secrets.token_hex(12)
        # The path is generated locally from hex; a failed mkdir never triggers removal.
        created = False
        try:
            subprocess.run(adb_cmd + ['shell', 'mkdir', remote], check=True, capture_output=True)
            created = True
            subprocess.run(adb_cmd + ['push', str(dex / 'classes.dex'), remote + '/classes.dex'], check=True, capture_output=True)
            subprocess.run(adb_cmd + ['push', str(library), remote + '/library.so'], check=True, capture_output=True)
            command = ('CLASSPATH=' + remote + '/classes.dex app_process /system/bin '
                       'com.openbonk.dataprep.SteamConvertedTextures ' + remote + '/library.so')
            run = subprocess.run(adb_cmd + ['shell', command], check=True, capture_output=True, text=True)
            actual = run.stdout.splitlines()
            if actual != expected:
                raise SystemExit('Texture smoke output differs from the reference; device cleanup will run.')
        finally:
            if created:
                cleanup = subprocess.run(adb_cmd + ['shell', 'rm', '-rf', remote], capture_output=True, text=True)
                if cleanup.returncode:
                    print('Could not remove temporary device directory:', remote)
                    print('Inspect it before retrying.')
    print('All 20 synthetic ARM64 texture conversion cases matched the reference.')


if __name__ == '__main__':
    main()
