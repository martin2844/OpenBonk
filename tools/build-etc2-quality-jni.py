#!/usr/bin/env python3
"""Build an experimental high-quality ETC2 JNI library; not used by the APK."""
import argparse
import hashlib
import importlib.util
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('texture_pins', ROOT / 'tools/build-android-texture-library.py')
pins = importlib.util.module_from_spec(spec); spec.loader.exec_module(pins)
QUALITY = ROOT / 'third_party/etc2comp-39422c1.tar.gz'
QUALITY_HASH = 'ba661f81c69fa4eecb2654afcacb0639617a619d964d6baf2310997c75f4381b'


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--target', choices=('host', 'android'), required=True)
    p.add_argument('--java-home', type=Path); p.add_argument('--ndk', type=Path)
    p.add_argument('--compiler', default='g++'); p.add_argument('--runner'); p.add_argument('--output', required=True, type=Path)
    a = p.parse_args(); out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    out = out.parent.resolve() / out.name
    if out.is_relative_to(ROOT): p.error('output must be outside the source checkout')
    if QUALITY.is_symlink() or not QUALITY.is_file() or hashlib.sha256(QUALITY.read_bytes()).hexdigest() != QUALITY_HASH: p.error('ETC2 source archive differs')
    for relative, expected in pins.PINS.items():
        path = ROOT / relative
        if path.is_symlink() or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected: p.error('texture source differs: ' + relative)
    quality_source = ROOT / 'analysis/data-conversion/android/experimental_etc2_quality_jni.cpp'
    if quality_source.is_symlink() or not quality_source.is_file(): p.error('quality adapter source missing')
    runner = shutil.which(a.runner) if a.runner else None
    if a.runner and not runner: p.error('runner not found')
    if a.target == 'android':
        if not a.ndk: p.error('Android NDK is required')
        compiler = a.ndk.expanduser().resolve(strict=True) / 'toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
        if not compiler.is_file() or not os.access(compiler, os.X_OK): p.error('Android compiler missing')
        jni_includes = []
    else:
        compiler = shutil.which(a.compiler)
        if not compiler or not a.java_home: p.error('host compiler and Java home are required')
        jdk = a.java_home.expanduser().resolve(strict=True)
        if not (jdk / 'include/jni.h').is_file() or not (jdk / 'include/linux/jni_md.h').is_file(): p.error('JNI headers missing')
        jni_includes = ['-I' + str(jdk / 'include'), '-I' + str(jdk / 'include/linux')]
    with tempfile.TemporaryDirectory(prefix='.openbonk-etc2-jni-', dir=out.parent) as temp:
        temp = Path(temp)
        for archive_path in (QUALITY, ROOT / 'third_party/etcpak-0.9.15.tar.gz'):
            with tarfile.open(archive_path, 'r:gz') as archive: archive.extractall(temp, filter='data')
        etc = temp / 'etc2comp-39422c1/EtcLib'; pak = temp / 'etcpak-0.9.15/src/etcpak'
        sources = sorted((etc / 'Etc').glob('*.cpp')) + sorted((etc / 'EtcCodec').glob('*.cpp'))
        if not sources: raise ValueError('ETC2 sources missing')
        result = temp / 'libopenbonk_quality.so'
        cmd = ([runner] if runner else []) + [str(compiler), '-std=c++17', '-O2', '-pthread', '-shared', '-fPIC', '-DOPENBONK_JNI', '-Wl,--no-undefined']
        if a.target == 'android': cmd += ['-static-libstdc++', '-U__ARM_NEON']
        cmd += jni_includes + ['-I' + str(pak), '-I' + str(etc / 'Etc'), '-I' + str(etc / 'EtcCodec'), str(quality_source),
                               str(pak / 'ProcessRGB.cpp'), str(pak / 'Tables.cpp'), *map(str, sources), '-o', str(result)]
        subprocess.run(cmd, check=True)
        with result.open('rb') as stream: header = stream.read(20)
        if len(header) != 20 or header[:6] != b'\x7fELF\x02\x01' or struct.unpack_from('<H', header, 18)[0] != (183 if a.target == 'android' else 62):
            raise ValueError('compiler output has unexpected architecture')
        os.link(result, out)
    print('Built experimental ETC2 JNI library:', out, flush=True)


if __name__ == '__main__': main()
