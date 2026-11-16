#!/usr/bin/env python3
"""Generate five legacy shared-asset plans from local Steam and Android data.

The output is game-derived and must remain private.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import shutil
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('legacy_builder', ROOT/'tools/rebuild-legacy-shader-base.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
STAGES = (
    ('shared2-atlases-template', 'shared2-font-atlases-prototype.py', 'atlases-manifest.json', 'cad14caa26c75e5dce4efc66f05be00517ac0281f25314f8709ed878dc7f3080'),
    ('shared2-matched72-template', 'shared2-matched-textures-prototype.py', 'textures-manifest.json', '9df05620261b30b27f3b64ed050ba3dfdd2fe087087b2c1993974e005048386e'),
    ('shared2-mesh-template', 'shared2-mesh-buffers-prototype.py', 'mesh-manifest.json', '831fee2261236f0ec9db20798e5b8f4c05bfb2baa786e26d9ed65b5acfb26b61'),
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--android-obb', required=True, type=Path)
    parser.add_argument('--shared1-terrain-asset', required=True, type=Path)
    parser.add_argument('--shared1-mesh-input', required=True, type=Path, help='pinned texture-template directory')
    parser.add_argument('--output', required=True, type=Path, help='new private directory')
    args = parser.parse_args()
    steam, obb = args.steam_data.resolve(strict=True), args.android_obb.resolve(strict=True)
    terrain, mesh = args.shared1_terrain_asset.resolve(strict=True), args.shared1_mesh_input.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not obb.is_file() or not terrain.is_file() or not mesh.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (steam, obb, terrain, mesh, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    with tempfile.TemporaryDirectory(prefix='.openbonk-shared2-plans-', dir=output.parent) as directory:
        tmp = Path(directory)
        current = tmp/'original.assets'
        helper.extract(obb, current, 'assets/bin/Data/sharedassets2.assets.split', True)
        for folder, script, manifest, expected in STAGES:
            target = tmp/folder
            helper.run('analysis/data-conversion/'+script, 'template', steam, current, target)
            if hashlib.sha256((target/manifest).read_bytes()).hexdigest() != expected:
                raise ValueError('generated plan differs: '+folder)
            current = target/'sharedassets2.assets'
        terrain_output, mesh_output = tmp/'shared1-terrain-animation-template', tmp/'full-cubemaps-stripped'
        helper.run('analysis/data-conversion/shared1-terrain-animation-prototype.py', 'template', steam, terrain, terrain_output)
        helper.run('analysis/data-conversion/cross-scene-mesh-bc-prototype.py', 'template', steam, mesh, mesh_output)
        shared1 = (
            ('shared1-terrain-animation-template', 'objects-manifest.json', '9641811a775b44f61ab6b2148ceb384ea6fa2155acd99a5d8fc44449402a1259'),
            ('full-cubemaps-stripped', 'mesh-manifest.json', 'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3'),
        )
        for folder, manifest, expected in shared1:
            if hashlib.sha256((tmp/folder/manifest).read_bytes()).hexdigest() != expected:
                raise ValueError('generated plan differs: '+folder)
        output.mkdir(mode=0o700)
        try:
            plan_root = output/'share-source-experiment'
            plan_root.mkdir(mode=0o700)
            for folder, _, manifest, _ in STAGES:
                (plan_root/folder).mkdir(mode=0o700)
                shutil.copyfile(tmp/folder/manifest, plan_root/folder/manifest)
            for folder, manifest, _ in shared1:
                (plan_root/folder).mkdir(mode=0o700)
                shutil.copyfile(tmp/folder/manifest, plan_root/folder/manifest)
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated five pinned shared-asset plans; keep them private.')


if __name__ == '__main__':
    main()
