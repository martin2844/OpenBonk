#!/usr/bin/env python3
"""Rebase one Windows-aware launcher onto the private diagnostic build."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': 'c82f953f18a732d154fd386867841e5db0291b4ed245c921855a1e0396887c5d',
    'source_obb': 'e591409aa0656f667c30009abe193f4eeac87d244e64ef0d450c8d807669d8a2',
    'new_apk': '6e13928d02024f5acb710fb794f032b0d5ff04dfdd75613ca73781d99164be0d',
    'new_source_obb': 'abf96cd85d04889f3ecee7cdf5adf567c3f3a2eef46ca8c0adee3f94de61d331',
    'target_apk': '115b14410cb632c9c1f0968cc395783ae54d6e56e49adf2d40f61bca70ecae23',
    'target_obb': 'e13453698c86d8082a7c4183a905a9f189cf9d660257b85a062f160044538378',
}
module.EXTRA_REPLACEMENTS = {'lib/arm64-v8a/libil2cpp.so', 'assets/bin/Data/Managed/Metadata/global-metadata.dat'}
module.TARGET_APK_CONTENT = '557d14a4632e6c08004ebad37204b804b17cacbd855fb6c0e579385044c3fe38'
module.ALLOWED = {'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
                  'assets/unity_obb_guid', 'classes.dex', 'lib/arm64-v8a/libswappywrapper.so'} | module.EXTRA_REPLACEMENTS
if __name__ == '__main__': module.main()
