#!/usr/bin/env python3
"""Rebuild three shared assets from pinned local Steam data for a diagnostic.

Local experiment only. Inputs, templates and outputs retain game and third-party
material. This is not a distributor, public installer, or release build. It
does not install anything or modify the Steam installation or Android devices.
"""
import argparse
import contextlib
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
BUILD = ROOT/'build/android'
MANIFEST_ROOT = Path(os.environ.get('OPENBONK_MANIFEST_ROOT',str(BUILD/'share-source-experiment'))).expanduser()
OLD_APK = BUILD/'Megabonk-arm64-split-test.apk'
OLD_OBB = BUILD/'Megabonk-arm64-split-test.main.obb'
NEW_PAIR = BUILD/'no-rewired-split-repaired-v2'
NEW_APK = NEW_PAIR/'OpenBonk-diagnostic.apk'
OBB_NAME = 'main.1.com.megabonk.nativeport.splittest.obb'
NEW_OBB = NEW_PAIR/OBB_NAME
PINNED = {OLD_APK:'850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5', OLD_OBB:'baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d', NEW_APK:'b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38', NEW_OBB:'ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'}
EXPECTED = {'sharedassets0.assets':'ff3b5e27ca87d1a9186c485ab90e599fa02fb442347fb42c6ff214e14306933c','sharedassets1.assets':'b6b1d5fd2d261c1163608dfcdf354eca87afbec514840b963a9b5c50ce7fd33e','sharedassets2.assets':'d8811ed3806e81bd7b9a44a69789b948a845ff3cb8bce67301aa662c0b5a34aa','sharedassets1.resource':'93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6','sharedassets2.resource':'6abc02d2702824b936add3ea2a6780b5b10e2954e1df7543b8cc79b5c9e1b1bc'}
SCRUBBED_SCENE_APK='76386a92c658dda1d262f2611c6bed5182196c5a36d372eb2b6ca8725215857f'
SCRUBBED_SCENE_OBB='cac0c58a6852be36afc9e284e88bdad443a74ac8ce52db4b905b2442f79914a3'
SCRUBBED_SHARED_APK='e131cce897989fe2b05cb304aa749e25e4c3c2e90a60d2d7fc8f5870ad106ce3'
SCRUBBED_SHARED_OBB='ca8694de6c7148e1c93eb2d9fa45525edfaf87e39deffad67641c80f2e3bab23'
SCRUBBED_SHARED_MANIFEST='c4cec041aec2aac138b0ddae6b62ca354701340dd8b0eae02f6938aff5e1a082'
SCRUBBED_HASHED_APK='5383c674bc5f6131498859e05a01324379bec6094bbecd5c4182b1967f0f5be8'
SCRUBBED_HASHED_OBB='ca3a6f50dbcb61e798e525d69700f4e111b68a99b8a38aa186f0dfe2d6373852'
SCRUBBED_HASHED_MANIFEST='876d086b963679f1513532737859c5a79133d706f5e7f64643b0007ef53515df'
EXPANDED_HASHED_APK='1bab76352b72bddec06216ffbfc8d83b2ce00cf28387c7b1d817906641dc7406'
EXPANDED_HASHED_OBB='ca79d294f0082d4b3ceb41c7cfb42329a72f760bce1599fdc0c3cc6daf2a005b'
EXPANDED_SHARED_MANIFEST='294055b5e8c9edec427883c2d1ea73fddcd39b25e9f2ad37b8804a08b8924325'
COMPACT_APK='cc53e5e6c88dd5c4e6b57b7930bf6650eec889b267e9ca5feb49faa5ff0a0216'
COMPACT_OBB='ca2280993b055ff7ad2172169ba100c895e69781e67ad4af53b71b027a9159af'
SHARED4_BASE_APK='ed93dc58ffab7d3d152d48586f3cf4ed9ef4842b04026710f9ed169e2cfdf3a4'
SHARED4_BASE_OBB='ca28198dfbb8d67eaa6e066fada95f1c4cc2bb8658dbf9dd69b3de597c868de8'
SHARED4_MESH_APK='4bc532e815fcaf058527fc1ffb6d3ae2ab7057c0fcb382eb68b3afcc83c79bab'
SHARED4_MESH_OBB='ca2fd174f7a0eceebef4d5df0c4bc8031f6addf2be9895b3164525dcbf251041'
SHARED4_MESH_MANIFEST='5599b039f9891bb624c077a8cfbad52b8dd383d47a87e1d1813b375f6a3b44ff'
SHARED4_TEXTURE_APK='d1fd6eff774e54b7e6685e7cce8d2d9b47a2de5dd78e7a2d6287922d1efcb24b'
SHARED4_TEXTURE_OBB='ca1257a62b412bddb02cb60a7c4b03397d2616b3a68650a6aa3b56e9ccebefad'
SHARED4_TEXTURE_MANIFEST='08001488876c5f3c1faca6737507b5d72776ab5c4f743325e253dbf055428354'
SHARED4_TEXTURE_ASSET='6185cfca7d60e4518d0f6d37753367230a8d15f899cdb674348aba04c5722f2f'
SHARED4_PARTICLE_APK='6923ddae9d6fd352b13b144fd6cf7f192e4133bfcb132d23eb5c18b21df53451'
SHARED4_PARTICLE_OBB='ca2cecc18fe83e50eae46c56ef4dc4e712b3ce89313e7bd91409075f36b913f6'
SHARED4_PARTICLE_MANIFEST='16119d8d7dd6ba8f7bdce87ab4033875fa9106386c60a87e22eba042e9e1820f'
SHARED4_PARTICLE_REFERENCE_APK='864defe28eca6310d9fd3a0a97a2365621ce47f12cb19c7c2bc750f30c798e7d'
SHARED4_PARTICLE_REFERENCE_OBB='cab3394d867cd28e30e5a4528ea8d64b9eb3835bcd2ff82dcb3f77267d06b0dd'
SHARED4_PARTICLE_REFERENCE_MANIFEST='97fe46c853bbbfdb1fd5f54cf6b281f3eeae1fb5c43db230e5c4455d6171983f'
GAMEPAD_BASE_APK='cb30d85bc6cc2d2ce61e7ef16dc59da2a762bc52a4e5bfdbf98f3be8fa22c2ca'
GAMEPAD_BASE_OBB='ca5a34fe3c0d84af23072913478413546034ced84a728f653c570e252fe590c6'
GAMEPAD_MANIFEST='6b6e7b3ffd146286760664d87a583c1271e9ad4058a00ded438eab151992a616'
GAMEPAD_FINAL_SHARED2='aa581f3af73a1b1e0cc20b0ba8a73705db19a474bc8153687c0ed36ef7941616'
ANIMATION_BASE_APK='88f026dde2886e8a33cabaca43e3cfba7ddaafe63e25b64032ed5a39588f94aa'
ANIMATION_BASE_OBB='ca99de9c1fb7a0d917a99cd2fc4fe0f421a1e44b4b7e2eef395551e8cbe3110d'
ANIMATION_MANIFEST='c260aa264809a9afe70937c6c10097dde70c13ec92a2d127cdfe9aa57d5bb20f'
ANIMATION_FINAL_SHARED2='a3bb71785a61a2acddb230e28b1b1f8466c076a944112e18273433a635faea37'
FONT_BASE_APK='8e7fb232d255905840cc34b8efd3a72b0dbc3693c8a34635ff85f5c95e3b0892'
FONT_MANIFEST='f3f899fbbad2b29534dfb6854b1f33552616112b2afc1569db2ec7280bf69c5c'
SHARED4_ASSET='cd0a2e7c6f93ecc00bdde165a6654b3818c3e4afaaad2eb52068a93a62b1c29a'
SHARED4_AUDIO='2f320598479bd723d63bb920cc582b62c95f5961e59eca85d9ca20f1782824f7'
UNITY = Path(os.environ.get('OPENBONK_UNITY_EDITOR',str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Data/PlaybackEngines/AndroidPlayer'
RUNNER = os.environ.get('OPENBONK_TOOL_RUNNER','steam-run' if shutil.which('steam-run') else 'none')
PREFIX = [] if RUNNER == 'none' else [RUNNER]
FLAGS = ['--experimental-font-atlases','--experimental-font-metadata','--experimental-default-cubemap','--experimental-remaining-textures','--experimental-terrain-animations','--experimental-shared1-textures','--experimental-shared1-four-textures','--experimental-water-rgba','--experimental-shared2-atlases','--experimental-shared2-audio','--experimental-shared2-textures','--experimental-shared2-meshes']
HASHED_FLAGS = ['--experimental-hashed-meshes','--experimental-hashed-animations','--experimental-hashed-textures','--experimental-hashed-matched','--experimental-hashed-rgba','--experimental-hashed-extra','--experimental-hashed-final','--experimental-hashed-audio','--experimental-scene-particles','--experimental-scene-references','--experimental-scene-mono']
HASHED_CATEGORIES = [('hashed-mesh-template','meshes',50),('hashed-animation-template','animations',108),('hashed-exact-textures-template','textures',56),('hashed-matched53-template','textures',53),('hashed-excluded-rgba-template','textures',10),('hashed-extra-candidates-template','textures',13),('hashed-final-candidates-template','textures',2),('hashed-audio-template','clips',63)]
HASHED_TEMPLATE_OPTIONS=['--hashed-mesh-template','--hashed-animation-template','--hashed-texture-template','--hashed-matched-template','--hashed-rgba-template','--hashed-extra-template','--hashed-final-template','--hashed-audio-template']
HYBRID_ANIMATION_MANIFEST=BUILD/'no-rewired-hashed-animation-hybrid-manifest.json'
HYBRID_ANIMATION_SHA='716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'
PRELOAD_PARTICLE_APK='dad55ad51e47593bf457538bc20d2078bbaa545627505681fc89088a4c68b37c'
PRELOAD_PARTICLE_OBB='ca1b8fc1992bc18a641cf6ce899cd9caefc2108d328731cf49b2d98491a42567'
PRELOAD_PARTICLE_MANIFEST='c062e92a1acc22e52c050ffb542f434086c61d9f2ae1addb7136685fa2c444d7'
PRELOAD_OBJECTS_APK='fc550a92a5afd94af4a47076b6f9bad034d764708c617fc9a65cb13e8fe27d86'
PRELOAD_OBJECTS_OBB='caa5e5b2809e0f5e0db659ef7589d7fffad764f9b6d64022097067b5a647ab05'
PRELOAD_OBJECTS_MANIFEST='825618022a8c01d0e24e74300a7261f1196d8950b44073946a60af89ae82ac33'
PRELOAD_EXTRA_APK='d20c58a2548196270e57b6e0392b4a103e09014ee4f01d4f4b970bb2924026a2'
PRELOAD_EXTRA_OBB='ca41168e07591229b061a7dfa8c46d70e56a455e4de1e57d5dd00425d09ae43d'
PRELOAD_EXTRA_MANIFEST='ccc8f077900aa731066a21c1a94b0081b11fc02a495bb20b6d884ed5d6bc59a5'
SPLASH_BASE_APK='6b8ccf42cf51cdcac9bbfab3a2ced1ad7a0e5b0485c2efa671d2d26d57cf8b27'
SPLASH_MANIFEST='67ae8f665b12c0ac0a03ca7ecf808f71379b99a5f1c52ac9f50509eca6515b44'
MATERIAL_BASE_APK='f756235db25a5aed06afb34ec7e0267aa49cf770eb148e0e3cd048674b13e93a'
MATERIAL_BASE_OBB='caba193e78eef5c4b2b4d4e5dd066306584ef151832e5f4c9f9d1ecfb4ca64fb'
MATERIAL_MANIFEST='d441f24f0e4c2efabd90428f5171e9dc2133f026c6fb9c0a1355ddf3b7da537b'
GAMEOBJECT_BASE_APK='9ae20660c192b251fc63a7831eb6c44a61d724d4b961e0a58bb4da47b3fc6bd4'
GAMEOBJECT_BASE_OBB='ca049117478a740c11cb4f6c105e1f3aa7ebc82dcbc4281b84b9c9b317d6e318'
GAMEOBJECT_MANIFEST='991d529e746effdc75c075f610ec88f3126f15520d61efdd6f7b0eb3b1f9bd04'
TRANSFORM_BASE_APK='5e251631fe41877dcd53f101346a3c19e75714971b4e0f72d765083012a4f27a'
TRANSFORM_BASE_OBB='ca12334ed5b939c61c01914e0caf3180087db6926b4feab82648caaf4f01e9bd'
TRANSFORM_MANIFEST='aff9388b6e8950a16c091db90ec3ba592189ab5af8b85d6ef5bedbc3f88977b2'
POSTPROCESSING_PACKAGE=ROOT/'reconstruction/AssetRipperExport/ExportedProject/Library/PackageCache/com.unity.postprocessing@3.4.0'
HASHED_MANIFEST_HASHES=['44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24','d906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8','72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9','7e4bd817c3138434082496ba797c5ef216798357b7f8ef7bc6ecf1fed02061a7','828fde47398ad39c9a0a42d2afba85111dd577f704f3fd5740c4ddcb5e79c76d','34e87d8502957407489faca56ae44db02c1ed7d7eed9b2363ffdb44f44504f44','e2d7f81ff5d42db9b8e698b063212e3abf0cffdcbe27d12737fa78739370e6cd','6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f']
SCENE_HASHES={'level1':'164012e485062f572bba37de85aafe42a494810202d80e13d4f72dbf20ce24dc','level2':'bbee6f3c886a795f0029626ed62ccc6d569fca3ce9ee821876619d3009a55dea','level4':'665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972'}


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024), b''): h.update(b)
    return h.hexdigest()


