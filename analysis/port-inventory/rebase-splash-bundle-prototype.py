#!/usr/bin/env python3
"""Rebase the bundle launcher onto the pinned rebuilt-splash diagnostic."""
import importlib.util
from pathlib import Path

spec=importlib.util.spec_from_file_location('bundle_rebase',Path(__file__).with_name('rebase-bundle-launcher-prototype.py'))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
module.base.REBUILT_APK='8f1f23623b373f5902567f0023f2baa8d28e43fe7c52015a7a14cab205d365f8'

if __name__=='__main__':module.base.main()
