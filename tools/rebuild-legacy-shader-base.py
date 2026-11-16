#!/usr/bin/env python3
"""Rebuild two consecutive private legacy shader-scrubbing stages.

This consumes a pinned earlier base and locally compiled shader bundles. It
does not build the complete beta APK, and its output contains private data.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent


def extract(obb, output, prefix, split):
    with zipfile.ZipFile(obb) as archive:
        names = archive.namelist()
        if len(names) != len(set(names)):
            raise ValueError('duplicate OBB entries')
        if split:
            parts = [name for name in names if name.startswith(prefix)]
            if any(not name[len(prefix):].isdigit() for name in parts):
                raise ValueError('invalid split asset name')
            parts.sort(key=lambda name: int(name[len(prefix):]))
            if [int(name[len(prefix):]) for name in parts] != list(range(len(parts))) or not parts:
                raise ValueError('incomplete split asset')
        else:
            if prefix not in names:
                raise ValueError('missing built-in asset')
            parts = [prefix]
        with output.open('xb') as stream:
            for name in parts:
                with archive.open(name) as part:
                    while data := part.read(1024 * 1024):
                        stream.write(data)


def run(script, *args):
    subprocess.run([sys.executable, str(ROOT / script), *(str(arg) for arg in args)], check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True, type=Path, help='pinned instancing-reference-scrubbed base')
    parser.add_argument('--shader-bundles', required=True, type=Path, help='pinned local shader cache')
    parser.add_argument('--output', required=True, type=Path, help='new private base directory')
    args = parser.parse_args()
    base, shaders = args.base.resolve(strict=True), args.shader_bundles.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not base.is_dir() or not shaders.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (base, shaders, ROOT)):
        parser.error('output must be separate from the checkout and inputs')
    spec = importlib.util.spec_from_file_location('shader_pins', ROOT / 'tools/pinned-shader-bundles.py')
    pins = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(pins)
    for name in ('feature-probe', 'builtin-probe'):
        path = shaders / name
        if not path.is_file() or path.stat().st_size > 8 * 1024 * 1024:
            raise ValueError('missing or oversized shader bundle: ' + name)
        with path.open('rb') as stream:
            if hashlib.file_digest(stream, 'sha256').hexdigest() != pins.PINS[name]:
                raise ValueError('incompatible shader bundle: ' + name)
    with tempfile.TemporaryDirectory(prefix='.openbonk-legacy-shaders-', dir=output.parent) as tmp:
        tmp = Path(tmp)
        feature_input, feature_template, feature_base = tmp/'feature.assets', tmp/'feature-template', tmp/'feature-base'
        extract(base/'base.obb', feature_input, 'assets/bin/Data/sharedassets1.assets.split', True)
        run('analysis/data-conversion/shared1-feature-shaders-template.py', 'template', shaders, feature_input, feature_template)
        run('analysis/port-inventory/scrub-shared1-feature-shader-base.py', base, feature_template, feature_base)
        builtin_input, builtin_template = tmp/'builtin.assets', tmp/'builtin-template'
        extract(feature_base/'base.obb', builtin_input, 'assets/bin/Data/0000000000000000f000000000000000', False)
        run('analysis/data-conversion/builtin-shaders-template.py', 'template', shaders, builtin_input, builtin_template)
        run('analysis/port-inventory/scrub-builtin-shader-base.py', feature_base, builtin_template, output)
    print('Rebuilt both pinned legacy shader stages; keep the output private.')


if __name__ == '__main__':
    main()
