#!/usr/bin/env python3
"""Restore the Steam buffering clip after the launcher-hint rebase."""
import importlib.util
from pathlib import Path
spec=importlib.util.spec_from_file_location('video_restore',Path(__file__).with_name('restore-buffering-video-prototype.py'))
base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)
base.APK_SHA='2dedbcfc87f42e48442dc1994990a17e5e0c2de349443878c09fdd8e58a4c931'
base.APK_CONTENT_SHA='18774838dbcd9f0a9523462ebf2934978dd37f72c003a0613c8983bf023432f8'
base.OBB_SHA='a72a758d8e47b7103ffda6870b5ded543d645d0ef2b9708939af90114b031991'
if __name__=='__main__':base.main()
