#!/usr/bin/env python3
"""Check candidate signing and Android upgrade behavior on a disposable emulator.

Requires a clean, running emulator. Never use an attached personal device.
Both test keys and all generated APKs are temporary; this does not authorize
or produce an APK for distribution.
"""
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
JDK = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK'
PACKAGE = 'com.megabonk.nativeport.splittest'


def run(args, **kwargs):
    return subprocess.run([str(x) for x in args], check=True, text=True, capture_output=True, **kwargs)


def main():
    if len(sys.argv) != 3 or not sys.argv[1].startswith('emulator-'):
        raise ValueError('usage: SCRIPT emulator-SERIAL UNSIGNED_APK')
    serial, source = sys.argv[1], Path(sys.argv[2]).resolve()
    if not source.is_file(): raise ValueError('missing source APK')
    adb = ['adb', '-s', serial]
    if run(adb + ['shell', 'getprop', 'ro.kernel.qemu']).stdout.strip() != '1':
        raise ValueError('target is not an emulator')
    if run(adb + ['shell', 'pm', 'list', 'packages', PACKAGE]).stdout.strip():
        raise ValueError('package already exists; preserve it and use a clean emulator')
    env = os.environ.copy()
    env['JAVA_HOME'] = str(JDK)
    env['PATH'] = str(JDK / 'bin') + ':' + env.get('PATH', '')
    env['OPENBONK_BETA_KEYSTORE_PASSWORD'] = secrets.token_urlsafe(24)
    env['OPENBONK_BETA_KEY_PASSWORD'] = env['OPENBONK_BETA_KEYSTORE_PASSWORD']
    installed = False
    try:
        with tempfile.TemporaryDirectory(prefix='openbonk-sign-test-', dir=ROOT / 'build/android') as directory:
            directory = Path(directory)
            for alias in ('first', 'other'):
                run(['steam-run', JDK / 'bin/keytool', '-genkeypair', '-keystore', directory / (alias + '.p12'),
                     '-storetype', 'PKCS12', '-storepass:env', 'OPENBONK_BETA_KEYSTORE_PASSWORD',
                     '-keypass:env', 'OPENBONK_BETA_KEY_PASSWORD', '-alias', alias, '-keyalg', 'RSA',
                     '-keysize', '2048', '-validity', '2', '-dname', 'CN=OpenBonk Ephemeral Test'], env=env)
            def sign(name, alias):
                result = directory / (name + '.apk')
                run([ROOT / 'tools/sign-beta-candidate.sh', source, result, directory / (alias + '.p12'), alias], env=env)
                return result
            first, update, other = sign('first', 'first'), sign('update', 'first'), sign('other', 'other')
            run(adb + ['root']); run(adb + ['wait-for-device'])
            run(adb + ['install', first]); installed = True
            marker = '/data/user/0/' + PACKAGE + '/files/openbonk-signing-test-marker'
            run(adb + ['shell', 'mkdir', '-p', '/data/user/0/' + PACKAGE + '/files'])
            run(adb + ['shell', 'printf beta-signing-fixture > ' + marker])
            run(adb + ['install', '-r', update])
            if run(adb + ['shell', 'cat', marker]).stdout != 'beta-signing-fixture':
                raise AssertionError('same-key update did not preserve data')
            rejected = subprocess.run([*adb, 'install', '-r', str(other)], text=True, capture_output=True)
            if rejected.returncode == 0 or 'INSTALL_FAILED_UPDATE_INCOMPATIBLE' not in (rejected.stdout + rejected.stderr):
                raise AssertionError('different signing key was not rejected as expected')
            if run(adb + ['shell', 'cat', marker]).stdout != 'beta-signing-fixture':
                raise AssertionError('rejected update changed data')
            print('PASS: same key updates in place and preserves data; different key rejected without data loss')
    finally:
        if installed:
            run(adb + ['uninstall', PACKAGE])


if __name__ == '__main__': main()
