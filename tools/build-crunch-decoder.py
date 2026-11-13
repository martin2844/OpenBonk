#!/usr/bin/env python3
"""Build the local six-face Unity Crunch adapter from pinned source files."""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
HEADERS = ROOT/'third_party/texture2ddecoder-unitycrunch'
SOURCE = ROOT/'analysis/data-conversion/unpack-unity-crunch-cubemap.cpp'
PINS = {
    SOURCE: 'e6df78f711a66b59a4601ea6dbae674265a1adb23344b7b958ce3b580ce44c8a',
    HEADERS/'crn_decomp.h': '0cfc0f722492cf2e18d36225a95bd0eed02cb5a5fe8125f3afbb732685184a28',
    HEADERS/'crn_defs.h': '9bdb576f07dc2673d632c89ebb9e69199851cced6d7269cf3134ae9cfb935f5c',
    HEADERS/'crnlib.h': 'cf05c16902e0c282369a5d4468c4c97aa0e8538c22aeb63713e2ae7b043395b8',
    HEADERS/'LICENSE.texture2ddecoder': 'f933e94e0f23cc8ee4da01a7c5f832221f509c190819c5e2985b6cda7dbb9066',
}


def check():
    compiler = os.environ.get('CXX', 'g++')
    executable = shutil.which(compiler)
    if not executable:
        raise ValueError('no se encontró el compilador C++: '+compiler)
    for path, expected in PINS.items():
        if not path.is_file(): raise ValueError('falta el código fuente: '+str(path))
        with path.open('rb') as stream:
            if hashlib.file_digest(stream, 'sha256').hexdigest() != expected:
                raise ValueError('código fuente incompatible: '+str(path))
    return executable


def build(output):
    output = Path(output).absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        raise ValueError('decoder output must be new and its parent must exist')
    compiler = check()
    with tempfile.TemporaryDirectory(prefix='.openbonk-crunch-', dir=output.parent) as tmp:
        compiled = Path(tmp)/'decoder'
        subprocess.run([compiler, '-std=c++11', '-O2', '-DNDEBUG', '-fno-strict-aliasing',
                        '-I', str(HEADERS), str(SOURCE), '-o', str(compiled)], check=True)
        if not compiled.is_file() or not os.access(compiled, os.X_OK):
            raise ValueError('C++ compiler did not produce an executable')
        os.link(compiled, output)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    try:
        build(args.output)
    except (ValueError, OSError, subprocess.CalledProcessError) as exc:
        parser.error(str(exc))
    print('Unity Crunch decoder created:', args.output)
