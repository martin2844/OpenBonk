#!/usr/bin/env python3
"""Combine the current runtime directly with the pinned video-restored data.

Research only: game and third-party material remain; not for distribution.
"""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('runtime_rebase',Path(__file__).with_name('rebase-current-runtime-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.OLD_APK='4127d391f7dd76b67ccac51318ee7facbf16fd128350290048cb815bf31f6605'
base.OLD_OBB='d794c498d2631f32307c1b0c97b9af77906991f42e07fb33ec3a0baf3ca9a5f5'
base.NEW_APK='f337676127872d1bb6dcfabb2e9e465709c691087378685b8fb207b9a0e02486'
base.NEW_OBB='3a6ce0ac78310852c1a2252fe65c267a9812b9b900e94c3e9062472a4ada2d58'
base.REBUILT_APK='8f1f23623b373f5902567f0023f2baa8d28e43fe7c52015a7a14cab205d365f8'
base.REBUILT_OBB='d47ca939431bfe4555c195155f29ea66303f24b848b2c90a8822d91d305c84ca'
base.REPLACEMENTS={'assets/bin/Data/Managed/Metadata/global-metadata.dat',
                   'assets/bin/Data/RuntimeInitializeOnLoads.json', 'assets/bin/Data/boot.config',
                   'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
                   'assets/dexopt/baseline.profm', 'classes.dex', 'lib/arm64-v8a/libil2cpp.so',
                   'lib/arm64-v8a/libswappywrapper.so'}
base.OVERRIDE_PATH=Path(__file__).resolve().parents[2]/'build/android/local-runtime-compatibility.zip'
base.OVERRIDE_SHA='3d9813b602fe57056bccf37fc223ade9c5c9d6a9cca04804f4ec4287307d1e6e'
base.OVERRIDE_HASHES={
    'assets/bin/Data/boot.config':'a763a7d051c098aae40aa4e0a4a2394db4e7ad2435b97b95ac0009cb9f993509',
    'assets/bin/Data/unity_app_guid':'8211495a2635c338ba72207f2c3fb242a0d81583f5230860d896860dffc3986b',
    'assets/dexopt/baseline.prof':'c3e2d99bd84bb7ea57ea6af30f5b40caf557ba0ca3b46d75582cdbfbd7159e15',
    'lib/arm64-v8a/libswappywrapper.so':'3ad4e9a8049fbebf26cc7e5263bb92462ffdfacd4d5262283351c6538085d68a'}
base.enable_local_obb_inventories()
base.enable_local_apk_inventories()
if __name__=='__main__':base.main()
