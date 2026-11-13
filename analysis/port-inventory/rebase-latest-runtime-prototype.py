#!/usr/bin/env python3
"""Directly combine the latest runtime with locally rebuilt Steam data.

Experimental local diagnostic; the inputs and output are not distributable.
"""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('direct_rebase',Path(__file__).with_name('rebase-direct-transaction-prototype.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);base=module.base
base.OLD_APK='b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38'
base.OLD_OBB='ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
base.REBUILT_APK='19cc08f1917c6bd80b8399b2ce5d2a403c537e5594aecfc123a0bc319e590311'
base.REBUILT_OBB='ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2'
base.REPLACEMENTS=base.REPLACEMENTS|{'lib/arm64-v8a/libmegabonk_lua.so'}
root=Path(__file__).resolve().parents[2]/'build/android/local-split-apk-inventories'
base.APK_INVENTORIES[base.NEW_APK]=(root/'latest-direct.json','6678b672097b0c39bf24746df227d4d5cdcc8ad15f82b2a5aba1e3174c650458',root/'latest-direct.zip','d21a029f1ac6e17198d835e0070683a4fdff29da69fd995ead67d9a5ca25c315')
if __name__=='__main__':base.main()
