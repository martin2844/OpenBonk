#!/usr/bin/env python3
"""Confirm email patterns are counted without leaking them into the report."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import zipfile

root = Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory() as folder:
    tmp = Path(folder); apk = tmp/'fixture.apk'; report = tmp/'report.json'
    secret = 'sample-person@example.invalid'
    with zipfile.ZipFile(apk, 'w') as z:
        z.writestr('synthetic.bin', 'contact '+secret)
        z.writestr('encoded.bin', secret.encode('utf-16-le'))
    result = subprocess.run([sys.executable, str(root/'tools/audit-apk-email-markers.py'),str(apk),str(report)],
                            text=True, capture_output=True, check=True)
    data = json.loads(report.read_text())
    assert len(data['entries_with_matches']) == 1
    assert data['entries_with_matches'][0]['matches'] == 1
    assert data['entries_with_matches'][0]['unmatched'] == 1
    assert secret not in report.read_text() and secret not in result.stdout
print('Email marker privacy fixture: PASS')
