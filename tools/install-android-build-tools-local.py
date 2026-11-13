#!/usr/bin/env python3
"""Install pinned Google Android build-tools 34.0.0 into a local SDK folder.

The SDK is downloaded from Google if --archive is omitted. This only prepares
the tools; it does not install an APK or modify a Steam installation.
"""
import argparse
import hashlib
import os
from pathlib import Path, PurePosixPath
import shutil
import stat
import tempfile
import urllib.request
import zipfile

URL = 'https://dl.google.com/android/repository/build-tools_r34-linux.zip'
SIZE = 61224257
SHA256 = 'e858c4b60069d0431051b225d384413b1643e1289b00a4825aed347f25bd510f'
PREFIX = 'android-14/'


def verify(path):
    if not path.is_file() or path.stat().st_size != SIZE:
        raise ValueError('incorrect Android build-tools archive size')
    with path.open('rb') as f:
        if hashlib.file_digest(f, 'sha256').hexdigest() != SHA256:
            raise ValueError('incorrect Android build-tools archive hash')


def extract(archive, staging):
    with zipfile.ZipFile(archive) as z:
        entries = z.infolist(); seen = set(); total = 0
        if not entries or len(entries) > 1000: raise ValueError('unexpected archive entry count')
        for entry in entries:
            name = entry.filename
            if not name.startswith(PREFIX) or '\\' in name or ':' in name:
                raise ValueError('unsafe Android SDK archive path')
            relative = name[len(PREFIX):].rstrip('/')
            if not relative: continue
            parts = relative.split('/')
            if any(p in ('', '.', '..') for p in parts): raise ValueError('unsafe Android SDK archive path')
            key = relative.casefold()
            if key in seen: raise ValueError('duplicate Android SDK archive path')
            seen.add(key)
            mode = entry.external_attr >> 16
            if stat.S_ISLNK(mode) or entry.flag_bits & 1: raise ValueError('unsupported Android SDK archive entry')
            target = staging.joinpath(*PurePosixPath(relative).parts)
            if entry.is_dir():
                target.mkdir(parents=True, exist_ok=True)
                continue
            total += entry.file_size
            if entry.file_size > 64*1024*1024 or total > 256*1024*1024:
                raise ValueError('unexpected Android SDK archive size')
            target.parent.mkdir(parents=True, exist_ok=True)
            with z.open(entry) as src, target.open('xb') as dst:
                shutil.copyfileobj(src, dst, 1024*1024)
            target.chmod(0o755 if mode & 0o111 else 0o644)
    for required in ('zipalign', 'apksigner', 'lib/apksigner.jar'):
        if not (staging/required).is_file(): raise ValueError('missing Android build tool: '+required)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('sdk_root', type=Path, help='existing SDK root directory')
    p.add_argument('--archive', type=Path, help='already downloaded Google archive; otherwise download it')
    a = p.parse_args(); sdk = a.sdk_root.expanduser().resolve()
    if not sdk.is_dir(): p.error('SDK root must be an existing directory')
    parent = sdk/'build-tools'; target = parent/'34.0.0'
    if target.exists() or target.is_symlink(): p.error('build-tools/34.0.0 already exists')
    parent.mkdir(exist_ok=True)
    try:
        with tempfile.TemporaryDirectory(prefix='.openbonk-build-tools-', dir=parent) as tmp:
            tmp = Path(tmp); archive = a.archive.expanduser().resolve() if a.archive else tmp/'download.zip'
            if not a.archive:
                print('Descargando Android Build Tools 34.0.0 desde Google…', flush=True)
                with urllib.request.urlopen(URL, timeout=60) as src, archive.open('xb') as dst:
                    total = 0
                    while block := src.read(1024*1024):
                        total += len(block)
                        if total > SIZE: raise ValueError('Android build-tools download too large')
                        dst.write(block)
            verify(archive)
            stage = tmp/'files'; stage.mkdir(); extract(archive, stage)
            # Exclusive directory creation prevents replacing existing SDK tools.
            target.mkdir(mode=0o755)
            try:
                for item in stage.iterdir():
                    destination = target/item.name
                    if item.is_dir(): shutil.copytree(item, destination)
                    else: shutil.copy2(item, destination)
            except BaseException:
                shutil.rmtree(target); raise
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        p.error(str(exc))
    print('Android Build Tools 34.0.0 instalado en:', target)


if __name__ == '__main__': main()
