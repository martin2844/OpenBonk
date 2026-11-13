#!/usr/bin/env python3
"""Rebase the current local launcher onto Steam-rebuilt diagnostic data."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('latest_rebase',Path(__file__).with_name('rebase-latest-runtime-prototype.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);base=module.base
base.NEW_APK='806f5778f1cac43f4ff588b893db28fbca45f004a21dc4833c80c191dee45775'
base.NEW_OBB='1653cdadcdbdd03d95230a1616422b48ecc33b3fa6f1820c92c721ab7a250223'
root=Path(__file__).resolve().parents[2]/'build/android/launcher-busy-inventories'
base.APK_INVENTORIES[base.NEW_APK]=(root/'apk.json','82c461b1a633db30dce1309f9166ac79178b626a6630d24763108d298a750e8b',root/'payload.zip','37233bf7549e110dab35fdf3a4bf2cf9442ac44431b02198dcbfc64e8cff1980')
base.OBB_INVENTORIES[base.NEW_OBB]=(root/'obb.json','4a1f6daf317dbdff76efeb52c25dfe2e109bed4c3e54bbc2096e5c25de8de026')
if __name__=='__main__':base.main()
