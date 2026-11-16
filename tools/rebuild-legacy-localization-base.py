#!/usr/bin/env python3
"""Regenerate the private legacy localization base from local Steam data.

This is an intermediate research stage, not an APK for distribution.
"""
import argparse
import hashlib
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parent.parent
EXPECTED = {'base.apk': '2b5bf0d217a18bbb9ef295672654cb4f9fe62ea308ad0372bd1dc44640f25510',
            'base.obb': 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'}


def run(script, *args):
    print('+', script, flush=True)
    subprocess.run([sys.executable, str(ROOT/script), *(str(x) for x in args)], check=True)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('base', 'linux-data', 'windows-data', 'output'):
        p.add_argument('--'+name, type=Path, required=True)
    a = p.parse_args()
    base, linux, windows = (x.expanduser().resolve(strict=True) for x in (a.base, a.linux_data, a.windows_data))
    out = a.output.expanduser().absolute()
    if not all(x.is_dir() for x in (base, linux, windows)) or out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('inputs must be directories and output must be new')
    out = out.parent.resolve()/out.name
    if any(out.is_relative_to(x) or x.is_relative_to(out) for x in (ROOT, base, linux, windows)):
        p.error('output must be separate from checkout and inputs')
    with tempfile.TemporaryDirectory(prefix='.openbonk-localization-base-', dir=out.parent) as directory:
        tmp = Path(directory); inventory, text, first, shared, final = (tmp/n for n in ('inventory.json', 'text', 'first', 'shared', 'final'))
        run('tools/generate-windows-localization-inventory.py', '--linux-data', linux, '--windows-data', windows, '--output', inventory)
        run('analysis/data-conversion/localization-text-template.py', 'template', windows, base/'base.obb', text, '--source-manifest', inventory)
        run('analysis/port-inventory/scrub-localization-base.py', base, text, first)
        run('analysis/data-conversion/localization-shared-template.py', 'template', windows, first/'base.obb', shared, '--source-manifest', inventory)
        run('analysis/port-inventory/scrub-localization-shared-base.py', first, shared, final)
        for name, expected in EXPECTED.items():
            with (final/name).open('rb') as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                    raise ValueError('localization base differs: '+name)
        out.mkdir(mode=0o700)
        try:
            for path in final.iterdir():
                if path.is_symlink(): raise ValueError('unexpected output symlink')
                if path.is_file(): shutil.copyfile(path, out/path.name)
                elif path.is_dir():
                    if any(x.is_symlink() for x in path.rglob('*')): raise ValueError('unexpected nested output symlink')
                    shutil.copytree(path, out/path.name)
                else: raise ValueError('unexpected output type')
        except BaseException:
            shutil.rmtree(out); raise
    print('Rebuilt the pinned localization base; keep it private.')


if __name__ == '__main__': main()
