#!/usr/bin/env python3
"""Rebuild nine consecutive private legacy shader-scrubbing stages.

This consumes a pinned earlier base and locally compiled shader bundles. With
Steam data, it can also build the preceding additional-materials stage. It does
not build the complete beta APK, and its output contains private data.
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
    parser.add_argument('--base', required=True, type=Path, help='pinned starting base for the selected stages')
    parser.add_argument('--shader-bundles', required=True, type=Path, help='pinned local shader cache')
    parser.add_argument('--steam-data', type=Path, help='Linux Megabonk_Data, to generate the preceding material templates')
    parser.add_argument('--include-shared1-material', action='store_true', help='also reconstruct the earlier shared1 material stage')
    parser.add_argument('--scene-source-obb', type=Path, help='pinned original Android OBB, to reconstruct the earlier scene GameObject stage')
    parser.add_argument('--include-scene-transforms', action='store_true', help='also reconstruct the preceding scene transform stage')
    parser.add_argument('--include-splash', action='store_true', help='also reconstruct the preceding Unity splash stage')
    preload_group = parser.add_mutually_exclusive_group()
    preload_group.add_argument('--preload-asset', type=Path, help='pinned sharedassets4.assets with preload references, to include the preceding preload stage')
    preload_group.add_argument('--preload-from-obb', action='store_true', help='derive equivalent preload references from the original OBB')
    parser.add_argument('--output', required=True, type=Path, help='new private base directory')
    args = parser.parse_args()
    if args.include_shared1_material and args.steam_data is None:
        parser.error('--include-shared1-material requires --steam-data')
    if args.scene_source_obb and (not args.include_shared1_material or not args.steam_data):
        parser.error('--scene-source-obb requires --steam-data and --include-shared1-material')
    if args.include_scene_transforms and not args.scene_source_obb:
        parser.error('--include-scene-transforms requires --scene-source-obb')
    if args.include_splash and not args.include_scene_transforms:
        parser.error('--include-splash requires --include-scene-transforms')
    if (args.preload_asset or args.preload_from_obb) and not args.include_splash:
        parser.error('--preload-asset or --preload-from-obb requires --include-splash')
    base, shaders = args.base.resolve(strict=True), args.shader_bundles.resolve(strict=True)
    steam = args.steam_data.resolve(strict=True) if args.steam_data else None
    scene_obb = args.scene_source_obb.resolve(strict=True) if args.scene_source_obb else None
    preload = args.preload_asset.resolve(strict=True) if args.preload_asset else None
    output = args.output.expanduser().absolute()
    if not base.is_dir() or not shaders.is_dir() or (steam is not None and not steam.is_dir()) or (scene_obb is not None and not scene_obb.is_file()) or (preload is not None and not preload.is_file()) or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (base, shaders, ROOT, *([steam] if steam else []), *([scene_obb] if scene_obb else []), *([preload] if preload else []))):
        parser.error('output must be separate from the checkout and inputs')
    spec = importlib.util.spec_from_file_location('shader_pins', ROOT / 'tools/pinned-shader-bundles.py')
    pins = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(pins)
    pins.verify(shaders)
    with tempfile.TemporaryDirectory(prefix='.openbonk-legacy-shaders-', dir=output.parent) as tmp:
        tmp = Path(tmp)
        current = base
        if preload or args.preload_from_obb:
            template, next_base = tmp/'preload-template', tmp/'preload-base'
            if args.preload_from_obb:
                preload = tmp/'preload-from-obb.assets'
                extract(scene_obb, preload, 'assets/bin/Data/sharedassets4.assets.split', True)
            run('analysis/data-conversion/preload-particle-bodies-prototype.py', 'template', steam, preload, scene_obb, template,
                '--include-gameobjects', '--include-extra-components', *(['--obb-preload'] if args.preload_from_obb else []))
            run('analysis/port-inventory/scrub-preload-particle-base.py', current, template, next_base,
                '--include-gameobjects', '--include-extra-components')
            current = next_base
        if args.include_splash:
            template, next_base = tmp/'splash-template', tmp/'splash-base'
            run('analysis/data-conversion/current-splash-template.py', 'template', current/'base.apk', template)
            run('analysis/port-inventory/scrub-current-splash-base.py', current, template, next_base)
            current = next_base
        if args.include_scene_transforms:
            template, next_base = tmp/'scene-transform-template', tmp/'scene-transform-base'
            run('analysis/data-conversion/scene-transform-values-template.py', 'template', steam, scene_obb, template)
            run('analysis/port-inventory/scrub-scene-transform-base.py', current, template, scene_obb, next_base)
            current = next_base
        if scene_obb:
            template, next_base = tmp/'scene-gameobject-template', tmp/'scene-gameobject-base'
            run('analysis/data-conversion/scene-gameobject-values-template.py', 'template', steam, scene_obb, template)
            run('analysis/port-inventory/scrub-scene-gameobject-base.py', current, template, scene_obb, next_base)
            current = next_base
        if args.include_shared1_material:
            source, template, next_base = tmp/'shared1-material.assets', tmp/'shared1-material-template', tmp/'shared1-material-base'
            extract(current/'base.obb', source, 'assets/bin/Data/sharedassets1.assets.split', True)
            run('analysis/data-conversion/shared1-material-values-template.py', 'template', steam, source, template,
                '--asset', 'sharedassets1.assets')
            run('analysis/port-inventory/scrub-shared1-material-base.py', current, template, next_base)
            current = next_base
        if steam:
            for i in (0, 2, 4):
                archive = current / ('base.apk' if i == 0 else 'base.obb')
                source = tmp / f'material-{i}.assets'
                template = tmp / f'additional-material-template-{i}'
                extract(archive, source, f'assets/bin/Data/sharedassets{i}.assets.split', True)
                run('analysis/data-conversion/shared1-material-values-template.py', 'template', steam, source, template,
                    '--asset', f'sharedassets{i}.assets')
            next_base = tmp / 'additional-material-base'
            run('analysis/port-inventory/additional-material-base.py', 'template', current, next_base, '--templates-root', tmp)
            current = next_base
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
