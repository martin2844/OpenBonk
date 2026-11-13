#!/usr/bin/env python3
"""Restore the Steam buffering clip after the launcher-hint rebase."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('video_restore',Path(__file__).with_name('restore-buffering-video-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.APK_SHA='47f52fe8858f9bb7571a2954093f21683e8e2b81221bbc6f6bf596c0c0c0b3e3'
base.OBB_SHA='1438b991b3ef92dada678e301f0ae8331a8ef5ffc8f3ff98f43fdbc8f29ee0f4'
if __name__=='__main__':base.main()
