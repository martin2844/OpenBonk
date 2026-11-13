#!/usr/bin/env python3
"""Rebase the current local launcher onto Steam-rebuilt diagnostic data."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('latest_rebase',Path(__file__).with_name('rebase-latest-runtime-prototype.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);base=module.base
base.NEW_APK='cc55e70452db90269632757e013021098bf16d7328534f0b43bd9c65a28eacc4'
base.NEW_OBB='834af7530f4d33822fb2707bffd517777205b2449c6f19f4dbdf56dd98dd8f18'
root=Path(__file__).resolve().parents[2]/'build/android/launcher-hint-inventories'
base.APK_INVENTORIES[base.NEW_APK]=(root/'apk.json','bf78c162e4c24cdd5156a6a2511625d10879aa551db26f86ea774478543b1cc2',root/'payload.zip','bc1906048405a08b40c8f0217a57369b429860c8ca594b03d80da635827b1883')
base.OBB_INVENTORIES[base.NEW_OBB]=(root/'obb.json','48790b0c005f145e05eb6c08035efaa2ca2f336abe1bf3e02707ee0f1e3b9324')
if __name__=='__main__':base.main()
