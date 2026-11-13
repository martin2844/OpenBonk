#!/usr/bin/env python3
"""Transplant only the tested launcher DEX onto the local diagnostic game."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': '6e13928d02024f5acb710fb794f032b0d5ff04dfdd75613ca73781d99164be0d',
    'source_obb': 'abf96cd85d04889f3ecee7cdf5adf567c3f3a2eef46ca8c0adee3f94de61d331',
    'new_apk': 'c3c84c511f22deb3868853d6e6ed3deb699f7a49212ab827e7e5cbad53f7e27a',
    'new_source_obb': 'e720857d0c69b083130c49fb4f84ce9efdd3f98c81d562203905f5db55032c2c',
    'target_apk': '02010e76d39d94dda6ae8deba2f1c3df38028077fb04d6962d94558b447bc71a',
    'target_obb': 'abea33cdc20042f03f434bd1eb554cd095e7a5f2e8162fa9bc8038cbdf539fb0',
}
module.TARGET_APK_CONTENT = '92f2c58d6d8eb82b5b32114ba84cb191ecabee03d922c321869843a2b91a251a'
# Other differences are from the ongoing Lua experiment and Unity's generated
# build IDs. They are checked here but deliberately not transplanted.
module.ALLOWED = {
    'assets/bin/Data/Managed/Metadata/global-metadata.dat', 'assets/bin/Data/RuntimeInitializeOnLoads.json',
    'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
    'assets/dexopt/baseline.profm', 'assets/unity_obb_guid', 'classes.dex',
    'lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libmegabonk_lua.so', 'lib/arm64-v8a/libswappywrapper.so',
}
module.EXTRA_REPLACEMENTS = set()
if __name__ == '__main__': module.main()
