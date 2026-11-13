#!/usr/bin/env python3
"""Restore the buffering clip in the pinned rebuilt-splash diagnostic."""
import importlib.util
from pathlib import Path

spec=importlib.util.spec_from_file_location('video_restore',Path(__file__).with_name('restore-buffering-video-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.APK_SHA='f186ea64d7fddf239dd39ac446b4acb91e55939314ea36fc7223b7fb566e5df5'

if __name__=='__main__':base.main()
