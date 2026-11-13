#!/usr/bin/env python3
"""Build the ARM64 texture conversion adapter from pinned local sources.

The scalar ETC encoder matches the legacy local reference on the tested
fixtures; this alone does not validate every game texture or the final APK.
"""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tarfile
import tempfile

ROOT = Path(__file__).resolve().parent.parent
PINS = {
    'analysis/data-conversion/android/steam_raw_texture_probe.cpp': 'af687267683cfe1561240de2f1555b042bc881d9dc1908211a7ee49bd6ccfdae',
    'analysis/data-conversion/android/steam_unity_crunch_jni.cpp': 'e7bdac5cf446e2ab35b0f1c9700cecb9af7d4589aeec0d7a2159f9238b0f4496',
    'third_party/texture2ddecoder-unitycrunch/crn_decomp.h': '0cfc0f722492cf2e18d36225a95bd0eed02cb5a5fe8125f3afbb732685184a28',
    'third_party/texture2ddecoder-unitycrunch/crn_defs.h': '9bdb576f07dc2673d632c89ebb9e69199851cced6d7269cf3134ae9cfb935f5c',
    'third_party/texture2ddecoder-unitycrunch/crnlib.h': 'cf05c16902e0c282369a5d4468c4c97aa0e8538c22aeb63713e2ae7b043395b8',
    'third_party/etcpak-0.9.15.tar.gz': 'fe09e32d8aa75b8163832428c3d6df65a51b2b7e0237292c10c1224ca5728a43',
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ndk', required=True, type=Path, help='Unity Android NDK directory')
    parser.add_argument('--output', required=True, type=Path, help='new ARM64 shared-library file')
    parser.add_argument('--runner', help='optional executable used to launch the compiler, e.g. steam-run')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    ndk = args.ndk.expanduser().resolve(strict=True)
    compiler = ndk / 'toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang++'
    if not compiler.is_file() or not os.access(compiler, os.X_OK):
        parser.error('Android ARM64 NDK compiler not found')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT / 'third_party') or output.is_relative_to(ROOT / 'analysis'):
        parser.error('output must not be inside source inputs')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('configured runner not found')
    for relative, expected in PINS.items():
        path = ROOT / relative
        if path.is_symlink() or not path.is_file():
            parser.error('missing regular source file: ' + relative)
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                parser.error('source hash mismatch: ' + relative)
    print('Pinned texture adapter sources and Android NDK found.', flush=True)
    if args.check:
        print('Check only; no files were written.')
        return
    with tempfile.TemporaryDirectory(prefix='.openbonk-texture-', dir=output.parent) as tmp:
        tmp = Path(tmp)
        with tarfile.open(ROOT / 'third_party/etcpak-0.9.15.tar.gz', 'r:gz') as archive:
            archive.extractall(tmp, filter='data')
        source = tmp / 'etcpak-0.9.15/src/etcpak'
        includes = ROOT / 'third_party/texture2ddecoder-unitycrunch'
        result = tmp / 'libopenbonk_texture.so'
        cmd = ([runner] if runner else []) + [str(compiler), '-std=c++17', '-O2', '-shared', '-fPIC',
               '-static-libstdc++', '-DOPENBONK_JNI', '-U__ARM_NEON', '-Wl,--no-undefined',
               '-I' + str(source), '-I' + str(includes),
               str(ROOT / 'analysis/data-conversion/android/steam_raw_texture_probe.cpp'),
               str(ROOT / 'analysis/data-conversion/android/steam_unity_crunch_jni.cpp'),
               str(source / 'ProcessRGB.cpp'), str(source / 'Tables.cpp'), '-o', str(result)]
        subprocess.run(cmd, check=True)
        with result.open('rb') as f:
            header = f.read(20)
        if len(header) != 20 or header[:6] != b'\x7fELF\x02\x01' or struct.unpack_from('<H', header, 18)[0] != 183:
            raise SystemExit('Compiler output is not an ARM64 little-endian shared library')
        os.link(result, output)
    with output.open('rb') as f:
        digest = hashlib.file_digest(f, 'sha256').hexdigest()
    print('Built local ARM64 texture library; SHA-256:', digest)


if __name__ == '__main__':
    main()
