#!/usr/bin/env python3
"""Create a new local beta signing identity; never overwrites an existing key.

The key and its generated password are stored outside build artifacts in a
private directory chosen by the user. Creating a key does not authorize
sharing a build. Back up the complete directory separately and securely.
"""
import os
from pathlib import Path
import secrets
import shutil
import subprocess
import sys

JDK = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK'
ALIAS = 'openbonk-beta'


def main():
    if len(sys.argv) != 2:
        raise ValueError('usage: create-beta-signing-key.py NEW_PRIVATE_DIRECTORY')
    directory = Path(sys.argv[1]).expanduser().absolute()
    if not directory.parent.is_dir() or directory.parent.is_symlink():
        raise ValueError('private directory parent must already exist')
    directory.mkdir(mode=0o700)  # Raises if the path already exists, including symlinks.
    complete = False
    try:
        password = secrets.token_urlsafe(36)
        env = os.environ.copy(); env['OPENBONK_GENERATED_KEY_PASSWORD'] = password
        env['JAVA_HOME'] = str(JDK); env['PATH'] = str(JDK / 'bin') + ':' + env.get('PATH', '')
        key = directory / 'openbonk-beta.p12'
        run = subprocess.run(['steam-run', str(JDK / 'bin/keytool'), '-genkeypair', '-keystore', str(key),
            '-storetype', 'PKCS12', '-storepass:env', 'OPENBONK_GENERATED_KEY_PASSWORD',
            '-keypass:env', 'OPENBONK_GENERATED_KEY_PASSWORD', '-alias', ALIAS, '-keyalg', 'RSA',
            '-keysize', '3072', '-validity', '10000', '-dname', 'CN=OpenBonk Beta'],
            env=env, check=True, capture_output=True, text=True)
        os.chmod(key, 0o600)
        password_path = directory / 'password.txt'
        fd = os.open(password_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(fd, 'w') as f:
            f.write(password + '\n'); f.flush(); os.fsync(f.fileno())
        cert = subprocess.run(['steam-run', str(JDK / 'bin/keytool'), '-list', '-v', '-keystore', str(key),
            '-storepass:env', 'OPENBONK_GENERATED_KEY_PASSWORD', '-alias', ALIAS],
            env=env, check=True, capture_output=True, text=True)
        fingerprint = next((line.strip() for line in cert.stdout.splitlines() if 'SHA256:' in line), None)
        if fingerprint is None: raise ValueError('certificate fingerprint unavailable')
        (directory / 'certificate-sha256.txt').write_text(fingerprint + '\n', encoding='utf-8')
        os.chmod(directory / 'certificate-sha256.txt', 0o600)
        complete = True
        print('Created local signing identity:', directory)
        print('Key alias:', ALIAS)
        print(fingerprint)
        print('Keep a secure backup of the entire directory; losing it prevents same-key updates.')
    finally:
        if not complete: shutil.rmtree(directory)


if __name__ == '__main__': main()
