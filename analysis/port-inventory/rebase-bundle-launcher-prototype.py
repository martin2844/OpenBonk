#!/usr/bin/env python3
"""Rebase the tested bundle launcher onto pinned rebuilt diagnostic data.

Local research only. This modifies DEX, signs with a debug key and retains
game and third-party content; it is not a distributable release.
"""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('runtime_rebase', Path(__file__).with_name('rebase-current-runtime-prototype.py'))
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
base.OLD_APK = '4127d391f7dd76b67ccac51318ee7facbf16fd128350290048cb815bf31f6605'
base.OLD_OBB = 'd794c498d2631f32307c1b0c97b9af77906991f42e07fb33ec3a0baf3ca9a5f5'
base.NEW_APK = 'd130551c64c64a3980c86aa85260a0a41ac016de1b0b7196a35af82e21d4e6ff'
base.NEW_OBB = 'a304957b263d31e5e9ae03f7ef46c705807da442a9863f2bbe3984fdfc586cbb'
base.REBUILT_APK = '1a71ecd1b30ab34db633ab78a23856fa99bc43bca159a2efc4b47bb6b9437950'
base.REBUILT_OBB = 'd47ca939431bfe4555c195155f29ea66303f24b848b2c90a8822d91d305c84ca'
base.REPLACEMENTS = {'assets/bin/Data/Managed/Metadata/global-metadata.dat',
                     'assets/bin/Data/RuntimeInitializeOnLoads.json',
                     'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid',
                     'assets/dexopt/baseline.prof', 'assets/dexopt/baseline.profm',
                     'classes.dex', 'lib/arm64-v8a/libil2cpp.so',
                     'lib/arm64-v8a/libswappywrapper.so'}

if __name__ == '__main__':
    base.main()
