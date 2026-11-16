#!/usr/bin/env python3
"""Regenerate the local compact base and its asset templates from Steam inputs.

This is one legacy build stage, not a complete APK build. Output is private.
"""
import argparse
import importlib.util
from pathlib import Path
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('legacy_shader_base', ROOT/'tools/rebuild-legacy-shader-base.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', required=True, type=Path, help='pinned expanded hashed base')
    parser.add_argument('--steam-data', required=True, type=Path, help='Linux Megabonk_Data')
    parser.add_argument('--output', required=True, type=Path, help='new private directory')
    args = parser.parse_args()
    base, steam = args.base.resolve(strict=True), args.steam_data.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not base.is_dir() or not steam.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('input directories must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (base, steam, ROOT)):
        parser.error('output must be separate from the checkout and inputs')
    with tempfile.TemporaryDirectory(prefix='.openbonk-legacy-compact-', dir=output.parent) as directory:
        tmp = Path(directory)
        def template(name, script, source):
            target = tmp / name
            helper.run('analysis/data-conversion/' + script, 'template', steam, source, target)
            return target
        def asset(index, archive):
            path = tmp / f'original-{index}.assets'
            helper.extract(base/archive, path, f'assets/bin/Data/sharedassets{index}.assets.split', True)
            return path
        original4 = asset(4, 'base.obb')
        mesh = template('mesh', 'shared4-mesh-buffers-prototype.py', original4)
        texture = template('texture', 'shared4-textures-prototype.py', mesh/'sharedassets4.assets')
        particle = template('particle', 'shared4-particle-bodies-prototype.py', texture/'sharedassets4.assets')
        reference = template('reference', 'shared4-particle-references-prototype.py', particle/'sharedassets4.assets')
        original2 = asset(2, 'base.obb')
        gamepad = template('gamepad', 'shared2-gamepad-texture-prototype.py', original2)
        animation = template('animation', 'shared2-animation-streams-prototype.py', gamepad/'sharedassets2.assets')
        fonts = template('fonts', 'no-rewired-shared0-font-metadata-prototype.py', asset(0, 'base.apk'))
        helper.run('analysis/port-inventory/compact-no-rewired-zero-audio.py', base, output,
                   '--blank-shared4-audio', '--shared4-mesh-template', mesh,
                   '--shared4-texture-template', texture, '--shared4-particle-template', particle,
                   '--shared4-particle-reference-template', reference, '--shared2-gamepad-template', gamepad,
                   '--shared2-animation-template', animation, '--shared0-font-metadata-template', fonts)
    print('Rebuilt the pinned compact base; keep the output private.')


if __name__ == '__main__':
    main()
