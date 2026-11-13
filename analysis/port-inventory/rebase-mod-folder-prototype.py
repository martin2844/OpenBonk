#!/usr/bin/env python3
"""Apply the tested mod-folder launcher to one pinned private diagnostic."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': 'ece0e3adf352a797340d22070c566e6d97aef9d951310285bed8c0ed0af1c16a',
    'source_obb': 'b1c89892dd3cdbb0e3c1f281268ecacc014b2fdaac223edd86d254073d7b63cb',
    'new_apk': '5401d345db80bbeee0f917712892f7e383dc16b606275cbb0cfcec950d020397',
    'new_source_obb': 'ff0099a62983e81c1124cf1c6e1c520cdb9255b1c20c1ed01ab3ab776670bec5',
    'target_apk': 'de5c9f46da91230e8eb9d2395be4ccaa4829c945f68c3907e96e648206ed0704',
    'target_obb': 'b0588720eec5ff380b29e2a91baa63aa223a1d62eca942d122a24be72a59a0db',
}
module.ALLOWED = {'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
                  'assets/unity_obb_guid', 'classes.dex', 'lib/arm64-v8a/libswappywrapper.so'}

if __name__ == '__main__': module.main()
