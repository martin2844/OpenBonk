#!/usr/bin/env python3
"""Regenerate private legacy font and texture templates from local inputs.

The result contains game-derived data. This is not an APK or release build.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import shutil
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('legacy_helper', ROOT/'tools/rebuild-legacy-shader-base.py')
helper = importlib.util.module_from_spec(spec); spec.loader.exec_module(helper)
OLD_APK = '850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5'
PINS = {
    'shared0-font-template/font-manifest.json': '4fd3e38f6c110ebcaff62e5e74da6df53678651f9c3e5a5530670a71c6502272',
    'shared0-atlas-template/atlas-manifest.json': '269da944e556073d7b670e6025653c7e02929e115c12f25776c72043346e431d',
    'shared0-metadata-template/metadata-manifest.json': '6f1caffaf13cfb2fd73cbe4d4264d01125cbb362c265570d2aae767d356dba00',
    'shared0-cubemap-template/sharedassets0.assets': 'a36b7152ad38ba4634eba085af73c01e5366a3168f7a0a0f4e86cd09e0250203',
    'shared0-last-textures-template/textures-manifest.json': 'e2b44b36a3d60d315f7e84861f5f0c84f71e4300894b3ee7d4f4353c09e40838',
}


def check(path, expected):
    with path.open('rb') as f:
        if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
            raise ValueError('pinned input or generated template differs: ' + str(path))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'old-apk', 'output'): p.add_argument('--'+name, type=Path, required=True)
    a = p.parse_args(); steam, apk = a.steam_data.resolve(strict=True), a.old_apk.resolve(strict=True)
    out = a.output.expanduser().absolute()
    if not steam.is_dir() or not apk.is_file() or out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('inputs must exist and output must be new')
    out = out.parent.resolve()/out.name
    if any(out.is_relative_to(x) or x.is_relative_to(out) for x in (ROOT, steam, apk)):
        p.error('output must be separate from checkout and inputs')
    check(apk, OLD_APK)
    with tempfile.TemporaryDirectory(prefix='.openbonk-shared0-templates-', dir=out.parent) as directory:
        tmp = Path(directory); initial = tmp/'sharedassets0.assets'
        helper.extract(apk, initial, 'assets/bin/Data/sharedassets0.assets.split', True)
        fonts, atlases, metadata, cubemap, textures = (tmp/n for n in ('shared0-font-template', 'shared0-atlas-template', 'shared0-metadata-template', 'shared0-cubemap-template', 'shared0-last-textures-template'))
        helper.run('analysis/data-conversion/shared0-fonts-prototype.py', 'template', steam, initial, fonts)
        helper.run('analysis/data-conversion/shared0-atlas-fonts-prototype.py', 'template', steam, fonts/'sharedassets0.assets', fonts/'font-manifest.json', atlases)
        helper.run('analysis/data-conversion/shared0-font-metadata-prototype.py', 'template', steam, atlases/'sharedassets0.assets', metadata)
        helper.run('analysis/data-conversion/shared0-default-cubemap-prototype.py', 'template', steam, metadata/'sharedassets0.assets', cubemap)
        helper.run('analysis/data-conversion/shared0-remaining-textures-prototype.py', 'template', steam, cubemap/'sharedassets0.assets', textures)
        for name, expected in PINS.items(): check(tmp/name, expected)
        out.mkdir(mode=0o700)
        try:
            for folder in (fonts, atlases, metadata, cubemap, textures):
                shutil.copytree(folder, out/folder.name)
        except BaseException:
            shutil.rmtree(out); raise
    print('Generated five pinned shared0 templates; keep them private.')


if __name__ == '__main__': main()
