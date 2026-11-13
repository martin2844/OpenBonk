#!/usr/bin/env python3
"""Rebase the current local launcher onto Steam-rebuilt diagnostic data."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('latest_rebase',Path(__file__).with_name('rebase-latest-runtime-prototype.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);base=module.base
base.NEW_APK='c061669f391230774908dd008658d12ece2ab95b9c5a31c7ab21c35fc9a38714'
base.NEW_OBB='a7e98aa97ac67547225d9f01853240c36f2614c16a967a0132422a8ce2323d1e'
base.REBUILT_CONTENT_SHA='76f2a6e2c86354d96054a9602064dd9db99c378a572ea38026bc698e88a07132'
root=Path(__file__).resolve().parents[2]/'build/android/launcher-recreation-inventories'
base.APK_INVENTORIES[base.NEW_APK]=(root/'apk.json','ab75585afb1791c28b892f04afd4cdc43bc35524f99ff903b170bf90c18acb4d',root/'payload.zip','ccb77f3b9532331715f88d13183c43c078cf90cfa4fa41dc4732b0209d4b5a90')
base.OBB_INVENTORIES[base.NEW_OBB]=(root/'obb.json','db0a434a41c8f2669b2bc47afae232b7c120250139d4358271eba95b0735b41d')
if __name__=='__main__':base.main()
