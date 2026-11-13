#!/usr/bin/env python3
"""Exercise path marker detection without exposing actual user information."""
import getpass
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

root = Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory() as folder:
    tmp = Path(folder)
    archive = tmp / 'fixture.apk'
    marker = '/home/' + getpass.getuser() + '/'
    with zipfile.ZipFile(archive, 'w') as z:
        z.writestr('a', marker.encode() + b'\x00' + ('/Users/exampleperson/').encode('utf-16-le'))
        z.writestr('b', b'X' + 'C:\\Users\\exampleperson\\'.encode('utf-16-be'))
        z.writestr('c', b''.join(bytes((x, 0x42)) for x in b'/home/notreally/'))
        z.writestr('d', b'C:/Users/secondperson/')
    report = tmp / 'result.json'
    output = subprocess.run([sys.executable, str(root/'tools/audit-apk-path-markers.py'), str(archive), str(report)],
                            text=True, capture_output=True, check=True).stdout
    data = json.loads(report.read_text())
    counts = {}
    for r in data['markers']:
        key = (r['kind'], r['subject'])
        counts[key] = counts.get(key, 0) + r['occurrences']
    assert counts == {('unix_home_user','local_username'): 1,
                      ('mac_users_user','other_or_placeholder'): 1,
                      ('windows_users_user','other_or_placeholder'): 2}, counts
    assert not data['incomplete'] and data['entries_scanned'] == 4
    assert getpass.getuser() not in output and getpass.getuser() not in report.read_text()
print('Path marker fixture: PASS')
