#!/usr/bin/env python3
"""Rebuild nine consecutive private legacy shader-scrubbing stages.

This consumes a pinned earlier base and locally compiled shader bundles. It
does not build the complete beta APK, and its output contains private data.
"""
import argparse
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
    parser.add_argument('--base', required=True, type=Path, help='pinned additional-material-scrubbed base')
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
    pins.verify(shaders)
    with tempfile.TemporaryDirectory(prefix='.openbonk-legacy-shaders-', dir=output.parent) as tmp:
        tmp = Path(tmp)
        current = base
        for name, template_script, scrub_script in (
            ('compute', 'shared1-compute-shaders-template.py', 'scrub-shared1-compute-base.py'),
            ('render', 'shared1-render-shaders-template.py', 'scrub-shared1-render-base.py'),
            ('fog', 'shared1-fog-shaders-template.py', 'scrub-shared1-fog-base.py'),
            ('local', 'shared1-local-shaders-template.py', 'scrub-shared1-local-shader-base.py'),
            ('shader-references', 'shared1-reference-shaders-template.py', 'scrub-shared1-reference-shader-base.py'),
            ('instancing', 'shared1-instancing-shaders-template.py', 'scrub-shared1-instancing-shader-base.py'),
            ('references', 'shared1-instancing-reference-template.py', 'scrub-shared1-instancing-reference-base.py'),
            ('feature', 'shared1-feature-shaders-template.py', 'scrub-shared1-feature-shader-base.py'),
        ):
            source, template, next_base = tmp/f'{name}.assets', tmp/f'{name}-template', tmp/f'{name}-base'
            extract(current/'base.obb', source, 'assets/bin/Data/sharedassets1.assets.split', True)
            run('analysis/data-conversion/' + template_script, 'template', shaders, source, template)
            run('analysis/port-inventory/' + scrub_script, current, template, next_base)
            current = next_base
        builtin_input, builtin_template = tmp/'builtin.assets', tmp/'builtin-template'
        extract(current/'base.obb', builtin_input, 'assets/bin/Data/0000000000000000f000000000000000', False)
        run('analysis/data-conversion/builtin-shaders-template.py', 'template', shaders, builtin_input, builtin_template)
        run('analysis/port-inventory/scrub-builtin-shader-base.py', current, builtin_template, output)
    print('Rebuilt nine pinned legacy shader stages; keep the output private.')


if __name__ == '__main__':
    main()
