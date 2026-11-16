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
    parser.add_argument('--java-home', type=Path, help='JDK directory for material and mesh plan checks')
    parser.add_argument('--java-runner', default='none', help='wrapper for Java tools, or none')
    converted_group = parser.add_mutually_exclusive_group(required=True)
    converted_group.add_argument('--converted-obb', type=Path, help='pinned historical converted OBB')
    converted_group.add_argument('--converted-assets', type=Path, help='directory of three independently rebuilt shared assets')
    converted_group.add_argument('--rebuild-converted-assets', action='store_true', help='regenerate the shared assets and their templates from the old APK/OBB and Steam')
    args = parser.parse_args()
    if args.include_layout_plan and not args.windows_data: parser.error('--include-layout-plan requires --windows-data')
    if args.include_material_mesh_plans and not (args.include_layout_plan and args.linux_zip and args.windows_zip and args.java_home):
        parser.error('--include-material-mesh-plans requires --include-layout-plan, both Steam ZIPs and --java-home')
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
    if args.java_runner != 'none' and not shutil.which(args.java_runner): parser.error('Java runner not found')
    if any(out.is_relative_to(p) or p.is_relative_to(out) for p in (ROOT, *([converted_input] if converted_input else []), *([shaders] if shaders else []), *([windows] if windows else []), *([java] if java else []), *zips, *inputs.values())):
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
        if args.include_material_mesh_plans:
            java_source = ROOT/'analysis/data-conversion/android'; classes = tmp/'java-classes'; classes.mkdir()
            prefix = [] if args.java_runner == 'none' else [args.java_runner]
            def java_run(*command): subprocess.run([*prefix, *(str(x) for x in command)], check=True)
            java_run(java/'bin/javac', '-d', classes, *(java_source/(name+'.java') for name in
                     ('SteamExactTextures', 'SteamShared1Layout', 'SteamShared1LayoutCheck',
                      'SteamShared1MaterialAudio', 'SteamShared1MaterialAudioCheck', 'SteamShared1Meshes', 'SteamShared1MeshesCheck')))
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
            for i, steam_zip in enumerate(zips):
                result = tmp/f'mesh-{i}.assets'
                check_java('SteamShared1MeshesCheck', material_assets[i], steam_zip, mesh_plan, result, 'pass')
                with result.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != '26df1db6a89a775557ddc9d29ac6bddb10f7f47f7370bd6064dce08cec0b1119': raise ValueError('mesh output differs')
            for path, expected in ((material_plan, 'fc96c214a73e702069425abe9bc75f5bdd825d9f7c73164c33c685527faafa3f'),
                                   (mesh_plan, '7ac1084c785c3a015821b3a155aa9e4f39bee48b3a10911e91c05b5df304d0c5')):
                with path.open('rb') as f:
                    if hashlib.file_digest(f, 'sha256').hexdigest() != expected: raise ValueError('shared1 plan differs: '+path.name)
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
            for plan in (material_plan, mesh_plan):
                if plan is not None:
                    with plan.open('rb') as src, (out/plan.name).open('xb') as dest: shutil.copyfileobj(src, dest)
        except BaseException:
            shutil.rmtree(out)
            raise
    print('Rebuilt the pinned '+('localization' if windows else 'shader' if shaders else 'compact')+' base. Keep it private; this is not an APK build.')


if __name__ == '__main__':
    main()
