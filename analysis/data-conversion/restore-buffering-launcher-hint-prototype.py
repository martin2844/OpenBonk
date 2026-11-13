#!/usr/bin/env python3
"""Restore the Steam buffering clip after the launcher-hint rebase."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('video_restore',Path(__file__).with_name('restore-buffering-video-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.APK_SHA='ee052a8786d2ef1f9b4fa2e097e84cb9632107b6d5cd467fd045cf81298a73a0'
base.OBB_SHA='88a4519f3b192118229f22bd6026f2047d3d9e993802af66732a0794a8d6803e'
if __name__=='__main__':base.main()
