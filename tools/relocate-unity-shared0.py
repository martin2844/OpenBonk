#!/usr/bin/env python3
"""Create private runtime inputs with Unity sharedassets0 moved into the OBB.

The input APK is used as build metadata, not as an installable APK. The output
APK is unsigned. This operation does not remove game data or make a release.
"""
import argparse
import hashlib
import os
from pathlib import Path
import re
import shutil
import zipfile


ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/sharedassets0.assets.split'
DATA = Path('unityLibrary/src/main/assets/bin/Data')
PART = re.compile(re.escape(PREFIX) + r'(0|[1-9][0-9]*)\Z')


def digest(stream):
    h = hashlib.sha256()
    while block := stream.read(1024 * 1024): h.update(block)
    return h.digest()


def validate(apk, obb, gradle):
    for folder, dirs, files in os.walk(gradle, followlinks=False):
        for name in dirs + files:
            if (Path(folder) / name).is_symlink(): raise ValueError('Gradle input contains a symbolic link')
    with zipfile.ZipFile(apk) as a, zipfile.ZipFile(obb) as b:
        anames, bnames = a.namelist(), b.namelist()
        if len(anames) != len(set(anames)) or len(bnames) != len(set(bnames)): raise ValueError('duplicate ZIP entry')
        parts = sorted((n for n in anames if PART.fullmatch(n)), key=lambda n: int(PART.fullmatch(n)[1]))
        if (not parts or len(parts) > 256 or [int(PART.fullmatch(n)[1]) for n in parts] != list(range(len(parts))) or
                any(n.startswith('assets/bin/Data/sharedassets0.assets') and n not in parts for n in anames) or
                any(n.startswith('assets/bin/Data/sharedassets0.assets') for n in bnames)):
            raise ValueError('invalid or conflicting sharedassets0 fragments')
        if (a.getinfo('assets/unity_obb_guid').file_size != 36 or b.getinfo('unity_obb_guid').file_size != 36 or
                a.read('assets/unity_obb_guid') != b.read('unity_obb_guid')):
            raise ValueError('runtime and OBB expansion identifiers differ')
        local = gradle / DATA
        expected = {n.rsplit('/', 1)[-1] for n in parts}
        actual = {p.name for p in local.glob('sharedassets0.assets*')}
        if actual != expected: raise ValueError('Gradle fragments differ from runtime APK')
        for i, name in enumerate(parts):
            info = a.getinfo(name); file = local / name.rsplit('/', 1)[-1]
            if (not file.is_file() or file.is_symlink() or not 0 < info.file_size <= 1048576 or
                    (i < len(parts) - 1 and info.file_size != 1048576) or file.stat().st_size != info.file_size):
                raise ValueError('invalid sharedassets0 fragment size')
            with a.open(info) as left, file.open('rb') as right:
                if digest(left) != digest(right): raise ValueError('Gradle fragment differs from runtime APK')
        return parts


def signature(name):
    upper = name.upper()
    if not upper.startswith('META-INF/'): return False
    leaf = upper[len('META-INF/'):]
    return '/' not in leaf and (leaf == 'MANIFEST.MF' or leaf.endswith(('.SF', '.RSA', '.DSA', '.EC')) or leaf.startswith('SIG-'))


def copy_entry(source, target, info):
    if info.is_dir():
        target.writestr(info, b'')
    else:
        with source.open(info) as inp, target.open(info, 'w') as out: shutil.copyfileobj(inp, out, 1024 * 1024)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('runtime-apk', 'runtime-obb', 'gradle-project', 'output'): p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--check', action='store_true', help='validate inputs without creating output')
    a = p.parse_args()
    inputs = []
    for value in (a.runtime_apk, a.runtime_obb, a.gradle_project):
        if value.expanduser().is_symlink(): p.error('inputs must not be symbolic links')
        try: inputs.append(value.expanduser().resolve(strict=True))
        except OSError: p.error('input is missing')
    apk, obb, gradle = inputs
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (ROOT, *inputs)): p.error('output must be separate from inputs')
    if not apk.is_file() or not obb.is_file() or not gradle.is_dir(): p.error('invalid inputs')
    try: parts = validate(apk, obb, gradle)
    except (OSError, KeyError, ValueError, zipfile.BadZipFile) as error: p.error(str(error))
    if a.check:
        print(f'{len(parts)} matching fragments found; no files were written.')
        return
    output.mkdir(mode=0o700)
    try:
        shutil.copytree(gradle, output / 'gradle', ignore=shutil.ignore_patterns('build', '.gradle', 'symbols'))
        for name in parts: (output / 'gradle' / DATA / name.rsplit('/', 1)[-1]).unlink()
        with zipfile.ZipFile(apk) as a, zipfile.ZipFile(output / 'runtime.apk', 'x') as dest:
            for info in a.infolist():
                if info.filename not in parts and not signature(info.filename): copy_entry(a, dest, info)
        with zipfile.ZipFile(obb) as b, zipfile.ZipFile(apk) as a, zipfile.ZipFile(output / 'runtime.obb', 'x') as dest:
            for info in b.infolist(): copy_entry(b, dest, info)
            for name in parts: copy_entry(a, dest, a.getinfo(name))
        with zipfile.ZipFile(apk) as a, zipfile.ZipFile(output / 'runtime.obb') as b:
            for name in parts:
                with a.open(name) as left, b.open(name) as right:
                    if digest(left) != digest(right): raise ValueError('relocated fragment differs')
    except BaseException:
        shutil.rmtree(output)
        raise
    print(f'Relocated {len(parts)} fragments; outputs contain private game data and the APK is unsigned.')


if __name__ == '__main__': main()
