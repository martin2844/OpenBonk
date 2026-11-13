#!/usr/bin/env python3
"""Transplant the tested control-choice launcher to a pinned private build.

Experimental diagnostic only; all game and third-party material remains.
The input and output directory layout matches rebase-transaction-launcher.
"""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': 'c061669f391230774908dd008658d12ece2ab95b9c5a31c7ab21c35fc9a38714',
    'source_obb': 'a7e98aa97ac67547225d9f01853240c36f2614c16a967a0132422a8ce2323d1e',
    'new_apk': '021d126372a22a1266cdf191ca3ca58842f96ea33d4c1149d8f068153fa1e4d6',
    'new_source_obb': '04540908f4b39d5af0c6a98ad95676da6b805c23e107f4afef3af33df35bc5be',
    'target_apk': '62d6392827c642eda3ef0355c3ef596a5d2f36c096391a0d3c26786ad535b719',
    'target_obb': 'a7c5a320c7de060ca99908c0d69bb5a78e4fb1bd0c98a461f6c5c2c5e8bf111b',
}
module.ALLOWED = module.ALLOWED - {'lib/arm64-v8a/libgame.so'}

if __name__ == '__main__':
    module.main()
