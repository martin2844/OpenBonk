#!/usr/bin/env python3
"""Check that the APK scanner finds text across encodings and block boundaries."""
import getpass
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
SCANNER = ROOT / 'tools/audit-apk-local-data.py'


def main():
    user = getpass.getuser()
    if len(user.encode()) < 5:
        raise ValueError('fixture needs a username accepted by the scanner')
    home = str(Path.home()) + '/'
    with tempfile.TemporaryDirectory(prefix='openbonk-privacy-test-') as temp:
        root = Path(temp)
        apk, report = root / 'fixture.apk', root / 'report.json'
        # Force a UTF-16 match to straddle the scanner's 1 MiB block boundary.
        marker = home.encode('utf-16-le')
        with zipfile.ZipFile(apk, 'w', zipfile.ZIP_DEFLATED) as z:
            z.writestr('boundary.bin', b'x' * (1024 * 1024 - 3) + marker)
            z.writestr('username.bin', user.encode('utf-16-be'))
            z.writestr('unrelated.bin', b'no selected local data')
        result = subprocess.run([sys.executable, str(SCANNER), str(apk), str(report),
                                 '--search-local-username'], capture_output=True, text=True, check=True)
        data = json.loads(report.read_text())
        matches = {(entry['entry'], entry['kind']) for entry in data['matches']}
        assert ('fixture.apk!/boundary.bin', 'local_home_path_utf_16_le') in matches
        assert ('fixture.apk!/username.bin', 'local_username_utf_16_be') in matches
        assert not data['incomplete']
        assert home not in result.stdout and user not in result.stdout
    print('UTF-16 and block-boundary checks passed.')


if __name__ == '__main__':
    main()
