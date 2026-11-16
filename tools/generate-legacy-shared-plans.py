#!/usr/bin/env python3
"""Generate five legacy shared-asset plans from local Steam and Android data.

The output is game-derived and must remain private.
"""
import argparse
import hashlib
import importlib.util
import json
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
    terrain_group = parser.add_mutually_exclusive_group(required=True)
    terrain_group.add_argument('--shared1-terrain-asset', type=Path)
    terrain_group.add_argument('--shared1-terrain-source', type=Path, help='pinned earlier staged directory')
    terrain_group.add_argument('--shared1-before-cubemaps', type=Path, help='pinned stage before cubemap template generation')
    terrain_group.add_argument('--shared1-before-normal', type=Path, help='pinned stage before normal and large texture templates')
    terrain_group.add_argument('--shared1-before-exact', type=Path, help='pinned stage before exact renamed texture templates')
    terrain_group.add_argument('--shared1-before-alpha8', type=Path, help='pinned stage before alpha texture templates')
    terrain_group.add_argument('--shared1-terrain-from-obb', action='store_true', help='rebuild the terrain source from the old OBB')
    parser.add_argument('--crunch-decoder', type=Path, help='local six-face Unity Crunch decoder, required before cubemaps')
    mesh_group = parser.add_mutually_exclusive_group(required=True)
    mesh_group.add_argument('--shared1-mesh-input', type=Path, help='pinned texture-template directory')
    mesh_group.add_argument('--shared1-source-dir', type=Path, help='pinned pre-template asset directory; regenerate the texture template')
    mesh_group.add_argument('--shared1-source-from-obb', action='store_true', help='rebuild the pre-template asset from the old OBB and Steam audio')
    parser.add_argument('--output', required=True, type=Path, help='new private directory')
    parser.add_argument('--include-private-templates', action='store_true', help='retain regenerated shared1/shared2 asset templates for local reconstruction')
    args = parser.parse_args()
    if args.shared1_terrain_from_obb and not args.shared1_source_from_obb:
        parser.error('--shared1-terrain-from-obb requires --shared1-source-from-obb')
    if bool(args.shared1_before_cubemaps or args.shared1_before_normal or args.shared1_before_exact or args.shared1_before_alpha8 or args.shared1_terrain_from_obb) != bool(args.crunch_decoder):
        parser.error('an earlier terrain source and --crunch-decoder must be supplied together')
    steam, obb = args.steam_data.resolve(strict=True), args.android_obb.resolve(strict=True)
    terrain_path = args.shared1_terrain_asset or args.shared1_terrain_source or args.shared1_before_cubemaps or args.shared1_before_normal or args.shared1_before_exact or args.shared1_before_alpha8
    terrain = terrain_path.resolve(strict=True) if terrain_path else None
    decoder = args.crunch_decoder.resolve(strict=True) if args.crunch_decoder else None
    mesh = (args.shared1_mesh_input or args.shared1_source_dir).resolve(strict=True) if not args.shared1_source_from_obb else None
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not obb.is_file() or (terrain is not None and not (terrain.is_dir() if not args.shared1_terrain_asset else terrain.is_file())) or (decoder is not None and not decoder.is_file()) or (mesh is not None and not mesh.is_dir()) or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (steam, obb, ROOT, *([terrain] if terrain else []), *([mesh] if mesh else []), *([decoder] if decoder else []))):
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
        if args.shared1_source_from_obb:
            original1, retargeted = tmp/'original-shared1.assets', tmp/'retargeted-audio'
            helper.extract(obb, original1, 'assets/bin/Data/sharedassets1.assets.split', True)
            helper.run('analysis/data-conversion/retarget-pc-audio-prototype.py', steam, original1, retargeted)
            mesh = retargeted
        if args.shared1_source_dir or args.shared1_source_from_obb:
            direct, rgba, etc, bc, combined = (tmp/name for name in ('direct', 'rgba', 'etc', 'bc', 'combined'))
            helper.run('analysis/data-conversion/direct-texture-prototype.py', 'template', steam, mesh, direct)
            helper.run('analysis/data-conversion/rgba-texture-prototype.py', 'template', steam, mesh, rgba)
            helper.run('analysis/data-conversion/etc-mip-texture-prototype.py', 'template', steam, mesh,
                       direct/'manifest.json', rgba/'manifest.json', etc)
            helper.run('analysis/data-conversion/bc-to-etc-texture-prototype.py', 'template', steam, mesh,
                       direct/'manifest.json', rgba/'manifest.json', etc/'manifest.json', bc)
            if args.shared1_terrain_from_obb:
                extra_rgba = tmp/'extra-rgba'
                helper.run('analysis/data-conversion/bc-to-rgba-texture-prototype.py', 'template', steam, mesh,
                           direct/'manifest.json', rgba/'manifest.json', etc/'manifest.json', extra_rgba)
            helper.run('analysis/data-conversion/combined-texture-bc-prototype.py', 'template', steam, mesh,
                       direct/'manifest.json', rgba/'manifest.json', etc/'manifest.json', bc/'manifest.json', combined)
            mesh = combined
        mesh_output = tmp/'full-cubemaps-stripped'
        helper.run('analysis/data-conversion/cross-scene-mesh-bc-prototype.py', 'template', steam, mesh, mesh_output)
        if args.shared1_terrain_from_obb:
            outline, terrain = tmp/'terrain-outline', tmp/'terrain-bc-rgba'
            helper.run('analysis/data-conversion/rebuild-outline-bc-prototype.py', 'template', steam, mesh_output, outline)
            helper.run('analysis/data-conversion/strip-bc-rgba-template.py', 'strip', steam, outline, extra_rgba/'manifest.json', terrain)
        if args.shared1_before_alpha8 or args.shared1_terrain_from_obb:
            alpha = tmp/'terrain-alpha8'
            helper.run('analysis/data-conversion/alpha8-staged-prototype.py', 'template', steam, terrain, alpha)
            terrain = alpha
        if args.shared1_before_exact or args.shared1_before_alpha8 or args.shared1_terrain_from_obb:
            exact = tmp/'terrain-exact'
            helper.run('analysis/data-conversion/exact-renamed-textures-prototype.py', 'strip', steam, terrain, exact)
            terrain = exact
        if args.shared1_before_normal or args.shared1_before_exact or args.shared1_before_alpha8 or args.shared1_terrain_from_obb:
            normal, large = tmp/'terrain-normal', tmp/'terrain-large'
            helper.run('analysis/data-conversion/normal-map-staged-prototype.py', 'template', steam, terrain, normal)
            helper.run('analysis/data-conversion/large-textures-staged-prototype.py', 'template', steam, normal, large)
            terrain = large
        if args.shared1_before_cubemaps or args.shared1_before_normal or args.shared1_before_exact or args.shared1_before_alpha8 or args.shared1_terrain_from_obb:
            cubemaps = tmp/'terrain-cubemaps'
            helper.run('analysis/data-conversion/all-cubemaps-staged-prototype.py', 'template', steam, terrain, decoder, cubemaps)
            terrain = cubemaps
        if args.shared1_terrain_source or args.shared1_before_cubemaps or args.shared1_before_normal or args.shared1_before_exact or args.shared1_before_alpha8 or args.shared1_terrain_from_obb:
            matched = tmp/'terrain-source'
            helper.run('analysis/data-conversion/matched-textures-staged-prototype.py', 'template', steam, terrain, matched)
            terrain = matched/'sharedassets1.assets'
        terrain_output = tmp/'shared1-terrain-animation-template'
        helper.run('analysis/data-conversion/shared1-terrain-animation-prototype.py', 'template', steam, terrain, terrain_output)
        shared1 = (
            ('shared1-terrain-animation-template', 'objects-manifest.json', '9641811a775b44f61ab6b2148ceb384ea6fa2155acd99a5d8fc44449402a1259'),
            ('full-cubemaps-stripped', 'mesh-manifest.json', 'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3'),
        )
        for folder, manifest, expected in shared1:
            if hashlib.sha256((tmp/folder/manifest).read_bytes()).hexdigest() != expected:
                raise ValueError('generated plan differs: '+folder)
        if args.include_private_templates:
            remaining, four = tmp/'shared1-last-textures-template', tmp/'shared1-four-textures-template'
            helper.run('analysis/data-conversion/shared1-remaining-textures-prototype.py', 'template', steam, terrain_output/'sharedassets1.assets', remaining)
            helper.run('analysis/data-conversion/shared1-four-textures-prototype.py', 'template', steam, remaining/'sharedassets1.assets', four)
            for folder, expected in ((remaining, '721bfcb5ee20eb0e713f6ae2c61e21a72ed016d1cb60d8279151b9f2f41e2c63'),
                                     (four, 'a8c72940f67acabb614f4e9a5c1cccab198f6b7a312960c738890795ecbb3be1')):
                if hashlib.sha256((folder/'textures-manifest.json').read_bytes()).hexdigest() != expected:
                    raise ValueError('generated texture plan differs: '+folder.name)
        core_plans = ()
        if args.shared1_terrain_from_obb:
            # The staged alpha input is a different layout from the original
            # standalone plan. The texture records are equivalent, while the
            # legacy consumer expects the standalone layout identifiers.
            def canonical_plan(source, name, template_hash):
                doc = json.loads(source.read_text())
                doc['target_sha256'] = '5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
                doc['template_sha256'] = template_hash
                target = tmp/name; target.write_text(json.dumps(doc, indent=2)+'\n')
                return target
            alpha_manifest = canonical_plan(alpha/'manifest.json', 'canonical-alpha.json', 'aa1f70f95f0cd69255881f5897dd6c1b048ae1a76f9d5a181d2dcd71f894c5e6')
            normal_manifest = canonical_plan(normal/'normal.json', 'canonical-normal.json', 'a0d481ab764dafced2d2326ae7fcdc0cc7122bc53c6194bab75a775dca26fa32')
            large_manifest = canonical_plan(large/'large.json', 'canonical-large.json', '01c4d95891f915e9fd10de5237e9f6a428dfcfb8a45358aa5e3581fe918a6fff')
            cubemap_manifest = canonical_plan(cubemaps/'cubemaps.json', 'canonical-cubemap.json', '527ee17e843fe5c8829a2a6333d4c45609b09e4c7050474334bc188a7c11d2f6')
            matched_manifest = canonical_plan(matched/'matched.json', 'canonical-matched.json', '04f615e93cf803aefd30e2bc3c06239cacdd4a83ed967871ce940b703c244f97')
            core_plans = (
                (direct/'manifest.json', 'direct-texture-template/manifest.json', '3bcf4a4ff496fcdac1520384bd30c49d671e9944b4ec5e7e23cc9e23ae5fe100'),
                (rgba/'manifest.json', 'rgba-template/manifest.json', 'abbad059169083017f36b6c560faed2ad88c92a17d6b4bc8f76d9816d5327750'),
                (etc/'manifest.json', 'etc-mip-template/manifest.json', '64b2c54efaebfc6fafa4b3cdf19e880d0a0067ec32e25b034c22c38ad274c0e0'),
                (bc/'manifest.json', 'bc-etc-template/manifest.json', '779a342d550dd4d703b5700cd382d55bfd5e1e0a7db214e580f7ec0b9cab1c3f'),
                (extra_rgba/'manifest.json', 'bc-rgba-template/manifest.json', 'c0b287bee5928fd5fbc4cc36c430f6c9d2cab43b99e0a9e2dc71906f7638280c'),
                (alpha_manifest, 'alpha8-template/manifest.json', 'c303ad6169e2fe7a9a459f19a485f4dc3992ab7b4911f8507c8309ec68020852'),
                (exact/'exact-renamed.json', 'full-exact-stripped/exact-renamed.json', '37f2efb47b603148210cd66887b0a57f2c5ad12fd3049ea71d19642531b86984'),
                (normal_manifest, 'normal-template/manifest.json', '9523e3d7e2a9aefc74d8c805acc27b861796b701b74926a227f0ae4279231720'),
                (large_manifest, 'large-template/manifest.json', '084a8c562538e518bb9bb660d8a8632cc9a262d9a1bd0153b65363a80520ebd8'),
                (cubemap_manifest, 'all-cubemaps-template/cubemaps.json', 'c88c880339f06b269fad6b5606f1118b000321f2141040e685cadcf0f9d668d3'),
                (matched_manifest, 'matched-template/matched.json', 'bcf2bfa4012f9344fcd8e0a22753f6e0e5dcc014dbe2dcad2abe881233ecc075'),
            )
            for source, _, expected in core_plans:
                if hashlib.sha256(source.read_bytes()).hexdigest() != expected:
                    raise ValueError('generated core plan differs: '+str(source))
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
            for source, relative, _ in core_plans:
                target = plan_root/relative; target.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
                shutil.copyfile(source, target)
            if args.include_private_templates:
                for folder, _, _, _ in STAGES:
                    shutil.copyfile(tmp/folder/'sharedassets2.assets', plan_root/folder/'sharedassets2.assets')
                for folder, _, _ in shared1:
                    if folder == 'shared1-terrain-animation-template':
                        shutil.copyfile(tmp/folder/'sharedassets1.assets', plan_root/folder/'sharedassets1.assets')
                for folder in (remaining, four): shutil.copytree(folder, plan_root/folder.name)
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated five pinned shared-asset plans; keep them private.')


if __name__ == '__main__':
    main()
