#!/usr/bin/env python3
"""Build an experimental standalone ETC2 quality probe; not an APK component."""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]
ARCHIVE = ROOT / 'third_party/etc2comp-39422c1.tar.gz'
SOURCE = ROOT / 'analysis/data-conversion/android/experimental_etc2_quality.cpp'
ARCHIVE_SHA256 = 'ba661f81c69fa4eecb2654afcacb0639617a619d964d6baf2310997c75f4381b'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--target', choices=('host', 'android'), required=True)
    p.add_argument('--ndk', type=Path, help='Unity Android NDK; required for Android')
    p.add_argument('--compiler', default='g++', help='host C++ compiler')
    p.add_argument('--runner', help='optional compiler wrapper')
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args(); out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    out = out.parent.resolve() / out.name
    if out.is_relative_to(ROOT): p.error('output must be outside the source checkout')
    if ARCHIVE.is_symlink() or not ARCHIVE.is_file() or hashlib.sha256(ARCHIVE.read_bytes()).hexdigest() != ARCHIVE_SHA256:
        p.error('pinned ETC2 source archive missing or different')
    if SOURCE.is_symlink() or not SOURCE.is_file(): p.error('probe source missing')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner: p.error('runner not found')
    if a.target == 'android':
        if not a.ndk: p.error('Android NDK is required')
        compiler = a.ndk.expanduser().resolve(strict=True) / 'toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        if not compiler.is_file() or not os.access(compiler, os.X_OK): p.error('Android compiler missing')
    else:
        compiler = shutil.which(a.compiler)
        if not compiler: p.error('host compiler missing')
    with tempfile.TemporaryDirectory(prefix='.openbonk-etc2-probe-', dir=out.parent) as temp:
        temp = Path(temp)
        with tarfile.open(ARCHIVE, 'r:gz') as archive: archive.extractall(temp, filter='data')
        source = temp / 'etc2comp-39422c1/EtcLib'; paths = sorted((source / 'Etc').glob('*.cpp')) + sorted((source / 'EtcCodec').glob('*.cpp'))
        if not paths: raise ValueError('ETC2 source archive is incomplete')
        result = temp / 'probe'
        cmd = ([runner] if runner else []) + [str(compiler), '-std=c++11', '-O2', '-pthread']
        if a.target == 'android': cmd += ['-static-libstdc++']
        cmd += ['-I' + str(source / 'Etc'), '-I' + str(source / 'EtcCodec'), str(SOURCE), *map(str, paths), '-o', str(result)]
        subprocess.run(cmd, check=True)
        header = result.read_bytes()[:20]
        if len(header) != 20 or header[:6] != b'\x7fELF\x02\x01' or struct.unpack_from('<H', header, 18)[0] != (183 if a.target == 'android' else 62):
            raise ValueError('compiler output has unexpected architecture')
        os.link(result, out)
    print('Built experimental ETC2 probe:', out, flush=True)


if __name__ == '__main__': main()