def run(cmd, env=None):
    print('+', ' '.join(map(str,cmd)), flush=True)
    subprocess.run(list(map(str,cmd)), check=True, env=env)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('pc_data',type=Path); p.add_argument('output',type=Path)
    p.add_argument('--experimental-hashed',action='store_true',help='also reconstruct matching hashed meshes, animations, textures and audio')
    p.add_argument('--experimental-scene-bodies',action='store_true',help='also restore selected new-build scene bodies (requires --experimental-hashed)')
    p.add_argument('--windows-scene-bodies',type=Path,help='experimentally source selected scene bodies from the pinned Windows Megabonk_Data directory')
    p.add_argument('--experimental-extra-mono',action='store_true',help='also restore additional exact component bodies (requires --experimental-scene-bodies)')
    p.add_argument('--scrubbed-scene-base',type=Path,help='use pinned local APK/OBB with selected scene bodies blanked')
    p.add_argument('--experimental-pinned-inputs',action='store_true',help='use pinned object hashes instead of opening the old full APK/OBB')
    p.add_argument('--experimental-direct-rebuild',action='store_true',help='rebuild inputs directly without packaging the old scrubbed pair')
    p.add_argument('--experimental-scrubbed-shared',action='store_true',help='restore selected blank shared objects and audio (requires direct rebuild)')
    p.add_argument('--experimental-scrubbed-hashed',action='store_true',help='restore selected blank hashed payloads (requires scrubbed shared mode)')
    p.add_argument('--experimental-expanded-shared',action='store_true',help='restore additional identical rebuilt objects (requires scrubbed hashed mode)')
    p.add_argument('--experimental-compact-base',action='store_true',help='use a base with compressed zero audio (requires expanded shared mode)')
    p.add_argument('--experimental-shared4-audio',action='store_true',help='rebuild six sharedassets4 clips (requires compact base mode)')
    p.add_argument('--windows-shared4',type=Path,help='experimentally source final-map particles, textures, meshes and audio from Windows')
    p.add_argument('--experimental-shared4-meshes',action='store_true',help='rebuild seven sharedassets4 mesh buffers (requires shared4 audio mode)')
    p.add_argument('--experimental-scenes-from-base',action='store_true',help='restore extra scene bodies from the scrubbed base instead of separate scenes')
    p.add_argument('--experimental-hashed-from-base',action='store_true',help='read individual hashed templates from the scrubbed OBB')
    p.add_argument('--experimental-shared4-textures',action='store_true',help='lossily rebuild three final-scene textures from Steam')
    p.add_argument('--experimental-shared4-particles',action='store_true',help='restore 33 final-scene particle bodies from Steam')
    p.add_argument('--experimental-shared4-particle-references',action='store_true',help='restore three additional particle bodies while preserving Android references')
    p.add_argument('--experimental-gamepad-texture',action='store_true',help='rebuild the controller texture from Steam using lossy compression')
    p.add_argument('--windows-gamepad-animations',type=Path,help='experimentally source controller texture and two animation streams from Windows')
    p.add_argument('--windows-terrain-four',type=Path,help='experimentally source terrain, animations and 63 textures from Windows')
    p.add_argument('--windows-core-assets',type=Path,help='experimentally source selected core textures, meshes, cubemaps and audio from Windows')
    p.add_argument('--windows-hashed-assets',type=Path,help='experimentally source selected hashed assets from Windows')
    p.add_argument('--windows-shared0',type=Path,help='experimentally source selected shared0 textures, cubemap and font assets from Windows')
    p.add_argument('--windows-remaining-assets',type=Path,help='experimentally source water normal, preload bodies and splash from Windows')
    p.add_argument('--windows-shared2',type=Path,help='experimentally source shared2 meshes, textures, atlases and audio from Windows')
    p.add_argument('--experimental-shared2-animation-streams',action='store_true',help='rebuild two animation streams from Steam with numeric differences')
    p.add_argument('--experimental-shared0-font-metadata',action='store_true',help='rebuild six multilingual font metadata objects from Steam')
    p.add_argument('--experimental-font-semantic-references',action='store_true',help='derive Android font references by matching named objects')
    p.add_argument('--experimental-avoid-legacy-font-patches',action='store_true',help='leave old intermediate font metadata blank')
    p.add_argument('--experimental-hybrid-animation-curves',action='store_true',help='use Steam streams for 25 close animation candidates')
    p.add_argument('--experimental-preload-particles',action='store_true',help='restore 101 preloaded particle bodies from Steam')
    p.add_argument('--experimental-preload-gameobjects',action='store_true',help='also restore 503 preloaded GameObject bodies')
    p.add_argument('--experimental-preload-extra-components',action='store_true',help='also restore 31 matching component bodies')
    p.add_argument('--experimental-current-splash',action='store_true',help='rebuild the current Unity splash from the Steam installation')
    p.add_argument('--experimental-material-values',action='store_true',help='restore material values from Steam')
    p.add_argument('--windows-materials',type=Path,help='experimentally source selected material values from Windows')
    p.add_argument('--experimental-shared0-delta',action='store_true',help='rebuild the historical shared0 template from the restored local base')
    p.add_argument('--experimental-shared12-delta',action='store_true',help='rebuild historical shared1 and shared2 templates from the restored local base')
    p.add_argument('--experimental-scene-gameobjects',action='store_true',help='restore scene GameObject values from Steam')
    p.add_argument('--experimental-scene-transforms',action='store_true',help='restore transform value ranges from Steam while preserving Android references')
    p.add_argument('--include-postprocessing-notice',action='store_true',help='add the pinned Unity Post-processing package notice (not a complete notice set)')
    a=p.parse_args(); pc=a.pc_data.resolve(); out=a.output.absolute()
    if a.windows_shared4 and not all((a.experimental_shared4_audio,a.experimental_shared4_meshes,a.experimental_shared4_textures,a.experimental_shared4_particles,a.experimental_shared4_particle_references)):p.error('--windows-shared4 requires all shared4 reconstruction options')
    if a.windows_shared4 and (not a.windows_shared4.is_dir() or out.resolve().is_relative_to(a.windows_shared4.resolve()) or a.windows_shared4.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_shared2 and (not a.windows_shared2.is_dir() or out.resolve().is_relative_to(a.windows_shared2.resolve()) or a.windows_shared2.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_terrain_four and (not a.windows_terrain_four.is_dir() or out.resolve().is_relative_to(a.windows_terrain_four.resolve()) or a.windows_terrain_four.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_core_assets and (not a.windows_core_assets.is_dir() or out.resolve().is_relative_to(a.windows_core_assets.resolve()) or a.windows_core_assets.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_hashed_assets and (not a.windows_hashed_assets.is_dir() or out.resolve().is_relative_to(a.windows_hashed_assets.resolve()) or a.windows_hashed_assets.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_shared0 and (not a.windows_shared0.is_dir() or out.resolve().is_relative_to(a.windows_shared0.resolve()) or a.windows_shared0.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_shared0 and not a.experimental_avoid_legacy_font_patches:p.error('--windows-shared0 requires --experimental-avoid-legacy-font-patches')
    if a.windows_remaining_assets and (not a.windows_remaining_assets.is_dir() or out.resolve().is_relative_to(a.windows_remaining_assets.resolve()) or a.windows_remaining_assets.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_remaining_assets and not all((a.experimental_preload_particles,a.experimental_preload_gameobjects,a.experimental_preload_extra_components,a.experimental_current_splash)):p.error('--windows-remaining-assets requires all preload options and current splash')
    if a.windows_materials and (not a.windows_materials.is_dir() or out.resolve().is_relative_to(a.windows_materials.resolve()) or a.windows_materials.resolve().is_relative_to(out.resolve())):p.error('Windows data must exist and be separate from output')
    if a.windows_materials and not a.experimental_material_values:p.error('--windows-materials requires --experimental-material-values')
    if a.windows_gamepad_animations and not (a.experimental_gamepad_texture and a.experimental_shared2_animation_streams):p.error('--windows-gamepad-animations requires gamepad texture and animation stream options')
    if a.windows_gamepad_animations and not a.windows_gamepad_animations.is_dir():p.error('Windows resource directory is missing')
    if a.windows_gamepad_animations and (out.resolve().is_relative_to(a.windows_gamepad_animations.resolve()) or a.windows_gamepad_animations.resolve().is_relative_to(out.resolve())):p.error('output must be separate from Windows resource data')
    if a.windows_scene_bodies and not a.experimental_scene_bodies:p.error('--windows-scene-bodies requires --experimental-scene-bodies')
    if a.windows_scene_bodies and a.experimental_extra_mono and not a.experimental_scenes_from_base:p.error('--windows-scene-bodies with extra components requires --experimental-scenes-from-base')
    if a.windows_scene_bodies and not a.windows_scene_bodies.is_dir():p.error('Windows scene data directory is missing')
    if a.windows_scene_bodies and (out.resolve().is_relative_to(a.windows_scene_bodies.resolve()) or a.windows_scene_bodies.resolve().is_relative_to(out.resolve())):p.error('output must be separate from Windows scene data')
    if a.experimental_scene_bodies and not a.experimental_hashed:p.error('--experimental-scene-bodies requires --experimental-hashed')
    if a.experimental_extra_mono and not a.experimental_scene_bodies:p.error('--experimental-extra-mono requires --experimental-scene-bodies')
    if a.scrubbed_scene_base and not a.experimental_extra_mono:p.error('--scrubbed-scene-base requires --experimental-extra-mono')
    if a.experimental_pinned_inputs and not a.scrubbed_scene_base:p.error('--experimental-pinned-inputs requires --scrubbed-scene-base')
    if a.experimental_direct_rebuild and not a.experimental_pinned_inputs:p.error('--experimental-direct-rebuild requires --experimental-pinned-inputs')
    if a.experimental_shared0_delta and not (a.experimental_direct_rebuild and a.experimental_material_values):p.error('--experimental-shared0-delta requires direct reconstruction and material restoration')
    if a.experimental_shared12_delta and not (a.experimental_direct_rebuild and a.experimental_material_values):p.error('--experimental-shared12-delta requires direct reconstruction and material restoration')
    if a.experimental_scrubbed_shared and not a.experimental_direct_rebuild:p.error('--experimental-scrubbed-shared requires --experimental-direct-rebuild')
    if a.experimental_scrubbed_hashed and not a.experimental_scrubbed_shared:p.error('--experimental-scrubbed-hashed requires --experimental-scrubbed-shared')
    if a.experimental_expanded_shared and not a.experimental_scrubbed_hashed:p.error('--experimental-expanded-shared requires --experimental-scrubbed-hashed')
    if a.experimental_compact_base and not a.experimental_expanded_shared:p.error('--experimental-compact-base requires --experimental-expanded-shared')
    if a.experimental_shared4_audio and not a.experimental_compact_base:p.error('--experimental-shared4-audio requires --experimental-compact-base')
    if a.experimental_shared4_meshes and not a.experimental_shared4_audio:p.error('--experimental-shared4-meshes requires --experimental-shared4-audio')
    if a.experimental_scenes_from_base and not a.scrubbed_scene_base:p.error('--experimental-scenes-from-base requires --scrubbed-scene-base')
    if a.experimental_hashed_from_base and not a.experimental_scrubbed_hashed:p.error('--experimental-hashed-from-base requires --experimental-scrubbed-hashed')
    if a.experimental_shared4_textures and not a.experimental_shared4_meshes:p.error('--experimental-shared4-textures requires --experimental-shared4-meshes')
    if a.experimental_shared4_particles and not a.experimental_shared4_textures:p.error('--experimental-shared4-particles requires --experimental-shared4-textures')
    if a.experimental_shared4_particle_references and not a.experimental_shared4_particles:p.error('--experimental-shared4-particle-references requires --experimental-shared4-particles')
    if a.experimental_gamepad_texture and not a.experimental_shared4_particle_references:p.error('--experimental-gamepad-texture requires --experimental-shared4-particle-references')
    if a.experimental_shared2_animation_streams and not a.experimental_gamepad_texture:p.error('--experimental-shared2-animation-streams requires --experimental-gamepad-texture')
    if a.experimental_shared0_font_metadata and not a.experimental_shared2_animation_streams:p.error('--experimental-shared0-font-metadata requires --experimental-shared2-animation-streams')
    if a.experimental_font_semantic_references and not a.experimental_shared0_font_metadata:p.error('--experimental-font-semantic-references requires --experimental-shared0-font-metadata')
    if a.experimental_avoid_legacy_font_patches and not a.experimental_font_semantic_references:p.error('--experimental-avoid-legacy-font-patches requires --experimental-font-semantic-references')
    if a.experimental_hybrid_animation_curves and not a.experimental_hashed_from_base:p.error('--experimental-hybrid-animation-curves requires --experimental-hashed-from-base')
    if a.experimental_preload_particles and not (a.experimental_hybrid_animation_curves and a.experimental_avoid_legacy_font_patches and a.experimental_scenes_from_base):p.error('--experimental-preload-particles requires hybrid animation, font and scene options')
    if a.experimental_preload_gameobjects and not a.experimental_preload_particles:p.error('--experimental-preload-gameobjects requires --experimental-preload-particles')
    if a.experimental_preload_extra_components and not a.experimental_preload_gameobjects:p.error('--experimental-preload-extra-components requires --experimental-preload-gameobjects')
    if a.experimental_current_splash and not a.experimental_preload_extra_components:p.error('--experimental-current-splash requires --experimental-preload-extra-components')
    if a.experimental_material_values and not a.experimental_scene_gameobjects:p.error('--experimental-material-values requires scene GameObjects')
    if a.experimental_scene_gameobjects and not a.experimental_scene_transforms:p.error('--experimental-scene-gameobjects requires scene transforms')
    if a.experimental_scene_transforms and not (a.experimental_current_splash and a.experimental_scenes_from_base):p.error('--experimental-scene-transforms requires current splash and scenes from base')
    if a.include_postprocessing_notice:
        for path,expected in ((POSTPROCESSING_PACKAGE/'LICENSE.md','97cfe4fa7afe601adcd526ccd07708a2a435024b818a2e7e7b17a37aabd62f33'),(POSTPROCESSING_PACKAGE/'package.json','d3f2274ea35d1bd43e3f63b628dfe273da9ec4bd3177ce52567b97d315eb2316')):
            if not path.is_file() or sha(path)!=expected:p.error('missing or incompatible post-processing package notice: '+str(path))
    if a.experimental_hybrid_animation_curves and (not HYBRID_ANIMATION_MANIFEST.is_file() or sha(HYBRID_ANIMATION_MANIFEST)!=HYBRID_ANIMATION_SHA):p.error('missing or incompatible hybrid animation manifest')
    if not pc.is_dir(): p.error('Steam data directory is missing')
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be a new directory')
    if out.resolve().is_relative_to(pc) or pc.is_relative_to(out.resolve()): p.error('output must be separate from Steam data')
    for path,expected in PINNED.items():
        if a.experimental_pinned_inputs:continue
        if a.scrubbed_scene_base and path in (NEW_APK,NEW_OBB):continue
        if not path.is_file() or sha(path)!=expected: p.error('missing or incompatible local base: '+str(path))
    package_apk,package_obb=NEW_APK,NEW_OBB
    if a.scrubbed_scene_base:
        base_dir=a.scrubbed_scene_base.resolve()
        if out.resolve().is_relative_to(base_dir) or base_dir.is_relative_to(out.resolve()):p.error('output must be separate from the scrubbed base')
        package_apk=a.scrubbed_scene_base/'base.apk';package_obb=a.scrubbed_scene_base/'base.obb'
        for path,expected in ((package_apk,MATERIAL_BASE_APK if a.experimental_material_values else GAMEOBJECT_BASE_APK if a.experimental_scene_gameobjects else TRANSFORM_BASE_APK if a.experimental_scene_transforms else SPLASH_BASE_APK if a.experimental_current_splash else PRELOAD_EXTRA_APK if a.experimental_preload_extra_components else PRELOAD_OBJECTS_APK if a.experimental_preload_gameobjects else PRELOAD_PARTICLE_APK if a.experimental_preload_particles else FONT_BASE_APK if a.experimental_shared0_font_metadata else ANIMATION_BASE_APK if a.experimental_shared2_animation_streams else GAMEPAD_BASE_APK if a.experimental_gamepad_texture else SHARED4_PARTICLE_REFERENCE_APK if a.experimental_shared4_particle_references else SHARED4_PARTICLE_APK if a.experimental_shared4_particles else SHARED4_TEXTURE_APK if a.experimental_shared4_textures else SHARED4_MESH_APK if a.experimental_shared4_meshes else SHARED4_BASE_APK if a.experimental_shared4_audio else COMPACT_APK if a.experimental_compact_base else EXPANDED_HASHED_APK if a.experimental_expanded_shared else SCRUBBED_HASHED_APK if a.experimental_scrubbed_hashed else SCRUBBED_SHARED_APK if a.experimental_scrubbed_shared else SCRUBBED_SCENE_APK),(package_obb,MATERIAL_BASE_OBB if a.experimental_material_values else GAMEOBJECT_BASE_OBB if a.experimental_scene_gameobjects else TRANSFORM_BASE_OBB if a.experimental_scene_transforms else PRELOAD_EXTRA_OBB if a.experimental_preload_extra_components else PRELOAD_OBJECTS_OBB if a.experimental_preload_gameobjects else PRELOAD_PARTICLE_OBB if a.experimental_preload_particles else ANIMATION_BASE_OBB if a.experimental_shared2_animation_streams else GAMEPAD_BASE_OBB if a.experimental_gamepad_texture else SHARED4_PARTICLE_REFERENCE_OBB if a.experimental_shared4_particle_references else SHARED4_PARTICLE_OBB if a.experimental_shared4_particles else SHARED4_TEXTURE_OBB if a.experimental_shared4_textures else SHARED4_MESH_OBB if a.experimental_shared4_meshes else SHARED4_BASE_OBB if a.experimental_shared4_audio else COMPACT_OBB if a.experimental_compact_base else EXPANDED_HASHED_OBB if a.experimental_expanded_shared else SCRUBBED_HASHED_OBB if a.experimental_scrubbed_hashed else SCRUBBED_SHARED_OBB if a.experimental_scrubbed_shared else SCRUBBED_SCENE_OBB)):
            if not path.is_file() or sha(path)!=expected:p.error('missing or incompatible scrubbed scene base: '+str(path))
        if a.experimental_current_splash:
            manifest=a.scrubbed_scene_base/'splash-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SPLASH_MANIFEST:p.error('missing or incompatible splash manifest')
            splash_pc=a.windows_remaining_assets.resolve() if a.windows_remaining_assets else pc
            splash_hash='9d7146c2d338034b798aec477ce5c3e494a9cc0969861c2706996bf69ae16915' if a.windows_remaining_assets else 'b3f7db2d847edd89f66e387a9bbc926b5c555314a23ca55a0531cf662c89bbe0'
            for name,expected in (('globalgamemanagers.assets',splash_hash),('globalgamemanagers.assets.resS','39dd38fcd8dd95d41d6d55fe16a448d29fe2c31d9a162c825c24c6d99a893d7a')):
                if not (splash_pc/name).is_file() or sha(splash_pc/name)!=expected:p.error('missing or incompatible Steam splash file: '+name)
        if a.experimental_material_values:
            manifest=a.scrubbed_scene_base/'material-manifest.json'
            if not manifest.is_file() or sha(manifest)!=MATERIAL_MANIFEST:p.error('missing or incompatible material manifest')
        if a.experimental_scene_gameobjects:
            manifest=a.scrubbed_scene_base/'gameobject-manifest.json'
            if not manifest.is_file() or sha(manifest)!=GAMEOBJECT_MANIFEST:p.error('missing or incompatible GameObject manifest')
        if a.experimental_scene_transforms:
            manifest=a.scrubbed_scene_base/'transform-manifest.json'
            if not manifest.is_file() or sha(manifest)!=TRANSFORM_MANIFEST:p.error('missing or incompatible transform manifest')
        if a.experimental_preload_particles:
            manifest=a.scrubbed_scene_base/'preload-particle-manifest.json'
            if not manifest.is_file() or sha(manifest)!=(PRELOAD_EXTRA_MANIFEST if a.experimental_preload_extra_components else PRELOAD_OBJECTS_MANIFEST if a.experimental_preload_gameobjects else PRELOAD_PARTICLE_MANIFEST):p.error('missing or incompatible preload particle manifest')
        if a.experimental_scrubbed_shared:
            manifest=a.scrubbed_scene_base/'asset-manifest.json'
            if not manifest.is_file() or sha(manifest)!=(EXPANDED_SHARED_MANIFEST if a.experimental_expanded_shared else SCRUBBED_SHARED_MANIFEST):p.error('missing or incompatible scrubbed asset manifest')
        if a.experimental_scrubbed_hashed:
            manifest=a.scrubbed_scene_base/'hashed-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SCRUBBED_HASHED_MANIFEST:p.error('missing or incompatible scrubbed hashed manifest')
        if a.experimental_shared4_meshes:
            manifest=a.scrubbed_scene_base/'shared4-mesh-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SHARED4_MESH_MANIFEST:p.error('missing or incompatible shared4 mesh manifest')
        if a.experimental_shared4_textures:
            manifest=a.scrubbed_scene_base/'shared4-texture-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SHARED4_TEXTURE_MANIFEST:p.error('missing or incompatible shared4 texture manifest')
        if a.experimental_shared4_particles:
            manifest=a.scrubbed_scene_base/'shared4-particle-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SHARED4_PARTICLE_MANIFEST:p.error('missing or incompatible shared4 particle manifest')
        if a.experimental_shared4_particle_references:
            manifest=a.scrubbed_scene_base/'shared4-particle-reference-manifest.json'
            if not manifest.is_file() or sha(manifest)!=SHARED4_PARTICLE_REFERENCE_MANIFEST:p.error('missing or incompatible shared4 particle reference manifest')
        if a.experimental_gamepad_texture:
            manifest=a.scrubbed_scene_base/'shared2-gamepad-manifest.json'
            if not manifest.is_file() or sha(manifest)!=GAMEPAD_MANIFEST:p.error('missing or incompatible gamepad texture manifest')
        if a.experimental_shared2_animation_streams:
            manifest=a.scrubbed_scene_base/'shared2-animation-manifest.json'
            if not manifest.is_file() or sha(manifest)!=ANIMATION_MANIFEST:p.error('missing or incompatible animation stream manifest')
        if a.experimental_shared0_font_metadata and not a.experimental_font_semantic_references:
            manifest=a.scrubbed_scene_base/'shared0-font-metadata-manifest.json'
            if not manifest.is_file() or sha(manifest)!=FONT_MANIFEST:p.error('missing or incompatible font metadata manifest')
    if PREFIX and not shutil.which(RUNNER): p.error('configured runner missing: '+RUNNER)
    tools=Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT',str(UNITY/'SDK'))).expanduser()/'build-tools/34.0.0'; java=Path(os.environ.get('OPENBONK_JAVA_HOME',str(UNITY/'OpenJDK'))).expanduser(); key=Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE',str(Path.home()/'.android/debug.keystore'))).expanduser()
    for f in (tools/'zipalign',tools/'apksigner',java/'bin/java',key):
        if not f.is_file(): p.error('missing prerequisite: '+str(f))
    env=os.environ.copy();env['JAVA_HOME']=str(java);env['PATH']=str(java/'bin')+os.pathsep+env.get('PATH','')
    with tempfile.TemporaryDirectory(prefix='.openbonk-data-',dir=out.parent) as t:
        t=Path(t); old_output=t/'reconstructed-old'; py=sys.executable
        # UnityPy's bundled FMOD needs the host C++ runtime on this Nix system.
        extra_flags = [*HASHED_FLAGS[:8],'--experimental-export-rebuilt-only'] if a.experimental_direct_rebuild else [*HASHED_FLAGS,'--scrubbed-scene-mono-pair',MANIFEST_ROOT/'shared-scene-mono-scrubbed-base'] if a.experimental_hashed else []
        if a.experimental_shared0_delta:
            shared0_template=t/'shared0-reconstructed-template'
            run([py,ROOT/'analysis/port-inventory/shared0-template-delta.py','restore',package_apk,
                 BUILD/'shared0-template-delta',shared0_template,'--delta-sha256',
                 'ddfddc2154ab570c0e321fc8fb3a23cb11cbc2e6b6b52ce21e6e0e190df909bd'])
            extra_flags += ['--remaining-textures-template',shared0_template]
        if a.experimental_shared12_delta:
            for index,flag,folder,digest in ((1,'--shared1-four-texture-template','shared1-object-delta','5968da095314b7fd11532bf4c1bd2050079b4f894bbede41c2dc2bf06a803d29'),
                                             (2,'--shared2-mesh-template','shared2-mesh-object-delta','70b245cdec76c91b45464037bd18efd43c52d89a4b757350bfc513ec8c445ef1')):
                template=t/f'shared{index}-reconstructed-template'
                run([py,ROOT/'analysis/port-inventory/shared-template-object-delta.py','restore',package_obb,str(index),
                     BUILD/folder,template,'--delta-sha256',digest])
                extra_flags += [flag,template]
        if a.experimental_avoid_legacy_font_patches:extra_flags+=['--experimental-leave-font-metadata-blank']
        if a.experimental_hybrid_animation_curves:extra_flags+=['--hashed-animation-hybrid-manifest',HYBRID_ANIMATION_MANIFEST]
        if a.experimental_hashed_from_base:
            template_root=t/'hashed-input-templates';template_root.mkdir()
            inventory=json.loads((a.scrubbed_scene_base/'hashed-manifest.json').read_text())
            if inventory.get('version')!=1 or len(inventory.get('entries',{}))!=418:raise ValueError('invalid scrubbed hashed inventory')
            with zipfile.ZipFile(package_obb) as archive:
                if len(archive.namelist())!=len(set(archive.namelist())):raise ValueError('duplicate base archive entries')
                for i,(folder,records_key,count) in enumerate(HASHED_CATEGORIES):
                    manifest=HYBRID_ANIMATION_MANIFEST if i==1 and a.experimental_hybrid_animation_curves else MANIFEST_ROOT/folder/'manifest.json'
                    if sha(manifest)!=(HYBRID_ANIMATION_SHA if i==1 and a.experimental_hybrid_animation_curves else HASHED_MANIFEST_HASHES[i]):raise ValueError('hashed manifest mismatch: '+folder)
                    records=json.loads(manifest.read_text())[records_key]
                    if len(records)!=count or len({r['file'] for r in records})!=count:raise ValueError('invalid hashed template inventory')
                    dest=template_root/folder;dest.mkdir();shutil.copyfile(manifest,dest/'manifest.json')
                    for record in records:
                        name=record['file']
                        if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid hashed template name')
                        entry='assets/bin/Data/'+name;rec=inventory['entries'].get(entry)
                        if not rec or rec['kind']!=folder or archive.namelist().count(entry)!=1:raise ValueError('missing hashed template: '+entry)
                        data=archive.read(entry)
                        if hashlib.sha256(data).hexdigest()!=rec['scrubbed_sha256']:raise ValueError('hashed template differs: '+entry)
                        (dest/name).write_bytes(data)
                    extra_flags += [HASHED_TEMPLATE_OPTIONS[i],dest]
        terrain_args=['--windows-terrain-four',a.windows_terrain_four.resolve()] if a.windows_terrain_four else []
        core_args=['--windows-core-assets',a.windows_core_assets.resolve()] if a.windows_core_assets else []
        hashed_args=['--windows-hashed-assets',a.windows_hashed_assets.resolve()] if a.windows_hashed_assets else []
        shared0_args=['--windows-shared0',a.windows_shared0.resolve()] if a.windows_shared0 else []
        remaining_args=['--windows-remaining-assets',a.windows_remaining_assets.resolve()] if a.windows_remaining_assets else []
        shared2_args=['--windows-shared2',a.windows_shared2.resolve()] if a.windows_shared2 else []
        run(PREFIX+[py,ROOT/'tools/build-local-diagnostic.py',pc,old_output,*FLAGS,*extra_flags,*terrain_args,*core_args,*hashed_args,*shared0_args,*remaining_args,*shared2_args])
        converted=old_output/OBB_NAME; old_report=json.loads((old_output/'diagnostic-report.json').read_text())
        if not a.experimental_direct_rebuild and not converted.is_file(): raise ValueError('old reconstructor did not create OBB')
        gamepad_restored=None
        if a.experimental_gamepad_texture:
            gamepad_template=t/'shared2-gamepad-template.assets';gamepad_dir=t/'shared2-gamepad-restored'
            resource_pc=a.windows_gamepad_animations.resolve() if a.windows_gamepad_animations else pc
            resource_args=['--windows-source'] if a.windows_gamepad_animations else []
            with zipfile.ZipFile(package_obb) as z:
                prefix='assets/bin/Data/sharedassets2.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing sharedassets2 fragments')
                gamepad_template.write_bytes(b''.join(z.read(n) for n in parts))
            gamepad_flags=[]
            if a.experimental_shared2_animation_streams:
                animations_dir=t/'shared2-animations-restored'
                run(PREFIX+[py,ROOT/'analysis/data-conversion/shared2-animation-streams-prototype.py','restore',resource_pc,gamepad_template,animations_dir,'--manifest',a.scrubbed_scene_base/'shared2-animation-manifest.json',*resource_args])
                gamepad_template=animations_dir/'sharedassets2.assets';gamepad_flags=['--experimental-animation-streams']
            run(PREFIX+[py,ROOT/'analysis/data-conversion/shared2-gamepad-texture-prototype.py','restore',resource_pc,gamepad_template,gamepad_dir,'--manifest',a.scrubbed_scene_base/'shared2-gamepad-manifest.json',*gamepad_flags,*resource_args])
            gamepad_restored=gamepad_dir/'sharedassets2.assets'
        fonts_restored=None
        if a.experimental_shared0_font_metadata:
            fonts_template=t/'shared0-font-template.assets';fonts_dir=t/'shared0-fonts-restored'
            with zipfile.ZipFile(package_apk) as z:
                prefix='assets/bin/Data/sharedassets0.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing sharedassets0 fragments')
                fonts_template.write_bytes(b''.join(z.read(n) for n in parts))
            if a.experimental_font_semantic_references:
                fonts_restored=fonts_template
            else:
                run([py,ROOT/'analysis/data-conversion/no-rewired-shared0-font-metadata-prototype.py','restore',pc,fonts_template,fonts_dir,'--manifest',a.scrubbed_scene_base/'shared0-font-metadata-manifest.json'])
                fonts_restored=fonts_dir/'sharedassets0.assets'
        materials_restored=None
        if a.experimental_material_values:
            material_template=t/'material-template';material_template.mkdir()
            with zipfile.ZipFile(package_obb) as z:
                prefix='assets/bin/Data/sharedassets1.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing material fragments')
                (material_template/'sharedassets1.assets').write_bytes(b''.join(z.read(n) for n in parts))
            shutil.copyfile(a.scrubbed_scene_base/'material-manifest.json',material_template/'manifest.json')
            material_dir=t/'materials-restored'
            material_pc=a.windows_materials.resolve() if a.windows_materials else pc
            material_source=['--windows-source'] if a.windows_materials else []
            run([py,ROOT/'analysis/data-conversion/shared1-material-values-template.py','restore',material_pc,material_template,material_dir,*material_source])
            materials_restored=material_dir/'sharedassets1.assets'
        paths={}; transplant_counts={}
        for i in range(3):
            name=f'sharedassets{i}.assets'; result=t/name; transplant_result=t/'shared0-with-blank-fonts.assets' if i==0 and a.experimental_font_semantic_references else result; report=t/f'transplant-{i}.json'; source=OLD_APK if i==0 else OLD_OBB; target=materials_restored if i==1 and materials_restored else fonts_restored if i==0 and fonts_restored else gamepad_restored if i==2 and gamepad_restored else package_apk if i==0 else package_obb
            if a.experimental_pinned_inputs:
                cmd=[py,ROOT/'analysis/data-conversion/apply-pinned-converted-objects.py',old_output/name if a.experimental_direct_rebuild else converted,target,name,transplant_result,'--report',report]
                if a.experimental_direct_rebuild:cmd+=['--converted-is-file']
                if a.experimental_scrubbed_shared:cmd+=['--scrubbed-object-manifest',a.scrubbed_scene_base/'asset-manifest.json']
                if i==2 and gamepad_restored:cmd+=['--experimental-gamepad-rebuilt']
                if i==2 and a.experimental_shared2_animation_streams:cmd+=['--experimental-animation-streams']
                if i==1 and materials_restored:cmd+=['--experimental-materials-restored-file']
                if i==0 and fonts_restored:cmd+=['--experimental-fonts-blank-file' if a.experimental_font_semantic_references else '--experimental-fonts-restored-file']
                if i==0 and a.experimental_avoid_legacy_font_patches:cmd+=['--experimental-converted-fonts-blank']
            else:
                cmd=[py,ROOT/'analysis/data-conversion/transplant-matching-assets-prototype.py',source,converted,target,name,result,'--report',report]
                if i==1: cmd+=['--texture-payload-manifest',ROOT/'analysis/data-conversion/no-rewired-shared1-texture-manifest.json']
            run(cmd)
            if i==0 and a.experimental_font_semantic_references:
                semantic=t/'shared0-font-semantic-restored'
                font_pc=a.windows_shared0.resolve() if a.windows_shared0 else pc
                font_source=['--windows-source'] if a.windows_shared0 else []
                run([py,ROOT/'analysis/data-conversion/restore-shared0-font-semantic-references.py',font_pc,package_apk,transplant_result,semantic,*font_source])
                shutil.copyfile(semantic/'sharedassets0.assets',result)
            if sha(result)!=(ANIMATION_FINAL_SHARED2 if i==2 and a.experimental_shared2_animation_streams else GAMEPAD_FINAL_SHARED2 if i==2 and gamepad_restored else EXPECTED[name]): raise ValueError('unexpected reconstructed asset: '+name)
            tr=json.loads(report.read_text());transplant_counts[name]=tr['changed_object_count'] if a.experimental_pinned_inputs else len(tr['changed_objects'])
            paths[name]=result
        audio=t/'audio';audio.mkdir();shutil.copyfile(paths['sharedassets1.assets'],audio/'sharedassets1.assets')
        with contextlib.ExitStack() as stack:
            z=None if a.experimental_direct_rebuild else stack.enter_context(zipfile.ZipFile(converted))
            if z is not None and len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate reconstructed archive entries')
            for name in ('sharedassets1.resource','sharedassets2.resource'):
                if z is None:shutil.copyfile(old_output/name,audio/name)
                else:
                    with z.open('assets/bin/Data/'+name) as src,(audio/name).open('xb') as dst: shutil.copyfileobj(src,dst)
                if sha(audio/name)!=EXPECTED[name]: raise ValueError('unexpected reconstructed resource: '+name)
        hashed_dirs=[];hashed_expected={};hashed_counts={};scrubbed_hashed={}
        if a.experimental_scrubbed_hashed:
            hm=json.loads((a.scrubbed_scene_base/'hashed-manifest.json').read_text())
            if hm.get('version')!=1 or len(hm.get('entries',{}))!=418:raise ValueError('invalid scrubbed hashed inventory')
            scrubbed_hashed=hm['entries']
        if a.experimental_hashed:
            with contextlib.ExitStack() as stack:
                old=None if a.experimental_pinned_inputs else stack.enter_context(zipfile.ZipFile(OLD_OBB))
                new=stack.enter_context(zipfile.ZipFile(package_obb));rebuilt=None if a.experimental_direct_rebuild else stack.enter_context(zipfile.ZipFile(converted))
                for z in (x for x in (old,new,rebuilt) if x is not None):
                    if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate hashed archive entries')
                for i,(folder,records_key,count) in enumerate(HASHED_CATEGORIES):
                    manifest=HYBRID_ANIMATION_MANIFEST if i==1 and a.experimental_hybrid_animation_curves else MANIFEST_ROOT/folder/'manifest.json'
                    if sha(manifest)!=(HYBRID_ANIMATION_SHA if i==1 and a.experimental_hybrid_animation_curves else HASHED_MANIFEST_HASHES[i]):raise ValueError('hashed manifest mismatch: '+folder)
                    records=json.loads(manifest.read_text())[records_key]
                    if len(records)!=count or len({x['file'] for x in records})!=count:raise ValueError('invalid hashed manifest: '+folder)
                    dest=t/f'hashed-{i}';dest.mkdir();hashed_dirs.append(dest)
                    for item in records:
                        name=item['file']
                        if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid hashed resource name')
                        for suffix in (('', '.resource') if records_key=='clips' else ('',)):
                            entry='assets/bin/Data/'+name+suffix
                            archives=tuple(x for x in (old,new,rebuilt) if x is not None)
                            if entry in hashed_expected or any(z.namelist().count(entry)!=1 for z in archives):raise ValueError('missing or duplicate hashed resource: '+entry)
                            new_bytes=new.read(entry);converted_bytes=(old_output/f'hashed-{i}'/(name+suffix)).read_bytes() if rebuilt is None else rebuilt.read(entry)
                            if old is None:
                                original_hash=item['original_resource_sha256'] if records_key=='clips' and suffix else item['asset_template_sha256'] if records_key=='clips' else item['original_sha256']
                                converted_hash=item['resource_sha256'] if records_key=='clips' and suffix else item['asset_sha256'] if records_key=='clips' else item['converted_sha256'] if i==1 and a.experimental_hybrid_animation_curves and item['mode']=='steam-stream' else item['original_sha256'] if i<3 else item['converted_sha256']
                                if a.experimental_scrubbed_hashed:
                                    rec=scrubbed_hashed.get(entry)
                                    if not rec or rec['kind']!=folder or rec['original_sha256']!=original_hash:raise ValueError('scrubbed hashed entry mismatch: '+entry)
                                    target_hash=rec['scrubbed_sha256']
                                else:target_hash=original_hash
                                if hashlib.sha256(new_bytes).hexdigest()!=target_hash or hashlib.sha256(converted_bytes).hexdigest()!=converted_hash:raise ValueError('pinned hashed resource mismatch: '+entry)
                            elif old.read(entry)!=new_bytes:raise ValueError('hashed resource differs in new base: '+entry)
                            hashed_expected[entry]=hashlib.sha256(converted_bytes).hexdigest()
                            (dest/(name+suffix)).write_bytes(converted_bytes)
                    hashed_counts[folder]=count
            if a.experimental_scrubbed_hashed and set(hashed_expected)!=set(scrubbed_hashed):raise ValueError('scrubbed hashed inventory differs')
        scene_dir=None;scene_counts={};extra_mono_counts={}
        if a.experimental_scene_bodies:
            restored=t/'restored-scenes';scene_dir=t/'scene-bodies';scene_dir.mkdir()
            scene_pc=a.windows_scene_bodies.resolve() if a.windows_scene_bodies else pc
            scene_args=['--windows-scenes'] if a.windows_scene_bodies else []
            scene_template=BUILD/'no-rewired-scene-template'
            if a.experimental_extra_mono:
                extra_template=BUILD/'no-rewired-scene-extra-template';intermediate=t/'intermediate-scenes'
                if a.experimental_scenes_from_base:
                    scene_source=t/'base-scenes';scene_source.mkdir()
                    with zipfile.ZipFile(package_obb) as z:
                        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate base archive entries')
                        for scene in SCENE_HASHES:
                            prefix='assets/bin/Data/'+scene+'.split';parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                            if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing base scene fragments')
                            (scene_source/scene).write_bytes(b''.join(z.read(n) for n in parts))
                    if a.experimental_scene_gameobjects:
                        shutil.copyfile(a.scrubbed_scene_base/'gameobject-manifest.json',scene_source/'manifest.json')
                        restored_objects=t/'base-scenes-gameobjects-restored'
                        run([py,ROOT/'analysis/data-conversion/scene-gameobject-values-template.py','restore',scene_pc,scene_source,restored_objects,'--partial-base',*scene_args])
                        scene_source=restored_objects
                    if a.experimental_scene_transforms:
                        shutil.copyfile(a.scrubbed_scene_base/'transform-manifest.json',scene_source/'manifest.json')
                        restored_transforms=t/'base-scenes-transforms-restored'
                        run([py,ROOT/'analysis/data-conversion/scene-transform-values-template.py','restore',scene_pc,scene_source,restored_transforms,'--partial-base',*scene_args])
                        scene_source=restored_transforms
                    run([py,ROOT/'analysis/data-conversion/restore-extra-scene-from-base.py',scene_pc,scene_source,BUILD/'no-rewired-scene-template/manifest.json',extra_template/'extra-manifest.json',intermediate,*scene_args])
                else:
                    run([py,ROOT/'analysis/data-conversion/no-rewired-scene-extra-mono-prototype.py','restore',pc,extra_template,intermediate])
                extra_manifest=json.loads((extra_template/'extra-manifest.json').read_text())
                extra_mono_counts={name:extra_manifest['scenes'][name]['count'] for name in SCENE_HASHES}
                scene_template=intermediate
            run([py,ROOT/'analysis/data-conversion/no-rewired-scene-bodies-prototype.py','restore',scene_pc,scene_template,restored,*scene_args])
            scene_manifest=json.loads((restored/'manifest.json').read_text())
            for name,h in SCENE_HASHES.items():
                if sha(restored/name)!=h:raise ValueError('restored scene differs: '+name)
                scene_counts[name]=scene_manifest['scenes'][name]['counts']
                shutil.copyfile(restored/name,scene_dir/name)
        shared4_dir=None
        if a.experimental_shared4_audio:
            shared4_dir=t/'shared4-audio'
            shared4_pc=a.windows_shared4.resolve() if a.windows_shared4 else pc
            shared4_args=['--windows-source'] if a.windows_shared4 else []
            restored_args=[]
            if a.experimental_shared4_meshes:
                template=t/'shared4-mesh-template.assets';restored_mesh=t/'shared4-mesh-restored'
                with zipfile.ZipFile(package_obb) as z:
                    parts=['assets/bin/Data/sharedassets4.assets.split0','assets/bin/Data/sharedassets4.assets.split1']
                    template.write_bytes(b''.join(z.read(n) for n in parts))
                if a.experimental_shared4_particle_references:
                    particle_references_restored=t/'shared4-particle-references-restored'
                    run([py,ROOT/'analysis/data-conversion/shared4-particle-references-prototype.py','restore',shared4_pc,template,particle_references_restored,'--manifest',a.scrubbed_scene_base/'shared4-particle-reference-manifest.json',*shared4_args])
                    template=particle_references_restored/'sharedassets4.assets'
                if a.experimental_shared4_particles:
                    particle_restored=t/'shared4-particles-restored'
                    run([py,ROOT/'analysis/data-conversion/shared4-particle-bodies-prototype.py','restore',shared4_pc,template,particle_restored,'--manifest',a.scrubbed_scene_base/'shared4-particle-manifest.json',*shared4_args])
                    template=particle_restored/'sharedassets4.assets'
                mesh_flags=[]
                if a.experimental_shared4_textures:
                    texture_restored=t/'shared4-textures-restored'
                    run(PREFIX+[py,ROOT/'analysis/data-conversion/shared4-textures-prototype.py','restore',shared4_pc,template,texture_restored,'--manifest',a.scrubbed_scene_base/'shared4-texture-manifest.json',*shared4_args])
                    template=texture_restored/'sharedassets4.assets';mesh_flags=['--textures-rebuilt']
                run([py,ROOT/'analysis/data-conversion/shared4-mesh-buffers-prototype.py','restore',shared4_pc,template,restored_mesh,'--manifest',a.scrubbed_scene_base/'shared4-mesh-manifest.json',*mesh_flags,*shared4_args])
                restored_args=['--restored-asset',restored_mesh/'sharedassets4.assets']
            run([py,ROOT/'analysis/data-conversion/shared4-audio-retarget-prototype.py',shared4_pc,package_obb,shared4_dir,*restored_args,*shared4_args])
            if sha(shared4_dir/'sharedassets4.assets')!=(SHARED4_TEXTURE_ASSET if a.experimental_shared4_textures else SHARED4_ASSET) or sha(shared4_dir/'sharedassets4.resource')!=SHARED4_AUDIO:raise ValueError('unexpected sharedassets4 audio reconstruction')
        preload_particle_dir=None;preload_particle_expected={}
        if a.experimental_preload_particles:
            preload_template=t/'preload-particle-template';preload_template.mkdir()
            manifest=a.scrubbed_scene_base/'preload-particle-manifest.json'
            inventory=json.loads(manifest.read_text())
            if inventory.get('objects' if a.experimental_preload_gameobjects else 'particles')!=(635 if a.experimental_preload_extra_components else 604 if a.experimental_preload_gameobjects else 101) or len(inventory.get('files',{}))!=(68 if a.experimental_preload_extra_components else 47 if a.experimental_preload_gameobjects else 31):raise ValueError('invalid preload particle inventory')
            shutil.copyfile(manifest,preload_template/'manifest.json')
            with zipfile.ZipFile(package_obb) as z:
                if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate OBB entry')
                for name,rec in inventory['files'].items():
                    if not re.fullmatch(r'[0-9a-f]{32}',name):raise ValueError('invalid preload particle filename')
                    entry='assets/bin/Data/'+name;data=z.read(entry)
                    if hashlib.sha256(data).hexdigest()!=rec['template_sha256']:raise ValueError('preload particle template differs')
                    (preload_template/name).write_bytes(data);preload_particle_expected[entry]=rec['original_sha256']
            preload_particle_dir=t/'preload-particles-restored'
            remaining_pc=a.windows_remaining_assets.resolve() if a.windows_remaining_assets else pc
            remaining_source=['--windows-source'] if a.windows_remaining_assets else []
            run([py,ROOT/'analysis/data-conversion/preload-particle-bodies-prototype.py','restore',remaining_pc,particle_references_restored/'sharedassets4.assets',preload_template,preload_particle_dir,*(['--include-gameobjects'] if a.experimental_preload_gameobjects else []),*(['--include-extra-components'] if a.experimental_preload_extra_components else []),*remaining_source])
        unsigned=t/'unsigned.apk';aligned=t/'aligned.apk'; signed=t/'OpenBonk-diagnostic.apk'; obb=t/OBB_NAME
        run([py,ROOT/'analysis/port-inventory/relocate-split-probe.py',package_apk,package_obb,unsigned,obb,audio,paths['sharedassets0.assets'],paths['sharedassets2.assets'],audio/'sharedassets2.resource',*hashed_dirs,*([scene_dir] if scene_dir else []),*([shared4_dir] if shared4_dir else []),*([preload_particle_dir] if preload_particle_dir else [])])
        align_input=unsigned
        if a.experimental_current_splash:
            splash_template=t/'splash-template';splash_template.mkdir()
            shutil.copyfile(a.scrubbed_scene_base/'splash-manifest.json',splash_template/'manifest.json')
            with zipfile.ZipFile(package_apk) as z:
                prefix='assets/bin/Data/globalgamemanagers.assets.split'
                parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid splash fragments')
                (splash_template/'globalgamemanagers.assets').write_bytes(b''.join(z.read(n) for n in parts))
            restored=t/'splash-restored';patched=t/'unsigned-with-splash.apk'
            remaining_pc=a.windows_remaining_assets.resolve() if a.windows_remaining_assets else pc
            remaining_source=['--windows-source'] if a.windows_remaining_assets else []
            run([py,ROOT/'analysis/data-conversion/current-splash-template.py','restore',splash_template,restored,'--steam-data',remaining_pc,*remaining_source])
            run([py,ROOT/'analysis/data-conversion/patch-current-splash-apk.py',unsigned,restored/'globalgamemanagers.assets',patched])
            align_input=patched
        if a.include_postprocessing_notice:
            align_input=t/'unsigned-with-postprocessing-notice.apk'
            run([py,ROOT/'analysis/port-inventory/attach-postprocessing-notice.py',patched if a.experimental_current_splash else unsigned,POSTPROCESSING_PACKAGE,align_input])
        run(PREFIX+[tools/'zipalign','-p','4',align_input,aligned],env)
        run(PREFIX+[tools/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned],env)
        run(PREFIX+[tools/'zipalign','-c','4',signed],env);run(PREFIX+[tools/'apksigner','verify','--verbose',signed],env)
        with zipfile.ZipFile(signed) as az,zipfile.ZipFile(obb) as oz:
            for z in (az,oz):
                if len(z.namelist())!=len(set(z.namelist())) or z.testzip() is not None: raise ValueError('invalid output archive')
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or sha(obb).encode() not in az.read('classes.dex'): raise ValueError('APK and OBB mismatch')
            if a.experimental_current_splash:
                prefix='assets/bin/Data/globalgamemanagers.assets.split'
                parts=sorted((n for n in az.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))) or hashlib.sha256(b''.join(az.read(n) for n in parts)).hexdigest()!='a520ab88ffb10a0800028c84f18e8fb8b04fedcbe4c6474cd511a7dc3d929168':raise ValueError('packaged splash asset differs')
            if any('rewired' in n.lower() for n in json.loads(az.read('assets/bin/Data/ScriptingAssemblies.json'))['names']): raise ValueError('unexpected Rewired assembly name')
            if a.include_postprocessing_notice:
                notice=az.read('assets/ThirdPartyNotices/Unity-PostProcessing-3.4.0-LICENSE.md')
                if hashlib.sha256(notice).hexdigest()!='97cfe4fa7afe601adcd526ccd07708a2a435024b818a2e7e7b17a37aabd62f33':raise ValueError('post-processing notice differs')
            for name in EXPECTED:
                prefix='assets/bin/Data/'+name
                if name.endswith('.resource'):
                    with oz.open(prefix) as f: h=hashlib.file_digest(f,'sha256').hexdigest()
                else:
                    prefix+='.split'; parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                    if [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))) or not parts: raise ValueError('missing asset fragments')
                    hh=hashlib.sha256()
                    for part in parts:
                        with oz.open(part) as f:
                            for block in iter(lambda:f.read(1048576),b''): hh.update(block)
                    h=hh.hexdigest()
                if h!=(ANIMATION_FINAL_SHARED2 if name=='sharedassets2.assets' and a.experimental_shared2_animation_streams else GAMEPAD_FINAL_SHARED2 if name=='sharedassets2.assets' and gamepad_restored else EXPECTED[name]): raise ValueError('packaged asset differs: '+name)
            for entry,expected in hashed_expected.items():
                with oz.open(entry) as f:h=hashlib.file_digest(f,'sha256').hexdigest()
                if h!=expected:raise ValueError('packaged hashed resource differs: '+entry)
            for entry,expected in preload_particle_expected.items():
                with oz.open(entry) as f:h=hashlib.file_digest(f,'sha256').hexdigest()
                if h!=expected:raise ValueError('packaged preload particle resource differs: '+entry)
            if a.experimental_compact_base:
                audio_entries=[n for n in hashed_expected if n.endswith('.resource')]+['assets/bin/Data/sharedassets1.resource','assets/bin/Data/sharedassets2.resource']
                if shared4_dir:audio_entries.append('assets/bin/Data/sharedassets4.resource')
                if len(audio_entries)!=(66 if shared4_dir else 65) or any(oz.getinfo(n).compress_type!=zipfile.ZIP_STORED for n in audio_entries):raise ValueError('rebuilt audio must be stored without ZIP compression')
            if shared4_dir:
                prefix='assets/bin/Data/sharedassets4.assets.split';parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing sharedassets4 fragments')
                h=hashlib.sha256()
                for part in parts:h.update(oz.read(part))
                if h.hexdigest()!=(SHARED4_TEXTURE_ASSET if a.experimental_shared4_textures else SHARED4_ASSET) or hashlib.sha256(oz.read('assets/bin/Data/sharedassets4.resource')).hexdigest()!=SHARED4_AUDIO:raise ValueError('packaged sharedassets4 audio differs')
            if scene_dir:
                for name,expected in SCENE_HASHES.items():
                    prefix='assets/bin/Data/'+name+'.split';parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                    if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing scene fragments')
                    hh=hashlib.sha256()
                    for part in parts:hh.update(oz.read(part))
                    if hh.hexdigest()!=expected:raise ValueError('packaged scene differs: '+name)
        report={'kind':'local diagnostic only, not for distribution','apk_sha256':sha(signed),'obb_sha256':sha(obb),'assets':{**EXPECTED,**({'sharedassets2.assets':ANIMATION_FINAL_SHARED2 if a.experimental_shared2_animation_streams else GAMEPAD_FINAL_SHARED2} if gamepad_restored else {})},'transplanted_object_counts':transplant_counts,'hashed_category_counts':hashed_counts,'verified_hashed_archive_entries':len(hashed_expected),'scene_body_counts':scene_counts,'extra_scene_component_counts':extra_mono_counts,'scenes_read_from_base':a.experimental_scenes_from_base,'hashed_templates_read_from_base':a.experimental_hashed_from_base,'shared4_audio_clips':6 if shared4_dir else 0,'shared4_meshes':7 if a.experimental_shared4_meshes else 0,'shared4_textures':3 if a.experimental_shared4_textures else 0,'shared4_particles':33 if a.experimental_shared4_particles else 0,'shared4_particle_references':3 if a.experimental_shared4_particle_references else 0,'gamepad_texture_rebuilt':bool(gamepad_restored),'shared2_animation_streams':2 if a.experimental_shared2_animation_streams else 0,'shared0_font_metadata':6 if fonts_restored else 0,'font_semantic_references':a.experimental_font_semantic_references,'legacy_font_patches_avoided':a.experimental_avoid_legacy_font_patches,'hybrid_animation_steam_streams':25 if a.experimental_hybrid_animation_curves else 0,'scene_base':'selected bodies zeroed' if a.scrubbed_scene_base else 'original bodies present','shared_base':'expanded selected objects and audio zeroed' if a.experimental_expanded_shared else 'selected objects and audio zeroed' if a.experimental_scrubbed_shared else 'original objects present','hashed_base':'selected payloads and audio zeroed' if a.experimental_scrubbed_hashed else 'original payloads present','zero_audio_compacted':a.experimental_compact_base,'object_comparison':'pinned hashes' if a.experimental_pinned_inputs else 'old full base','intermediate_mode':'direct files' if a.experimental_direct_rebuild else 'old APK/OBB','source_data':str(pc),'upstream_intermediate_not_final_package':old_report,'limitations':['Retains original game and third-party material; not redistributable.','Experimental DEX patch and local debug signature.','Other new-base assets and scenes remain; only the categories listed in this report were rebuilt.','Only one Steam build and pinned local templates are supported.','No physical-device or full-game validation.']}
        report['preload_particle_bodies']=101 if preload_particle_dir else 0
        report['preload_gameobject_bodies']=503 if a.experimental_preload_gameobjects else 0
        report['preload_extra_component_bodies']=31 if a.experimental_preload_extra_components else 0
        report['reconstructed_unity_splash']=a.experimental_current_splash
        report['scene_transform_value_objects']=7301 if a.experimental_scene_transforms else 0
        if a.experimental_current_splash:report['limitations'].append('Unity splash conversion is lossy; runtime display and rights remain unverified.')
        report['postprocessing_package_notice']=a.include_postprocessing_notice
        report['preload_particle_files']=len(preload_particle_expected)
        (t/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        out.mkdir()
        try:
            for f in (signed,obb,t/'report.json'): shutil.copyfile(f,out/f.name)
        except BaseException:
            shutil.rmtree(out);raise
        print('Local diagnostic created:',out,'APK SHA-256',report['apk_sha256'],flush=True)


if __name__=='__main__':main()
