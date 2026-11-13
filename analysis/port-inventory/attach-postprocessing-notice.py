#!/usr/bin/env python3
"""Attach one pinned Unity Post-processing package notice to an unsigned APK.

Diagnostic only: this is not the full Companion License or a complete notice
set, and does not establish provenance or permission to distribute the APK.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

NOTICE_SHA = '97cfe4fa7afe601adcd526ccd07708a2a435024b818a2e7e7b17a37aabd62f33'
PACKAGE_SHA = 'd3f2274ea35d1bd43e3f63b628dfe273da9ec4bd3177ce52567b97d315eb2316'
ENTRY = 'assets/ThirdPartyNotices/Unity-PostProcessing-3.4.0-LICENSE.md'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def has_signing_block(path, central_directory_offset):
    if central_directory_offset < 16:
        return False
    with path.open('rb') as f:
        f.seek(central_directory_offset - 16)
        return f.read(16) == b'APK Sig Block 42'


def is_jar_signature(name):
    upper = name.upper()
    if not upper.startswith('META-INF/'):
        return False
    leaf = upper[len('META-INF/'):]
    return '/' not in leaf and (leaf == 'MANIFEST.MF' or leaf.endswith(('.SF', '.RSA', '.DSA', '.EC')) or leaf.startswith('SIG-'))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('input_apk', type=Path)
    p.add_argument('package_dir', type=Path)
    p.add_argument('output_apk', type=Path)
    a = p.parse_args()
    if a.output_apk.exists() or a.output_apk.is_symlink() or not a.output_apk.parent.is_dir() or a.output_apk.resolve() == a.input_apk.resolve():
        p.error('output must be a new file')
    notice, package = (a.package_dir/'LICENSE.md').read_bytes(), (a.package_dir/'package.json').read_bytes()
    if digest(notice) != NOTICE_SHA or digest(package) != PACKAGE_SHA:
        raise ValueError('incompatible package notice or version')
    info = json.loads(package)
    if info.get('name') != 'com.unity.postprocessing' or info.get('version') != '3.4.0':
        raise ValueError('unexpected package identity')
    created = False
    try:
        with zipfile.ZipFile(a.input_apk) as source:
            names = source.namelist()
            if len(names) != len(set(names)) or ENTRY in names:
                raise ValueError('duplicate APK entry')
            if any(is_jar_signature(n) for n in names) or has_signing_block(a.input_apk, source.start_dir):
                raise ValueError('input APK must be unsigned')
            assemblies = json.loads(source.read('assets/bin/Data/ScriptingAssemblies.json'))['names']
            if 'Unity.Postprocessing.Runtime.dll' not in assemblies:
                raise ValueError('post-processing assembly evidence missing')
            with zipfile.ZipFile(a.output_apk, 'x', allowZip64=True) as dest:
                created = True
                for entry in source.infolist():
                    dest.writestr(entry, source.read(entry))
                entry = zipfile.ZipInfo(ENTRY, (1980, 1, 1, 0, 0, 0))
                entry.compress_type = zipfile.ZIP_DEFLATED
                entry.external_attr = 0o100644 << 16
                dest.writestr(entry, notice)
        with zipfile.ZipFile(a.input_apk) as before, zipfile.ZipFile(a.output_apk) as after:
            if after.testzip() is not None or len(after.namelist()) != len(set(after.namelist())):
                raise ValueError('invalid output APK')
            if set(after.namelist()) != set(before.namelist()) | {ENTRY}:
                raise ValueError('unexpected output members')
            if any(digest(before.read(n)) != digest(after.read(n)) for n in before.namelist()) or after.read(ENTRY) != notice:
                raise ValueError('existing APK entry or notice changed')
        print('Added pinned Post-processing notice; existing members unchanged; notice set is incomplete')
    except BaseException:
        if created:
            a.output_apk.unlink(missing_ok=True)
        raise


if __name__ == '__main__':
    main()
