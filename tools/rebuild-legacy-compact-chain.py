#!/usr/bin/env python3
"""Recreate the legacy compact base from pinned local inputs.

This is a research stage, not a clean APK build. Its Android archives, Steam
data, intermediate files and output contain private game-derived material.
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
    'new_apk': 'b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38',
    'new_obb': 'ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961',
    'converted_obb': 'bad3e899a7d75e7be6828261e04604f6d82c128296b0da125f4b48d0be0cdeb9',
}


def run(script, *arguments):
    print('+', script, flush=True)
    subprocess.run([sys.executable, str(ROOT / script), *(str(a) for a in arguments)], check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'old-apk', 'old-obb', 'new-apk', 'new-obb', 'converted-obb', 'crunch-decoder', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    args = parser.parse_args()
    names = ('steam_data', *PINS, 'crunch_decoder')
    inputs = {name: getattr(args, name).expanduser().resolve(strict=True) for name in names}
    out = args.output.expanduser().absolute()
    if not inputs['steam_data'].is_dir() or not all(inputs[n].is_file() for n in (*PINS, 'crunch_decoder')):
        parser.error('Steam data must be a directory and other inputs must be files')
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        parser.error('output must be a new directory in an existing parent')
    out = out.parent.resolve() / out.name
    if any(out.is_relative_to(p) or p.is_relative_to(out) for p in (ROOT, *inputs.values())):
        parser.error('output must be separate from checkout and inputs')
    for name, expected in PINS.items():
        with inputs[name].open('rb') as f:
            actual = hashlib.file_digest(f, 'sha256').hexdigest()
        if actual != expected:
            parser.error('pinned input differs: ' + name.replace('_', '-'))
    with tempfile.TemporaryDirectory(prefix='.openbonk-compact-chain-', dir=out.parent) as d:
        tmp = Path(d)
        steam, old_apk, old_obb, new_apk, new_obb, converted, decoder = (inputs[n] for n in names)
        scene_plans, scene, shared_inputs = tmp/'scene-plans', tmp/'scene', tmp/'shared-inputs'
        shared, hashed_plans, hashed, compact = (tmp/n for n in ('shared', 'hashed-plans', 'hashed', 'compact'))
        run('tools/generate-legacy-scene-manifests.py', '--steam-data', steam, '--old-apk', old_apk, '--old-obb', old_obb, '--output', scene_plans)
        run('tools/rebuild-legacy-scene-base.py', '--steam-data', steam, '--apk', new_apk, '--obb', new_obb, '--manifest-root', scene_plans, '--output', scene)
        run('tools/generate-legacy-transplant-reports.py', '--old-apk', old_apk, '--old-obb', old_obb, '--new-apk', new_apk, '--new-obb', new_obb, '--converted-obb', converted, '--output', shared_inputs)
        generated_plans = tmp/'generated-plans'
        run('tools/generate-legacy-shared-plans.py', '--steam-data', steam, '--android-obb', old_obb, '--shared1-terrain-from-obb', '--shared1-source-from-obb', '--crunch-decoder', decoder, '--output', generated_plans)
        shutil.move(generated_plans/'share-source-experiment', shared_inputs/'share-source-experiment')
        run('analysis/port-inventory/scrub-no-rewired-shared-prototype.py', scene, converted, shared, '--include-identical-rebuilt', '--inputs-root', shared_inputs)
        run('tools/generate-legacy-hashed-manifests.py', '--steam-data', steam, '--expanded-obb', old_obb, '--output', hashed_plans)
        run('analysis/port-inventory/scrub-no-rewired-hashed-prototype.py', shared, hashed, '--expanded-shared-base', '--manifest-root', hashed_plans)
        run('tools/rebuild-legacy-compact-base.py', '--base', hashed, '--steam-data', steam, '--output', compact)
        for name, expected in (('base.apk', '8e7fb232d255905840cc34b8efd3a72b0dbc3693c8a34635ff85f5c95e3b0892'),
                               ('base.obb', 'ca99de9c1fb7a0d917a99cd2fc4fe0f421a1e44b4b7e2eef395551e8cbe3110d')):
            with (compact/name).open('rb') as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                    raise ValueError('compact result differs: ' + name)
        # Reserve the destination exclusively; do not replace user data on a race.
        os.mkdir(out, 0o700)
        try:
            for path in compact.iterdir():
                if not path.is_file() or path.is_symlink():
                    raise ValueError('unexpected compact output')
                shutil.copyfile(path, out/path.name)
        except BaseException:
            shutil.rmtree(out)
            raise
    print('Rebuilt the pinned compact base. Keep it private; this is not an APK build.')


if __name__ == '__main__':
    main()
