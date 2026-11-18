#!/usr/bin/env python3
"""Advertise an already published, locally verified Android release.

Requires gh and Android's aapt. Review and commit updates/android.json after
running; this script never uploads an APK or changes a GitHub release.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parent.parent
RELEASE_CERTIFICATE = 'bb2167e814b376d57c1f6a685a9255953f07efe64a91725e56e59f6cebcd8774'


def manifest(tag, code, asset, local_size, local_digest):
    if not re.fullmatch(r'v[0-9]+\.[0-9]+\.[0-9]+(?:-[A-Za-z0-9.]+)?', tag):
        raise ValueError('invalid release tag')
    url = f'https://github.com/martin2844/OpenBonk/releases/download/{tag}/OpenBonk.apk'
    if (asset.get('name') != 'OpenBonk.apk' or asset.get('url') != url or
            asset.get('size') != local_size or asset.get('digest') != f'sha256:{local_digest}'):
        raise ValueError('published asset does not match the local APK')
    if code <= 0:
        raise ValueError('invalid version code')
    return {'schema': 1, 'tag': tag, 'version_code': code, 'url': url,
            'size': local_size, 'sha256': local_digest}


def verify_release_signer(apk, apksigner, runner):
    command = ([runner] if runner else []) + [str(apksigner), 'verify', '--print-certs', str(apk)]
    details = subprocess.check_output(command, text=True)
    signers = re.findall(r'^Signer #\d+ certificate SHA-256 digest: ([a-fA-F0-9]{64})\s*$', details, re.M)
    if [signer.lower() for signer in signers] != [RELEASE_CERTIFICATE]:
        raise ValueError('APK does not use the OpenBonk beta signing certificate')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('tag')
    parser.add_argument('apk', type=Path)
    parser.add_argument('--aapt', required=True, help='path to Android build-tools aapt')
    parser.add_argument('--runner', help='optional executable used to run aapt')
    args = parser.parse_args()
    apk = args.apk.resolve(strict=True)
    command = ([args.runner] if args.runner else []) + [args.aapt, 'dump', 'badging', str(apk)]
    badging = subprocess.check_output(command, text=True)
    package = re.search(r"^package: name='([^']+)' versionCode='(\d+)'", badging, re.M)
    if not package or package.group(1) != 'com.megabonk.nativeport.splittest':
        raise ValueError('unexpected Android application ID or unreadable APK')
    verify_release_signer(apk, Path(args.aapt).with_name('apksigner'), args.runner)
    release = json.loads(subprocess.check_output([
        'gh', 'release', 'view', args.tag, '--repo', 'martin2844/OpenBonk',
        '--json', 'tagName,isDraft,assets'], text=True))
    if release['isDraft'] or release['tagName'] != args.tag:
        raise ValueError('release is not published')
    assets = [item for item in release['assets'] if item.get('name') == 'OpenBonk.apk']
    if len(assets) != 1:
        raise ValueError('release must contain exactly one OpenBonk.apk')
    with apk.open('rb') as f:
        sha = hashlib.file_digest(f, 'sha256').hexdigest()
    result = manifest(args.tag, int(package.group(2)), assets[0], apk.stat().st_size, sha)
    destination = ROOT / 'updates/android.json'
    current = json.loads(destination.read_text())
    if result['version_code'] <= current['version_code'] and result != current:
        raise ValueError('refusing to publish a non-increasing version code')
    destination.write_text(json.dumps(result, indent=2) + '\n')
    print(f"Verified {args.tag}; review and commit {destination.relative_to(ROOT)}")


if __name__ == '__main__':
    main()
