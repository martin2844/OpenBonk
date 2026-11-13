#!/usr/bin/env python3
"""Rebase the cleanup launcher onto the pinned rebuilt-splash diagnostic."""
import importlib.util
from pathlib import Path

spec=importlib.util.spec_from_file_location('cleanup_rebase',Path(__file__).with_name('rebase-cleanup-launcher-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.PINS['target_apk']='c0ced67feb8e0226e22be65f76445dfe9f63996308acc3a5ed2b967dcf155a7d'

if __name__=='__main__':base.main()
