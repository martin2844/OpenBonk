#!/usr/bin/env python3
"""Rebuild private legacy shared assets from Steam and pinned Android inputs.

This regenerates its temporary templates locally. It is not a complete APK
build and the resulting files contain game-derived material.
"""
import argparse
import hashlib
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parent.parent
PINS = {
    'old_apk': '850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5',
    'old_obb': 'baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d',
}
EXPECTED = {
    'sharedassets0.assets': '5e76282651d6e6f120d1362277aea74a68bfa482ea8737101092f027d5d374b8',
    'sharedassets1.assets': 'b10fd4de8a0da336f39dff3dddbab994499b229fa5b3f443ab8adbcc16b2082e',
    'sharedassets1.resource': '93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6',
    'sharedassets2.assets': '42a617421d3feaca2907addb8362f0613ca48d5675171a677f8b669576a502e2',
    'sharedassets2.resource': '6abc02d2702824b936add3ea2a6780b5b10e2954e1df7543b8cc79b5c9e1b1bc',
}
FLAGS = '''--experimental-font-atlases --experimental-font-metadata --experimental-default-cubemap
--experimental-remaining-textures --experimental-terrain-animations --experimental-shared1-textures
--experimental-shared1-four-textures --experimental-water-rgba --experimental-shared2-atlases
--experimental-shared2-audio --experimental-shared2-textures --experimental-shared2-meshes
--experimental-hashed-meshes --experimental-hashed-animations --experimental-hashed-textures
--experimental-hashed-matched --experimental-hashed-rgba --experimental-hashed-extra
--experimental-hashed-final --experimental-hashed-audio --experimental-export-rebuilt-only'''.split()


def run(script, *arguments, env=None):
    print('+', script, flush=True)
    subprocess.run([sys.executable, str(ROOT/script), *(str(x) for x in arguments)], check=True, env=env)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'old-apk', 'old-obb', 'crunch-decoder', 'output'):
        p.add_argument('--'+name, required=True, type=Path)
    a = p.parse_args()
    steam, apk, obb, decoder = (x.expanduser().resolve(strict=True) for x in (a.steam_data, a.old_apk, a.old_obb, a.crunch_decoder))
    out = a.output.expanduser().absolute()
    if not steam.is_dir() or not all(x.is_file() for x in (apk, obb, decoder)) or out.exists() or out.is_symlink() or not out.parent.is_dir():
        p.error('inputs must exist and output must be new')
    out = out.parent.resolve()/out.name
    if any(out.is_relative_to(x) or x.is_relative_to(out) for x in (ROOT, steam, apk, obb, decoder)):
        p.error('output must be separate from checkout and inputs')
    for path, expected in ((apk, PINS['old_apk']), (obb, PINS['old_obb'])):
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                p.error('pinned input differs: '+path.name)
    with tempfile.TemporaryDirectory(prefix='.openbonk-converted-assets-', dir=out.parent) as directory:
        tmp = Path(directory); shared0, shared12, hashed, templates, rebuilt = (tmp/n for n in ('shared0', 'shared12', 'hashed', 'templates', 'rebuilt'))
        run('tools/generate-legacy-shared0-templates.py', '--steam-data', steam, '--old-apk', apk, '--output', shared0)
        run('tools/generate-legacy-shared-plans.py', '--steam-data', steam, '--android-obb', obb, '--shared1-terrain-from-obb', '--shared1-source-from-obb', '--crunch-decoder', decoder, '--include-private-templates', '--output', shared12)
        run('tools/generate-legacy-hashed-manifests.py', '--steam-data', steam, '--expanded-obb', obb, '--include-private-templates', '--output', hashed)
        templates.mkdir(mode=0o700)
        for source in (shared0, shared12/'share-source-experiment', hashed):
            for folder in source.iterdir():
                if not folder.is_dir() or folder.is_symlink() or (templates/folder.name).exists():
                    raise ValueError('invalid or duplicate generated template directory')
                shutil.copytree(folder, templates/folder.name)
        env = os.environ.copy(); env['OPENBONK_MANIFEST_ROOT'] = str(templates)
        run('tools/build-local-diagnostic.py', steam, rebuilt, '--decoder', decoder, *FLAGS, env=env)
        for name, expected in EXPECTED.items():
            with (rebuilt/name).open('rb') as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                    raise ValueError('rebuilt asset differs: '+name)
        out.mkdir(mode=0o700)
        try:
            for name in EXPECTED: shutil.copyfile(rebuilt/name, out/name)
        except BaseException:
            shutil.rmtree(out); raise
    print('Rebuilt five pinned shared assets; keep them private.')


if __name__ == '__main__': main()
