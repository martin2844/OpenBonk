#!/usr/bin/env python3
"""Rebase the Steam-rebuilt splash diagnostic onto the pinned current runtime."""
import importlib.util
from pathlib import Path

spec=importlib.util.spec_from_file_location('runtime_rebase',Path(__file__).with_name('rebase-current-runtime-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.REBUILT_APK='19cc08f1917c6bd80b8399b2ce5d2a403c537e5594aecfc123a0bc319e590311'
base.enable_local_obb_inventories()
base.enable_local_apk_inventories()

if __name__=='__main__':base.main()
