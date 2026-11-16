#!/usr/bin/env python3
"""Recreate the legacy compact base from pinned local inputs.

This is a research stage, not a clean APK build. Its Android archives, Steam
data, intermediate files and output contain private game-derived material.
"""
import argparse
import hashlib
import importlib.util
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
}


def run(script, *arguments):
    print('+', script, flush=True)
    subprocess.run([sys.executable, str(ROOT / script), *(str(a) for a in arguments)], check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'old-apk', 'old-obb', 'new-apk', 'new-obb', 'crunch-decoder', 'output'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--shader-bundles', type=Path, help='also rebuild the subsequent pinned scene and shader base using this local shader cache')
    parser.add_argument('--windows-data', type=Path, help='also reconstruct the localization base from a pinned Windows Megabonk_Data directory (requires --shader-bundles)')
    parser.add_argument('--linux-zip', type=Path, help='also generate the shared audio plan using this pinned Linux Steam ZIP')
    parser.add_argument('--windows-zip', type=Path, help='paired pinned Windows Steam ZIP for the shared audio plan')
    parser.add_argument('--include-layout-plan', action='store_true', help='also generate the shared1 layout plan (requires localization base)')
    parser.add_argument('--include-material-mesh-plans', action='store_true', help='also generate and verify the shared1 material/audio and mesh plans')
    parser.add_argument('--include-texture-plans', action='store_true', help='also generate and verify the shared1 texture plans')
    parser.add_argument('--java-home', type=Path, help='JDK directory for material and mesh plan checks')
    parser.add_argument('--java-runner', default='none', help='wrapper for Java tools, or none')
    parser.add_argument('--host-texture-library', type=Path, help='local host JNI texture conversion library')
    converted_group = parser.add_mutually_exclusive_group(required=True)
    converted_group.add_argument('--converted-obb', type=Path, help='pinned historical converted OBB')
    converted_group.add_argument('--converted-assets', type=Path, help='directory of three independently rebuilt shared assets')
    converted_group.add_argument('--rebuild-converted-assets', action='store_true', help='regenerate the shared assets and their templates from the old APK/OBB and Steam')
    args = parser.parse_args()
    if args.include_layout_plan and not args.windows_data: parser.error('--include-layout-plan requires --windows-data')
    if args.include_material_mesh_plans and not (args.include_layout_plan and args.linux_zip and args.windows_zip and args.java_home):
        parser.error('--include-material-mesh-plans requires --include-layout-plan, both Steam ZIPs and --java-home')
    if args.include_texture_plans and not (args.include_material_mesh_plans and args.host_texture_library):
        parser.error('--include-texture-plans requires --include-material-mesh-plans and --host-texture-library')
    if bool(args.linux_zip) != bool(args.windows_zip): parser.error('--linux-zip and --windows-zip must be provided together')
    if args.linux_zip and args.converted_obb: parser.error('audio plan generation requires --converted-assets or --rebuild-converted-assets')
    names = ('steam_data', *PINS, 'crunch_decoder')
    inputs = {name: getattr(args, name).expanduser().resolve(strict=True) for name in names}
    converted_path = args.converted_obb or args.converted_assets
    converted_input = converted_path.expanduser().resolve(strict=True) if converted_path else None
    shaders = args.shader_bundles.expanduser().resolve(strict=True) if args.shader_bundles else None
    windows = args.windows_data.expanduser().resolve(strict=True) if args.windows_data else None
    zips = tuple(x.expanduser().resolve(strict=True) for x in (args.linux_zip, args.windows_zip)) if args.linux_zip else ()
    java = args.java_home.expanduser().resolve(strict=True) if args.java_home else None
    texture_library = args.host_texture_library.expanduser().resolve(strict=True) if args.host_texture_library else None
    out = args.output.expanduser().absolute()
    if not inputs['steam_data'].is_dir() or not all(inputs[n].is_file() for n in (*PINS, 'crunch_decoder')):
        parser.error('Steam data must be a directory and other inputs must be files')
    if out.exists() or out.is_symlink() or not out.parent.is_dir():
        parser.error('output must be a new directory in an existing parent')
    out = out.parent.resolve() / out.name
    if shaders is not None and not shaders.is_dir(): parser.error('shader bundles must be a directory')
    if windows is not None and (not windows.is_dir() or shaders is None): parser.error('--windows-data requires a directory and --shader-bundles')
    if zips and not all(x.is_file() for x in zips): parser.error('Steam ZIPs must be files')
    if java is not None and (not (java/'bin/java').is_file() or not (java/'bin/javac').is_file()): parser.error('Java home must contain java and javac')
    if texture_library is not None and not texture_library.is_file(): parser.error('host texture library must be a file')
    if args.java_runner != 'none' and not shutil.which(args.java_runner): parser.error('Java runner not found')
    if any(out.is_relative_to(p) or p.is_relative_to(out) for p in (ROOT, *([converted_input] if converted_input else []), *([shaders] if shaders else []), *([windows] if windows else []), *([java] if java else []), *([texture_library] if texture_library else []), *zips, *inputs.values())):
        parser.error('output must be separate from checkout and inputs')
    if shaders is not None:
        spec = importlib.util.spec_from_file_location('shader_pins', ROOT/'tools/pinned-shader-bundles.py')
        mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod); mod.verify(shaders)
    if windows is not None:
        import json
        spec = importlib.util.spec_from_file_location('locale_inventory', ROOT/'tools/generate-windows-localization-inventory.py')
        mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
        inventory = json.loads((ROOT/'tools/launcher-steam-inputs.json').read_text())['platforms']
        document = mod.build_inventory(inventory, inputs['steam_data'], windows, mod.normalizer.PATCH_SHA)
        if hashlib.sha256((json.dumps(document, indent=2)+'\n').encode()).hexdigest() != mod.normalizer.MANIFEST_SHA:
            parser.error('localization inventory differs')
    for name, expected in PINS.items():
        with inputs[name].open('rb') as f:
            actual = hashlib.file_digest(f, 'sha256').hexdigest()
        if actual != expected:
            parser.error('pinned input differs: ' + name.replace('_', '-'))
    if args.converted_obb:
        if not converted_input.is_file(): parser.error('converted OBB must be a file')
        with converted_input.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != 'bad3e899a7d75e7be6828261e04604f6d82c128296b0da125f4b48d0be0cdeb9':
                parser.error('pinned input differs: converted-obb')
    elif args.converted_assets and not converted_input.is_dir():
        parser.error('converted assets must be a directory')
    with tempfile.TemporaryDirectory(prefix='.openbonk-compact-chain-', dir=out.parent) as d:
        tmp = Path(d)
        steam, old_apk, old_obb, new_apk, new_obb, decoder = (inputs[n] for n in names)
        converted = converted_input
        if args.rebuild_converted_assets:
            converted_input = tmp/'converted-rebuilt'
            run('tools/rebuild-legacy-converted-assets.py', '--steam-data', steam, '--old-apk', old_apk,
                '--old-obb', old_obb, '--crunch-decoder', decoder, '--output', converted_input)
        if args.converted_assets or args.rebuild_converted_assets:
            spec = importlib.util.spec_from_file_location('converted_assets', ROOT/'tools/legacy-converted-assets.py')
            mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
            converted = tmp/'converted-assets.zip'
            mod.make_archive(converted_input, converted)
        audio_plan = None
        if zips:
            audio_plan = tmp/'shared-audio-plan.bin'
            run('analysis/data-conversion/generate-android-shared-audio-plan.py', '--rebuilt-assets', converted_input,
                '--linux-zip', zips[0], '--windows-zip', zips[1], '--output', audio_plan)
            with audio_plan.open('rb') as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != '57a8feb4f98d9eb6692c208dd32d9f92556d52b41cb682936b03324945bd9883':
                    raise ValueError('shared audio plan differs')
        scene_plans, scene, shared_inputs = tmp/'scene-plans', tmp/'scene', tmp/'shared-inputs'
        shared, hashed_plans, hashed, compact = (tmp/n for n in ('shared', 'hashed-plans', 'hashed', 'compact'))
        run('tools/generate-legacy-scene-manifests.py', '--steam-data', steam, '--old-apk', old_apk, '--old-obb', old_obb, '--output', scene_plans)
        run('tools/rebuild-legacy-scene-base.py', '--steam-data', steam, '--apk', new_apk, '--obb', new_obb, '--manifest-root', scene_plans, '--output', scene)
        run('tools/generate-legacy-transplant-reports.py', '--old-apk', old_apk, '--old-obb', old_obb, '--new-apk', new_apk, '--new-obb', new_obb, '--converted-obb', converted, '--output', shared_inputs, *(['--include-private-shared1'] if args.include_layout_plan else []))
        generated_plans = tmp/'generated-plans'
        run('tools/generate-legacy-shared-plans.py', '--steam-data', steam, '--android-obb', old_obb, '--shared1-terrain-from-obb', '--shared1-source-from-obb', '--crunch-decoder', decoder, '--output', generated_plans, *(['--include-private-templates'] if args.include_texture_plans else []))
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
        final = compact
        if shaders is not None:
            final = tmp/'shader-base'
            run('tools/rebuild-legacy-shader-base.py', '--base', compact, '--shader-bundles', shaders,
                '--steam-data', steam, '--scene-source-obb', new_obb, '--include-shared1-material',
                '--include-scene-transforms', '--include-splash', '--preload-from-obb', '--output', final)
            for name, expected in (('base.apk', '66f75a49dbaf514fa254f2aacb88d1e5117abb1360789b4ecf32b577d493cf8d'),
                                   ('base.obb', 'ca0fc86fa3f1344191b04e4be6f0fb05a97fc4bbdb6e0f4dac105b94b3aa7f21')):
                with (final/name).open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                        raise ValueError('shader result differs: '+name)
        if windows is not None:
            locale = tmp/'localization-base'
            run('tools/rebuild-legacy-localization-base.py', '--base', final, '--linux-data', steam,
                '--windows-data', windows, '--output', locale)
            final = locale
        layout_plan = None
        if args.include_layout_plan:
            layout_plan = tmp/'shared1-layout-plan.bin'
            run('analysis/data-conversion/generate-android-shared1-layout-plan.py', '--base-obb', final/'base.obb',
                '--target-asset', shared_inputs/'reconstructed-sharedassets1.assets', '--output', layout_plan)
            with layout_plan.open('rb') as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != '0142daaaf1e07c3a9aa90dac01382f8fba6cce74983f669376b01fce1fff6ed0':
                    raise ValueError('shared1 layout plan differs')
        material_plan = mesh_plan = None
        mesh_extra_plans = []; texture_plans = []
        if args.include_material_mesh_plans:
            java_source = ROOT/'analysis/data-conversion/android'; classes = tmp/'java-classes'; classes.mkdir()
            prefix = [] if args.java_runner == 'none' else [args.java_runner]
            def java_run(*command): subprocess.run([*prefix, *(str(x) for x in command)], check=True)
            java_names = ['SteamExactTextures', 'SteamShared1Layout', 'SteamShared1LayoutCheck',
                          'SteamShared1MaterialAudio', 'SteamShared1MaterialAudioCheck', 'SteamShared1Meshes', 'SteamShared1MeshesCheck']
            if args.include_texture_plans: java_names += ['SteamConvertedTextures', 'SteamShared1ExactTextures', 'SteamShared1ExactTexturesCheck']
            java_run(java/'bin/javac', '-d', classes, *(java_source/(name+'.java') for name in java_names))
            def check_java(name, *arguments): java_run(java/'bin/java', '-cp', classes, 'com.openbonk.dataprep.'+name, *arguments)
            layout_asset = tmp/'layout.assets'
            check_java('SteamShared1LayoutCheck', final/'base.obb', layout_plan, layout_asset, 'pass')
            target = shared_inputs/'reconstructed-sharedassets1.assets'
            material_plan = tmp/'shared1-material-plan.bin'
            run('analysis/data-conversion/generate-android-shared1-material-audio-plan.py', '--base', layout_asset,
                '--target-asset', target, '--linux-zip', zips[0], '--windows-zip', zips[1], '--manifest', final/'material-manifest.json',
                '--audio-plan', audio_plan, '--output', material_plan)
            material_assets = []
            for i, steam_zip in enumerate(zips):
                result = tmp/f'material-{i}.assets'
                check_java('SteamShared1MaterialAudioCheck', layout_asset, steam_zip, material_plan, result, 'pass')
                material_assets.append(result)
            for file in material_assets:
                with file.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != 'fa2ef744b54d702abf75835b03a161da3db635c7facfd03cde3dece61f5d3288': raise ValueError('material output differs')
            mesh_plan = tmp/'shared1-mesh-plan.bin'
            run('analysis/data-conversion/generate-android-shared1-mesh-plan.py', '--base', material_assets[0], '--target-asset', target,
                '--linux-zip', zips[0], '--windows-zip', zips[1], '--manifest', shared_inputs/'share-source-experiment/full-cubemaps-stripped/mesh-manifest.json', '--output', mesh_plan)
            mesh_assets = []
            for i, steam_zip in enumerate(zips):
                result = tmp/f'mesh-{i}.assets'
                check_java('SteamShared1MeshesCheck', material_assets[i], steam_zip, mesh_plan, result, 'pass')
                with result.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != '26df1db6a89a775557ddc9d29ac6bddb10f7f47f7370bd6064dce08cec0b1119': raise ValueError('mesh output differs')
                mesh_assets.append(result)
            for path, expected in ((material_plan, 'fc96c214a73e702069425abe9bc75f5bdd825d9f7c73164c33c685527faafa3f'),
                                   (mesh_plan, '7ac1084c785c3a015821b3a155aa9e4f39bee48b3a10911e91c05b5df304d0c5')):
                with path.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != expected: raise ValueError('shared1 plan differs: '+path.name)
            for stage, plan_sha, asset_sha in (
                ('metadata', 'f61b4d6fdf91bcf99a3b786f5b89470f04944433be4a455c1895f4199a763068', '76991a41618218070f29fe7f3dc273d6bc78dd777d690d9636ce55de70a28464'),
                ('flags', 'ff751ce7421ef240f6a5a3909646942f735e7c642a0243e995e89e36c711ed8d', '83e78c94e692808d7d39ae57d5abe0bdd8aaea7be6b64baad7a3c27263ca7624'),
                ('stream', 'fa485e91f9bd394392c7e7ec0aeefa341b924c3a7cb6e74e4180c4b15d442400', 'e609d9c11f63ce8b42a582780dd23848bae7d06efc5936ac5d3db4d81a9b106f'),
            ):
                plan = tmp/f'shared1-mesh-{stage}-plan.bin'
                run('analysis/data-conversion/generate-android-shared1-mesh-'+stage+'-plan.py', '--base', mesh_assets[0],
                    '--target-asset', target, '--linux-zip', zips[0], '--windows-zip', zips[1],
                    '--manifest', shared_inputs/'share-source-experiment/full-cubemaps-stripped/mesh-manifest.json', '--output', plan)
                with plan.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != plan_sha: raise ValueError('mesh '+stage+' plan differs')
                next_assets = []
                for i, steam_zip in enumerate(zips):
                    result = tmp/f'mesh-{stage}-{i}.assets'
                    check_java('SteamShared1MeshesCheck', mesh_assets[i], steam_zip, plan, result, 'pass')
                    with result.open('rb') as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != asset_sha: raise ValueError('mesh '+stage+' output differs')
                    next_assets.append(result)
                mesh_assets = next_assets; mesh_extra_plans.append(plan)
            if args.include_texture_plans:
                stages = (
                    ('direct', 'generate-android-shared1-exact-textures-plan.py', (), 'direct-texture-template/manifest.json', '2b574ff7aafce67fabc6c30e443c306039003a77db5ed69f2614df3c1a1a344c', 'cde5b19279b3c5ddf3c72b973e96a93e99cb9ec4490b58178a6f47c1ea61564c'),
                    ('rgba', 'generate-android-shared1-exact-textures-plan.py', ('rgba',), 'rgba-template/manifest.json', '601c6d3a685d0f52c8854b0738b1b2c22897633e47e8c952f5b95775e6237375', '41bbce3f1884566a5ff844be5a0f448fa7032364c6e2dd3cd3286ee6ec201202'),
                    ('converted', 'generate-android-shared1-rgba-converted-plan.py', (), 'rgba-template/manifest.json', 'fe3716fdbfcf6e29167c05a7dd1e8cdf5eee44e468b3487be79dc576c89339de', '72792c774df00852df4d36bfdaba74c13597e35ea4b8814cc03ec421424408fd'),
                )
                for stage, script, mode, manifest, plan_sha, asset_sha in stages:
                    plan = tmp/f'shared1-texture-{stage}-plan.bin'
                    run('analysis/data-conversion/'+script, *mode, '--base', mesh_assets[0], '--target-asset', target,
                        '--linux-zip', zips[0], '--windows-zip', zips[1], '--manifest', shared_inputs/'share-source-experiment'/manifest, '--output', plan)
                    with plan.open('rb') as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != plan_sha: raise ValueError('texture '+stage+' plan differs')
                    next_assets = []
                    for i, steam_zip in enumerate(zips):
                        result = tmp/f'texture-{stage}-{i}.assets'
                        check_java('SteamShared1ExactTexturesCheck', texture_library, mesh_assets[i], steam_zip, plan, result, 'pass')
                        with result.open('rb') as f:
                            if hashlib.file_digest(f, 'sha256').hexdigest() != asset_sha: raise ValueError('texture '+stage+' output differs')
                        next_assets.append(result)
                    mesh_assets = next_assets; texture_plans.append(plan)
                extra_stages = (
                    ('etc', (), ('etc-mip-template/manifest.json', 'bc-etc-template/manifest.json'), '9073d61c766894381ce4f8851e510fde68cb536ef72fe9ad7cfbe69c924c2f1c', '48eb60ddb23455cb0fb7cadaeab8135940c055ccdc18d9ac719e39bed53a4bcf'),
                    ('bc-rgba', ('rgba',), ('bc-rgba-template/manifest.json',), '6f0f87f9da1878fa42f077966ced8f8818c907aee9c61a5586dac2c73dc22d78', 'cfb61e4dca3918522e44b62f03ff6924987e0a2a0eafb7fb7b1b262e28dcf2bd'),
                    ('alias', ('alias',), ('shared1-last-textures-template/textures-manifest.json',), 'bfbfdd0ef0edb0b3ecdc2e7f980efba27a22f39c290f6b937085876ddd1894eb', 'eeec5fd78e2f86a2f61062fc8aff2d689b98a54aad2f0048e80904e0a27f226e'),
                    ('matched', ('matched',), ('matched-template/matched.json',), '5c8d467e1ddafd6c78e5417e32be5061fab6e4ebeeddcbc22c7ae5cc412b93c2', '28370f83ecd06bdab5ce2209883e1e34f61c0a9c1f980ae14a57d5373926cb0b'),
                    ('alpha', ('alpha',), ('alpha8-template/manifest.json',), 'a239ccca1e72f1a5d571266a0bb449cf23b654829ea181cc52e21befbc54c297', 'b68de113a869c7c0ce009fcde2702d6a9fb4c5ce68007461bca6898f630fb091'),
                    ('large', ('large',), ('large-template/manifest.json',), 'd9c84839c9f1246c0034875f14e0a03067f7034a04d86d904a32731e9cc9448f', 'c7e867a69354c18c908bf7d0f4ab26a2740786cd3eabf2c9055a8be714b38b57'),
                    ('renamed', ('renamed',), ('full-exact-stripped/exact-renamed.json',), '0f828293b10ada34a9ce63162ccee078d1431af86453bcdf55249b6f983f3214', '571f134d24d874a6537b2648580ee1e6eb77bed5f2dfac43f09584a4caf97117'),
                    ('last', ('last',), ('shared1-four-textures-template/textures-manifest.json',), '133e184ea15a36f2802c7a8060f43ff955037f7fa7673a8ef59393dc26164f24', '407cc3a2b9644960eff549169c15d8515d5a59da615fca50e7479b717f1c7305'),
                )
                for stage, mode, manifests, plan_sha, asset_sha in extra_stages:
                    plan = tmp/f'shared1-texture-{stage}-plan.bin'
                    manifest_args = [item for name in manifests for item in ('--manifest', shared_inputs/'share-source-experiment'/name)]
                    run('analysis/data-conversion/generate-android-shared1-etc-plan.py', *mode, '--base', mesh_assets[0], '--target-asset', target,
                        '--linux-zip', zips[0], '--windows-zip', zips[1], *manifest_args, '--output', plan)
                    with plan.open('rb') as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != plan_sha: raise ValueError('texture '+stage+' plan differs')
                    next_assets = []
                    for i, steam_zip in enumerate(zips):
                        result = tmp/f'texture-{stage}-{i}.assets'
                        check_java('SteamShared1ExactTexturesCheck', texture_library, mesh_assets[i], steam_zip, plan, result, 'pass')
                        with result.open('rb') as f:
                            if hashlib.file_digest(f, 'sha256').hexdigest() != asset_sha: raise ValueError('texture '+stage+' output differs')
                        next_assets.append(result)
                    mesh_assets = next_assets; texture_plans.append(plan)
                normal_plan = tmp/'shared1-normal-plan.bin'
                run('analysis/data-conversion/generate-android-shared1-normal-plan.py',
                    '--base', mesh_assets[0], '--target-asset', target,
                    '--linux-zip', zips[0], '--windows-zip', zips[1], '--output', normal_plan)
                with normal_plan.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != 'f1771fae6e453c86ea277fb507fc66417114e9bb153addf450a3587ec51e6e52': raise ValueError('normal plan differs')
                next_assets = []
                for i, steam_zip in enumerate(zips):
                    result = tmp/f'normal-{i}.assets'
                    check_java('SteamShared1ExactTexturesCheck', texture_library, mesh_assets[i], steam_zip, normal_plan, result, 'pass')
                    with result.open('rb') as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != 'ae07560ac689959eb102e9dab569e0247b39d0ee7845e97d78639279ebedee03': raise ValueError('normal output differs')
                    next_assets.append(result)
                mesh_assets = next_assets; texture_plans.append(normal_plan)
        # Reserve the destination exclusively; do not replace user data on a race.
        os.mkdir(out, 0o700)
        try:
            for path in final.iterdir():
                if path.is_symlink(): raise ValueError('unexpected output symlink')
                if path.is_file(): shutil.copyfile(path, out/path.name)
                elif path.is_dir():
                    if any(p.is_symlink() for p in path.rglob('*')): raise ValueError('unexpected nested output symlink')
                    shutil.copytree(path, out/path.name)
                else: raise ValueError('unexpected output type')
            if audio_plan is not None:
                with audio_plan.open('rb') as src, (out/'shared-audio-plan.bin').open('xb') as dest:
                    shutil.copyfileobj(src, dest)
            if layout_plan is not None:
                with layout_plan.open('rb') as src, (out/'shared1-layout-plan.bin').open('xb') as dest:
                    shutil.copyfileobj(src, dest)
            for plan in (material_plan, mesh_plan, *mesh_extra_plans, *texture_plans):
                if plan is not None:
                    with plan.open('rb') as src, (out/plan.name).open('xb') as dest: shutil.copyfileobj(src, dest)
        except BaseException:
            shutil.rmtree(out)
            raise
    print('Rebuilt the pinned '+('localization' if windows else 'shader' if shaders else 'compact')+' base. Keep it private; this is not an APK build.')


if __name__ == '__main__':
    main()
