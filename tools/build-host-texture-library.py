#!/usr/bin/env python3
"""Build a local x86-64 JNI texture converter for research verification.

This host library is not an Android runtime dependency. It is built from the
same pinned sources as the experimental ARM64 converter. Exact output still
depends on the compiler and must be checked against each generated plan.
"""
import argparse
import hashlib
import importlib.util
import os
from pathlib import Path
import platform
import shutil
import struct
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('android_texture_build', ROOT / 'tools/build-android-texture-library.py')
android = importlib.util.module_from_spec(spec)
spec.loader.exec_module(android)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--java-home', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    p.add_argument('--compiler', default='g++')
    p.add_argument('--runner', help='optional compiler wrapper')
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    if platform.system() != 'Linux' or platform.machine() not in ('x86_64', 'amd64'): p.error('this research builder supports Linux x86-64')
    jdk = args.java_home.expanduser().resolve(strict=True)
    if not (jdk / 'include/jni.h').is_file() or not (jdk / 'include/linux/jni_md.h').is_file(): p.error('JDK JNI headers missing')
    compiler = shutil.which(args.compiler)
    runner = shutil.which(args.runner) if args.runner else None
    if not compiler or (args.runner and not runner): p.error('compiler or runner missing')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or ROOT.is_relative_to(output): p.error('output must be outside the source checkout')
    for relative, expected in android.PINS.items():
        path = ROOT / relative
        if path.is_symlink() or not path.is_file(): p.error('missing regular source: ' + relative)
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected: p.error('source hash differs: ' + relative)
    print('Pinned host converter inputs verified.', flush=True)
    if args.check: return
    with tempfile.TemporaryDirectory(prefix='.openbonk-host-texture-', dir=output.parent) as temp:
        work = Path(temp)
        with tarfile.open(ROOT / 'third_party/etcpak-0.9.15.tar.gz', 'r:gz') as archive:
            archive.extractall(work, filter='data')
        source = work / 'etcpak-0.9.15/src/etcpak'; result = work / 'libopenbonk_texture.so'
        command = ([runner] if runner else []) + [compiler, '-std=c++17', '-O2', '-shared', '-fPIC', '-DOPENBONK_JNI',
                   '-I' + str(source), '-I' + str(ROOT / 'third_party/texture2ddecoder-unitycrunch'),
                   '-I' + str(jdk / 'include'), '-I' + str(jdk / 'include/linux'),
                   str(ROOT / 'analysis/data-conversion/android/steam_raw_texture_probe.cpp'),
                   str(ROOT / 'analysis/data-conversion/android/steam_unity_crunch_jni.cpp'),
                   str(source / 'ProcessRGB.cpp'), str(source / 'Tables.cpp'), '-o', str(result)]
        subprocess.run(command, check=True)
        with result.open('rb') as f: header = f.read(20)
        if len(header) != 20 or header[:6] != b'\x7fELF\x02\x01' or struct.unpack_from('<H', header, 18)[0] != 62:
            raise ValueError('compiler output is not x86-64 ELF')
        os.link(result, output)
    print('Host converter built; use only for local research.', flush=True)


if __name__ == '__main__': main()
