#!/usr/bin/env python3
"""Rebase one Windows-aware launcher onto the private diagnostic build."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': '5401d345db80bbeee0f917712892f7e383dc16b606275cbb0cfcec950d020397',
    'source_obb': 'ff0099a62983e81c1124cf1c6e1c520cdb9255b1c20c1ed01ab3ab776670bec5',
    'new_apk': 'c82f953f18a732d154fd386867841e5db0291b4ed245c921855a1e0396887c5d',
    'new_source_obb': 'e591409aa0656f667c30009abe193f4eeac87d244e64ef0d450c8d807669d8a2',
    'target_apk': '6b30f89c4888b4dfb7e1ea4605d101aeb4303fde401561d1e6b72688869e2bb9',
    'target_obb': 'ff37e1c7d2408e0eb25babaec6805c160c8f6419c85fe87c4be0f9a9765a32b9',
}
module.EXTRA_REPLACEMENTS = {'lib/arm64-v8a/libil2cpp.so', 'assets/bin/Data/Managed/Metadata/global-metadata.dat'}
module.TARGET_APK_CONTENT = 'e6fdf30376940ca26dad63b6a9384f734a288920446d366379613e435ad5427c'
module.ALLOWED = {'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
                  'assets/unity_obb_guid', 'classes.dex', 'lib/arm64-v8a/libswappywrapper.so'} | module.EXTRA_REPLACEMENTS
if __name__ == '__main__': module.main()
