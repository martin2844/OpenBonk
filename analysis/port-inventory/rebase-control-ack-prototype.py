#!/usr/bin/env python3
"""Apply one tested control acknowledgement to a pinned private diagnostic."""
import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location('rebase', Path(__file__).with_name('rebase-transaction-launcher-prototype.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)
module.PINS = {
    'old_apk': '021d126372a22a1266cdf191ca3ca58842f96ea33d4c1149d8f068153fa1e4d6',
    'source_obb': '04540908f4b39d5af0c6a98ad95676da6b805c23e107f4afef3af33df35bc5be',
    'new_apk': 'ece0e3adf352a797340d22070c566e6d97aef9d951310285bed8c0ed0af1c16a',
    'new_source_obb': 'b1c89892dd3cdbb0e3c1f281268ecacc014b2fdaac223edd86d254073d7b63cb',
    'target_apk': 'cc773e27264322b617aa3032c8bb8d6aece8fb8c33c233f31eb281d0ebd10179',
    'target_obb': '00a50941a48f3263569b0f2ed4aad64d57c9187165ba545e5dd6aa944147e2cf',
}
module.EXTRA_REPLACEMENTS = {'lib/arm64-v8a/libil2cpp.so', 'assets/bin/Data/Managed/Metadata/global-metadata.dat'}
module.ALLOWED = {'assets/bin/Data/boot.config', 'assets/bin/Data/unity_app_guid', 'assets/dexopt/baseline.prof',
                  'assets/unity_obb_guid', 'classes.dex', 'lib/arm64-v8a/libswappywrapper.so'} | module.EXTRA_REPLACEMENTS

if __name__ == '__main__': module.main()
