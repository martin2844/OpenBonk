#!/usr/bin/env python3
"""Build one pinned local Android diagnostic with current launcher and Lua.

This is not a release, a general user installer, or a distributable package.
It uses local experimental bases and includes game and third-party material.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]
BASE = ROOT/'build/android/localization-shared-scrubbed-base'
OLD_SPLIT = ROOT/'build/android/no-rewired-split-repaired-v2'
TRANSACTION_SPLIT = ROOT/'build/android/launcher-recreation-split-repaired'
COMPATIBILITY = ROOT/'build/android/local-runtime-compatibility.zip'
OBB = 'main.1.com.megabonk.nativeport.splittest.obb'
PINS = {BASE/'base.apk':'2b5bf0d217a18bbb9ef295672654cb4f9fe62ea308ad0372bd1dc44640f25510',
        BASE/'base.obb':'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d',
        BASE/'localization-shared-manifest.json':'035de511eae73fde3c913869f38203294ce2177db672fbb3eac59ae34e372722',
        BASE/'localization-text-manifest.json':'0d3a6cc6ae23ea7b915664358b0cc619a1f5f450b595c1b9029249ac67e42cef',
        BASE/'builtin-shader-manifest.json':'c46fce9e463d02580c4fe748116f3761e1851e8fba5306a32e6bc876181dc108',
        BASE/'feature-shader-manifest.json':'68937ba74996405d19dd5ac3a9b2bf977c989e6f561706f4d4bf40d931ee7f22',
        BASE/'instancing-reference-manifest.json':'a45ad32e40a0e84788818dd20c21f06c56a93fe95b27d3572fe95980e39998df',
        BASE/'instancing-shader-manifest.json':'856e2b6801c7a45483e80c296cf3e439c9ce8ba062339914c4161c558214d241',
        BASE/'reference-shader-manifest.json':'28e11a2f01e8cc56475e12507b1747db61ac0b99ad4339b3428340d243e81a74',
        BASE/'local-shader-manifest.json':'8c17255c338cdfcc5abe02f4d23c46edd498a4800802f57cad200cd05e25f0c8',
        BASE/'fog-shader-manifest.json':'f5c6d801ef3bc40de5bfe709a07bf8f179dbc3f50715fc0db46953d1b6461390',
        BASE/'render-shader-manifest.json':'ce07ca801c3107d55d5058d605a11c081a8e5a6ca151ff970663181d3d1dc5ad',
        BASE/'compute-shader-manifest.json':'ad767897929ffb988fd841832d320fe0a6136cd9c6d668fdea045c4f1124c018',
        BASE/'material-extra-0/manifest.json':'8214d9517d17771a29347983f34c2916eba126f6f70cbcd82b9b43bc5899b976',
        BASE/'material-extra-2/manifest.json':'04f6eb9faac05dddd8e5ce4650733b666eb09bc430c98e00fe43be7740804a0f',
        BASE/'material-extra-4/manifest.json':'7bcd0832f3d79e5a2cbcda3fecc2deb6ab487d7ea03ac1c8a4d3102631aebe84',
        COMPATIBILITY:'3d9813b602fe57056bccf37fc223ade9c5c9d6a9cca04804f4ec4287307d1e6e',
        ROOT/'build/android/local-split-apk-inventories/old.json':'79f7712afec39536a9c5dc3436f021764a466c60339d79e7bc460dbad2e0f69b',
        ROOT/'build/android/launcher-recreation-inventories/apk.json':'ab75585afb1791c28b892f04afd4cdc43bc35524f99ff903b170bf90c18acb4d',
        ROOT/'build/android/launcher-recreation-inventories/payload.zip':'ccb77f3b9532331715f88d13183c43c078cf90cfa4fa41dc4732b0209d4b5a90',
        ROOT/'build/android/launcher-recreation-inventories/obb.json':'db0a434a41c8f2669b2bc47afae232b7c120250139d4358271eba95b0735b41d',
        ROOT/'build/android/control-choice-runtime/launcher.bsdiff':'5bc8b067d68372d0b3b3b6454dceb59988859c1e30ad5f9203aa0e5b7a6c951d',
        ROOT/'build/android/control-ack-runtime/classes.bsdiff':'b47f4ba098b454c3e5db8d0ec41004670021d633265c031bbfcec3d01ccdbd95',
        ROOT/'build/android/control-ack-runtime/metadata.bsdiff':'ceb5b26085406ab20f9c7ab5cfa7bee69f5e9c3e4e38e17995a0ca5ab2a861b2',
        ROOT/'build/android/control-ack-runtime/runtime.bsdiff':'a0bdcce5938eec8cfeb1d732048ef11822b5cb4d556259725251f1d831d4a7a0',
        ROOT/'build/android/mod-folder-runtime/launcher.bsdiff':'f64bf103c94ce464e733b650c6ce2f1a32a322edcc0971fe709ac9d95455048d',
        ROOT/'build/android/local-split-obb-inventories/old.json':'d4fb4b0da8553042474bd68664d3ba08964cf9975fea295b0da0bcba23b2ccde',
        ROOT/'build/android/shared0-template-delta/delta.zlib':'ddfddc2154ab570c0e321fc8fb3a23cb11cbc2e6b6b52ce21e6e0e190df909bd',
        ROOT/'build/android/shared0-template-delta/textures-manifest.json':'e2b44b36a3d60d315f7e84861f5f0c84f71e4300894b3ee7d4f4353c09e40838',
        ROOT/'build/android/shared1-object-delta/delta.zlib':'5968da095314b7fd11532bf4c1bd2050079b4f894bbede41c2dc2bf06a803d29',
        ROOT/'build/android/shared1-object-delta/textures-manifest.json':'a8c72940f67acabb614f4e9a5c1cccab198f6b7a312960c738890795ecbb3be1',
        ROOT/'build/android/shared2-mesh-object-delta/delta.zlib':'70b245cdec76c91b45464037bd18efd43c52d89a4b757350bfc513ec8c445ef1',
        ROOT/'build/android/shared2-mesh-object-delta/mesh-manifest.json':'831fee2261236f0ec9db20798e5b8f4c05bfb2baa786e26d9ed65b5acfb26b61',
        }
WINDOWS_LAUNCHER_PINS = {
    ROOT/'build/android/windows-launcher-runtime/classes.bsdiff':'0b3f99b9f0e2da5fc7411fbe6db483ba407f32b86d440d39532365ddb2423cb5',
    ROOT/'build/android/windows-launcher-runtime/metadata.bsdiff':'70331309a29bf8b664289ae0cf6a190815f074cceeb8dabff1d2e123ba30e6db',
    ROOT/'build/android/windows-launcher-runtime/runtime.bsdiff':'cdc5539a29c243682003781bbcbd72ba82d78122cc12106d3148e850d2220de5',
    ROOT/'build/android/direct-windows-runtime/classes.bsdiff':'00f6a99f2626460ac0b3634ba4ccab0b4a86999143cef6f3c1997ba8a26112df',
    ROOT/'build/android/direct-windows-runtime/metadata.bsdiff':'10757794ce292d5def0573abb5e1440e2602b5be16b6641276e235964ef912e1',
    ROOT/'build/android/direct-windows-runtime/runtime.bsdiff':'f1afbe5e78b2b6133707d5daa108b002bc7d77a6d715de436466584be0edf7fd',
    ROOT/'build/android/steam-picker-runtime/classes.bsdiff':'f3f5e23b41cad7969a88c9707901c32a8e5dcb5ebec5ce5288c571920755afee',
}
FLAGS = '''--experimental-hashed --experimental-scene-bodies --experimental-extra-mono
--experimental-pinned-inputs --experimental-direct-rebuild --experimental-scrubbed-shared
--experimental-scrubbed-hashed --experimental-expanded-shared --experimental-compact-base
--experimental-shared4-audio --experimental-shared4-meshes --experimental-scenes-from-base
--experimental-hashed-from-base --experimental-shared4-textures --experimental-shared4-particles
--experimental-shared4-particle-references --experimental-gamepad-texture
--experimental-shared2-animation-streams --experimental-shared0-font-metadata
--experimental-font-semantic-references --experimental-avoid-legacy-font-patches
--experimental-hybrid-animation-curves --experimental-preload-particles
--experimental-preload-gameobjects --experimental-preload-extra-components --experimental-current-splash --experimental-scene-transforms --experimental-scene-gameobjects --experimental-material-values --experimental-shared0-delta --experimental-shared12-delta'''.split()


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def checked_manifest_root():
    root = Path(os.environ.get('OPENBONK_MANIFEST_ROOT', str(ROOT/'build/android/local-research-manifests'))).expanduser().resolve()
    spec = importlib.util.spec_from_file_location('local_manifest_cache', ROOT/'tools/local-manifest-cache.py')
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    module.verify(root)
    return root


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('pc_data', type=Path, help='pinned Steam Megabonk_Data directory')
    p.add_argument('output', type=Path, help='new local output directory')
    p.add_argument('--windows-only', action='store_true', help='experimental: use the pinned Windows Steam build for all supported sources')
    p.add_argument('--windows-launcher', action='store_true', help='experimental: add the Windows ZIP reader tested in Android')
    p.add_argument('--windows-scene-bodies', type=Path, help='experimentally source selected scene bodies from the pinned Windows build')
    p.add_argument('--windows-gamepad-animations', type=Path, help='experimentally source controller texture and two animation streams from Windows')
    p.add_argument('--windows-buffering-video', type=Path, help='experimentally source the buffering clip from Windows')
    p.add_argument('--windows-terrain-four', type=Path, help='experimentally source terrain, animations and 63 textures from Windows')
    p.add_argument('--windows-core-assets', type=Path, help='experimentally source selected core textures, meshes, cubemaps and audio from Windows')
    p.add_argument('--windows-hashed-assets', type=Path, help='experimentally source selected hashed assets from Windows')
    p.add_argument('--windows-shared0', type=Path, help='experimentally source selected shared0 textures, cubemap and font assets from Windows')
    p.add_argument('--windows-materials', type=Path, help='experimentally source selected material values from Windows')
    p.add_argument('--windows-remaining-assets', type=Path, help='experimentally source water normal, preload bodies and splash from Windows')
    p.add_argument('--windows-shared2', type=Path, help='experimentally source shared2 meshes, textures, atlases and audio from Windows')
    p.add_argument('--windows-shared4', type=Path, help='experimentally source final-map particles, textures, meshes and audio from Windows')
    a = p.parse_args(); pc = a.pc_data.resolve(); out = a.output.absolute()
    if a.windows_only:
        for name in ('windows_scene_bodies', 'windows_gamepad_animations', 'windows_buffering_video',
                     'windows_terrain_four', 'windows_core_assets', 'windows_hashed_assets',
                     'windows_shared0', 'windows_materials', 'windows_remaining_assets',
                     'windows_shared2', 'windows_shared4'):
            if getattr(a, name) is not None: p.error('--windows-only cannot be combined with individual Windows source options')
            setattr(a, name, pc)
    if not pc.is_dir(): p.error('Steam data directory is missing')
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    if out.resolve().is_relative_to(pc) or pc.is_relative_to(out.resolve()): p.error('output must be separate from Steam data')
    if a.windows_scene_bodies:
        win = a.windows_scene_bodies.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        spec = importlib.util.spec_from_file_location('windows_scene_preflight', ROOT/'analysis/data-conversion/no-rewired-scene-bodies-prototype.py')
        scenes = importlib.util.module_from_spec(spec); spec.loader.exec_module(scenes)
        for name, expected in scenes.WINDOWS_SCENE_PINS.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows scene: '+name)
    if a.windows_gamepad_animations:
        win = a.windows_gamepad_animations.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        for name, expected in (('sharedassets1.assets','64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'),('sharedassets1.assets.resS','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'),('sharedassets2.assets','cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9')):
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_buffering_video:
        win = a.windows_buffering_video.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        for name, expected in (('sharedassets1.assets','64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'),('sharedassets1.resource','29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d')):
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_terrain_four:
        win = a.windows_terrain_four.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        for name, expected in (('sharedassets1.assets','64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'),('sharedassets1.assets.resS','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea')):
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_core_assets:
        win = a.windows_core_assets.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        pins={'sharedassets1.assets':'64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets1.resource':'29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d','level1':'9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','level1.resS':'bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6'}
        for name, expected in pins.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_hashed_assets:
        win = a.windows_hashed_assets.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        spec=importlib.util.spec_from_file_location('windows_sources_preflight',ROOT/'analysis/data-conversion/windows_sources.py')
        source_pins=importlib.util.module_from_spec(spec);spec.loader.exec_module(source_pins)
        for name, expected in source_pins.WINDOWS_PINS.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_shared0:
        win = a.windows_shared0.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        for name, expected in (('sharedassets0.assets','772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d'),('sharedassets0.assets.resS','374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b'),('globalgamemanagers.assets','9d7146c2d338034b798aec477ce5c3e494a9cc0969861c2706996bf69ae16915')):
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_materials:
        win = a.windows_materials.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        pins={'sharedassets0.assets':'772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d','sharedassets1.assets':'64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','sharedassets2.assets':'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','sharedassets4.assets':'896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9'}
        for name, expected in pins.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_remaining_assets:
        win = a.windows_remaining_assets.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        pins={'sharedassets1.assets':'64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea','sharedassets2.assets':'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','globalgamemanagers.assets':'9d7146c2d338034b798aec477ce5c3e494a9cc0969861c2706996bf69ae16915','globalgamemanagers.assets.resS':'39dd38fcd8dd95d41d6d55fe16a448d29fe2c31d9a162c825c24c6d99a893d7a'}
        for name, expected in pins.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_shared2:
        win = a.windows_shared2.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        pins={'sharedassets2.assets':'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9','sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84','sharedassets2.resource':'79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa','level1':'9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','level1.resS':'bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6','level2':'853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6','level2.resS':'544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51','level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}
        for name, expected in pins.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    if a.windows_shared4:
        win = a.windows_shared4.resolve()
        if not win.is_dir() or out.resolve().is_relative_to(win) or win.is_relative_to(out.resolve()): p.error('Windows data must exist and be separate from output')
        pins={'sharedassets4.assets':'896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9','sharedassets4.assets.resS':'862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127','sharedassets4.resource':'aa7cba281dd84a509bc1c0f200fa69cb21640615d92f16a65e9959db69a3b78b','level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}
        for name, expected in pins.items():
            if not (win/name).is_file() or sha(win/name) != expected: p.error('incompatible Windows resource: '+name)
    for path, expected in PINS.items():
        if not path.is_file() or sha(path) != expected: p.error('missing or incompatible local research base: '+str(path))
    if a.windows_launcher:
        for path, expected in WINDOWS_LAUNCHER_PINS.items():
            if not path.is_file() or sha(path) != expected: p.error('missing or incompatible Windows launcher patch: '+str(path))
    video_pc = a.windows_buffering_video.resolve() if a.windows_buffering_video else pc
    video_pins = (('sharedassets1.assets', '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'),
                  ('sharedassets1.resource', '29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d')) if a.windows_buffering_video else (
                  ('sharedassets1.assets', 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'),
                  ('sharedassets1.resource', '28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'))
    for name, expected in video_pins:
        path=video_pc/name
        if not path.is_file() or sha(path)!=expected:p.error('missing or incompatible Steam video source: '+str(path))
    runner = os.environ.get('OPENBONK_TOOL_RUNNER', 'steam-run' if shutil.which('steam-run') else 'none')
    if runner != 'none' and not shutil.which(runner): p.error('configured runner is missing: '+runner)
    env = os.environ.copy()
    env['OPENBONK_MANIFEST_ROOT'] = str(checked_manifest_root())
    libs = [str(path) for path in (Path('/nix/store/wgw25d181h0qks7qsfp1bh7c5nd3f0s7-zlib-1.3.2/lib'),
                                   Path('/nix/store/0ba5kj6r0zhfwd84l5vzccqvx7amlq2c-gcc-15.3.0-lib/lib')) if path.is_dir()]
    if env.get('LD_LIBRARY_PATH'): libs.append(env['LD_LIBRARY_PATH'])
    if libs: env['LD_LIBRARY_PATH'] = os.pathsep.join(libs)
    with tempfile.TemporaryDirectory(prefix='.openbonk-current-runtime-', dir=out.parent) as t:
        t = Path(t); rebuilt, rebased, video_fixed = t/'rebuilt', t/'rebased', t/'video-fixed'
        shader_bundles=t/'generated-shaders';shared_locale_base=t/'restored-shared-localization';locale_base=t/'restored-localization';builtin_base=t/'restored-builtin';feature_base=t/'restored-feature';instance_ref_base=t/'restored-instance-reference';instance_base=t/'restored-instance';reference_base=t/'restored-reference-base';local_base=t/'restored-local-base';fog_base=t/'restored-fog-base';render_base=t/'restored-render-base';shader_base=t/'restored-shader-base'
        restored_base=t/'restored-material-base'
        shader_cache = os.environ.get('OPENBONK_SHADER_BUNDLES')
        if shader_cache:
            spec = importlib.util.spec_from_file_location('shader_cache', ROOT/'tools/pinned-shader-bundles.py')
            cache = importlib.util.module_from_spec(spec); spec.loader.exec_module(cache)
            cache.stage(Path(shader_cache).expanduser(), shader_bundles)
            print('Usando shaders locales verificados; no se ejecutó el editor de Unity.', flush=True)
        else:
            subprocess.run([sys.executable, str(ROOT/'tools/build-compute-shaders-local.py'), str(BASE/'compute-shader-manifest.json'), str(shader_bundles), '--render-manifest', str(BASE/'render-shader-manifest.json'), '--fog-manifest', str(BASE/'fog-shader-manifest.json'), '--local-manifest', str(BASE/'local-shader-manifest.json'), '--reference-manifest', str(BASE/'reference-shader-manifest.json'), '--instancing-manifest', str(BASE/'instancing-shader-manifest.json'), '--instancing-reference-manifest', str(BASE/'instancing-reference-manifest.json'), '--feature-manifest', str(BASE/'feature-shader-manifest.json'), '--builtin-manifest', str(BASE/'builtin-shader-manifest.json')], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-localization-shared-base.py'), str(BASE), str(pc), str(shared_locale_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-localization-base.py'), str(shared_locale_base), str(pc), str(locale_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-builtin-shader-base.py'), str(locale_base), str(shader_bundles), str(builtin_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-feature-shader-base.py'), str(builtin_base), str(shader_bundles), str(feature_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-instancing-reference-base.py'), str(feature_base), str(shader_bundles), str(instance_ref_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-instancing-shader-base.py'), str(instance_ref_base), str(shader_bundles), str(instance_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-reference-shader-base.py'), str(instance_base), str(shader_bundles), str(reference_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-local-shader-base.py'), str(reference_base), str(shader_bundles), str(local_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-fog-shader-base.py'), str(local_base), str(shader_bundles), str(fog_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-render-shader-base.py'), str(fog_base), str(shader_bundles), str(render_base)], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/restore-compute-shader-base.py'), str(render_base), str(shader_bundles), str(shader_base)], check=True, env=env)
        material_pc=a.windows_materials.resolve() if a.windows_materials else pc
        material_source=['--windows-source'] if a.windows_materials else []
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/additional-material-base.py'), 'restore', str(shader_base), str(restored_base), '--steam-data', str(material_pc), *material_source], check=True, env=env)
        windows_args = ['--windows-scene-bodies',str(a.windows_scene_bodies.resolve())] if a.windows_scene_bodies else []
        if a.windows_gamepad_animations: windows_args += ['--windows-gamepad-animations',str(a.windows_gamepad_animations.resolve())]
        if a.windows_terrain_four: windows_args += ['--windows-terrain-four',str(a.windows_terrain_four.resolve())]
        if a.windows_core_assets: windows_args += ['--windows-core-assets',str(a.windows_core_assets.resolve())]
        if a.windows_hashed_assets: windows_args += ['--windows-hashed-assets',str(a.windows_hashed_assets.resolve())]
        if a.windows_shared0: windows_args += ['--windows-shared0',str(a.windows_shared0.resolve())]
        if a.windows_materials: windows_args += ['--windows-materials',str(a.windows_materials.resolve())]
        if a.windows_remaining_assets: windows_args += ['--windows-remaining-assets',str(a.windows_remaining_assets.resolve())]
        if a.windows_shared2: windows_args += ['--windows-shared2',str(a.windows_shared2.resolve())]
        if a.windows_shared4: windows_args += ['--windows-shared4',str(a.windows_shared4.resolve())]
        subprocess.run([sys.executable, str(ROOT/'tools/build-no-rewired-data-diagnostic.py'), str(pc), str(rebuilt), '--scrubbed-scene-base', str(restored_base), *FLAGS, *windows_args], check=True, env=env)
        subprocess.run([sys.executable, str(ROOT/'analysis/port-inventory/rebase-launcher-recreation-runtime-prototype.py'), str(OLD_SPLIT), str(TRANSACTION_SPLIT), str(rebuilt), str(rebased)], check=True, env=env)
        video_pc = a.windows_buffering_video.resolve() if a.windows_buffering_video else pc
        video_args = ['--windows-source'] if a.windows_buffering_video else []
        subprocess.run([sys.executable, str(ROOT/'analysis/data-conversion/restore-buffering-launcher-recreation-prototype.py'), str(video_pc), str(rebased), str(video_fixed), *video_args], check=True, env=env)
        control_choice=t/'control-choice'
        subprocess.run([sys.executable, str(ROOT/'tools/apply-control-choice-launcher.py'), str(video_fixed), str(control_choice)], check=True, env=env)
        control_ack=t/'control-ack'
        subprocess.run([sys.executable, str(ROOT/'tools/apply-control-ack-launcher.py'), str(control_choice), str(control_ack)], check=True, env=env)
        final=t/'mod-folder'
        subprocess.run([sys.executable, str(ROOT/'tools/apply-mod-folder-launcher.py'), str(control_ack), str(final)], check=True, env=env)
        if a.windows_launcher:
            upgraded=t/'windows-launcher'
            subprocess.run([sys.executable, str(ROOT/'tools/apply-windows-launcher.py'), str(final), str(upgraded)], check=True, env=env)
            direct=t/'direct-windows-launcher'
            subprocess.run([sys.executable, str(ROOT/'tools/apply-direct-windows-launcher.py'), str(upgraded), str(direct)], check=True, env=env)
            steam_picker=t/'steam-picker-launcher'
            subprocess.run([sys.executable, str(ROOT/'tools/apply-steam-picker-launcher.py'), str(direct), str(steam_picker)], check=True, env=env)
            final=steam_picker
        media_report=t/'media-audit.json'
        subprocess.run([sys.executable, str(ROOT/'analysis/data-conversion/audit-rebuilt-media.py'), str(final/'OpenBonk-diagnostic.apk'), str(final/OBB), str(media_report)], check=True, env=env)
        media=json.loads(media_report.read_text())
        if (media['counts']!={'AudioClip:fsb5_structural':311,'VideoClip:webm_header':1}
                or any(row.get('zip_storage')!='stored' for row in media['media'])
                or media['apk_sha256']!=sha(final/'OpenBonk-diagnostic.apk') or media['obb_sha256']!=sha(final/OBB)):
            raise ValueError('media reference audit failed')
        out.mkdir()
        try:
            for name in ('OpenBonk-diagnostic.apk', OBB, 'report.json'):
                shutil.copyfile(final/name, out/name)
            shutil.copyfile(video_fixed/'report.json', out/'video-report.json')
            shutil.copyfile(media_report,out/'media-audit.json')
        except BaseException:
            shutil.rmtree(out); raise
        print('Created local research diagnostic:', out, '; not for distribution', flush=True)


if __name__ == '__main__': main()
