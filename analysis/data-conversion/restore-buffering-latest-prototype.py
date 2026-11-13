#!/usr/bin/env python3
"""Restore Steam buffering clip after the direct latest runtime rebase."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('video_restore',Path(__file__).with_name('restore-buffering-video-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.APK_SHA='4f267479bb43e12ccdef5bad9a9575fd1ad83c97e3350a23d769a8be71237e35'
base.OBB_SHA='2b17afa08b14122cfe0fd9a5b4114ce931d6977f2f05759b2ad4de5cb11294dd'
if __name__=='__main__':base.main()
