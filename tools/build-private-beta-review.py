#!/usr/bin/env python3
"""Assemble the pinned, private OpenBonk review bundle; never publish it."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import zipfile

ROOT = Path(__file__).resolve().parent.parent
APK = ROOT / 'build/android/touch-glyph-test/candidate.apk'
AUDIT = APK.parent / 'privacy-audit.json'
PATH_AUDIT = APK.parent / 'path-marker-audit-v3.json'
EMAIL_AUDIT = APK.parent / 'email-pattern-outer-v2.json'
APK_SHA = 'dd595d38f2761364907bd30b0152375009d0caacb244c484096367fc6631a5a1'
CERT_SHA = 'bb2167e814b376d57c1f6a685a9255953f07efe64a91725e56e59f6cebcd8774'
ANDROID = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
NOTE = ("PRIVATE REVIEW ONLY — NOT APPROVED FOR DISTRIBUTION\n\n"
        "The APK still contains game-derived material and third-party components whose "
        "distribution permissions have not been resolved. The Windows full-folder "
        "installation has not been tested. The included privacy scan was bounded; "
        "it is not proof that all personal data is absent. Do not forward this bundle.\n")


def sha(data):
    return hashlib.sha256(data).hexdigest()


def verify_audit(audit):
    if audit.get('apk_sha256') != APK_SHA or audit.get('matches') != [] or audit.get('incomplete') != []:
        raise ValueError('candidate privacy scan is missing, incomplete or has matches')
    kinds = set(audit.get('search_kinds', []))
    required = {'exact_progression.json', 'exact_stats.json'}
    required.update(base + suffix for base in ('local_home_path', 'local_username', 'steam_account_id')
                    for suffix in ('', '_utf_16_le', '_utf_16_be'))
    if not required <= kinds:
        raise ValueError('candidate privacy scan does not include the required searches')


def verify_path_audit(audit):
    if (audit.get('apk_sha256') != APK_SHA or audit.get('markers') != [] or
            audit.get('incomplete') != [] or not isinstance(audit.get('entries_scanned'), int) or
            audit['entries_scanned'] < 1):
        raise ValueError('candidate path marker scan is missing, incomplete or has matches')


def verify_email_audit(audit):
    rows = audit.get('entries_with_matches')
    if (audit.get('apk_sha256') != APK_SHA or not isinstance(audit.get('entries_scanned'), int) or
            audit['entries_scanned'] < 1 or not isinstance(rows, list) or
            any(not isinstance(row, dict) or row.get('unmatched') != 0 for row in rows)):
        raise ValueError('candidate email-pattern scan is missing, incomplete or has unmatched patterns')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path, help='new ZIP file in an existing directory')
    args = parser.parse_args()
    output = args.output.absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new file in an existing directory')
    apk = APK.read_bytes()
    if sha(apk) != APK_SHA:
        raise ValueError('candidate differs from the reviewed APK')
    audit = json.loads(AUDIT.read_text(encoding='utf-8'))
    verify_audit(audit)
    verify_path_audit(json.loads(PATH_AUDIT.read_text(encoding='utf-8')))
    verify_email_audit(json.loads(EMAIL_AUDIT.read_text(encoding='utf-8')))
    cmd = ['steam-run', str(ANDROID / 'SDK/build-tools/34.0.0/apksigner'),
           'verify', '--print-certs', str(APK)]
    env = os.environ.copy()
    env['JAVA_HOME'] = str(ANDROID / 'OpenJDK')
    env['PATH'] = str(ANDROID / 'OpenJDK/bin') + os.pathsep + env.get('PATH', '')
    result = subprocess.run(cmd, check=True, text=True, capture_output=True, timeout=60, env=env)
    signers = re.findall(r'^Signer #(\d+) certificate SHA-256 digest: ([0-9a-f]+)$', result.stdout, re.MULTILINE)
    if signers != [('1', CERT_SHA)]:
        raise ValueError('unexpected signing identity')
    files = {
        'PRIVATE-REVIEW-ONLY.txt': NOTE.encode('utf-8'),
        'OpenBonk.apk': apk,
        'BETA_TESTER_GUIDE.md': (ROOT / 'BETA_TESTER_GUIDE.md').read_bytes(),
        'tools/create-launcher-steam-zip.py': (ROOT / 'tools/create-launcher-steam-zip.py').read_bytes(),
        'tools/create-launcher-steam-zip.ps1': (ROOT / 'tools/create-launcher-steam-zip.ps1').read_bytes(),
        'tools/launcher-steam-inputs.json': (ROOT / 'tools/launcher-steam-inputs.json').read_bytes(),
    }
    files['SHA256SUMS.txt'] = ''.join(f'{sha(data)}  {name}\n' for name, data in sorted(files.items())).encode('ascii')
    created = False
    try:
        fd = os.open(output, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
        created = True
        with os.fdopen(fd, 'wb') as stream, zipfile.ZipFile(stream, 'w', allowZip64=True) as archive:
            for name, data in sorted(files.items()):
                archive.writestr(name, data, compress_type=zipfile.ZIP_STORED if name.endswith('.apk') else zipfile.ZIP_DEFLATED)
        with zipfile.ZipFile(output) as archive:
            if set(archive.namelist()) != set(files) or archive.testzip() is not None:
                raise ValueError('bundle inventory or CRC differs')
            if any(sha(archive.read(name)) != sha(data) for name, data in files.items()):
                raise ValueError('bundle content differs')
    except BaseException:
        if created:
            output.unlink(missing_ok=True)
        raise
    print('Private review bundle:', output)
    print('SHA-256:', hashlib.file_digest(output.open('rb'), 'sha256').hexdigest())
    print('Not approved for distribution.')


if __name__ == '__main__':
    main()
