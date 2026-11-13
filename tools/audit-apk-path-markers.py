#!/usr/bin/env python3
"""Count build-path markers in outer APK entries without recording path values.

Heuristic, bounded inspection, not a complete privacy audit. Compressed nested
archives and transformed or split strings are not inspected by this script.
"""
import argparse
from collections import Counter
import getpass
import hashlib
import json
from pathlib import Path
import re
import zipfile

MAX_ENTRY = 256 * 1024 * 1024
MAX_TOTAL = 1024 * 1024 * 1024
MAX_ENTRIES = 3000
MARKERS = {
    'unix_home_user': re.compile(rb'/home/([A-Za-z0-9_.-]{2,64})/'),
    'mac_users_user': re.compile(rb'(?<![A-Za-z]:)/Users/([A-Za-z0-9_.-]{2,64})/'),
    'windows_users_user': re.compile(rb'(?i)[a-z]:[/\\]Users[/\\]([A-Za-z0-9_.-]{2,64})[/\\]'),
}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path)
    p.add_argument('report', type=Path)
    a = p.parse_args()
    if not a.apk.is_file() or a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():
        p.error('APK must exist and report must be a new file')
    username = getpass.getuser().encode().lower()
    result = {'apk_sha256': hashlib.file_digest(a.apk.open('rb'), 'sha256').hexdigest(),
              'entries_scanned': 0, 'bytes_scanned': 0, 'markers': [], 'incomplete': [],
              'scope': 'Outer APK files only; ASCII and UTF-16 markers. No nested decompression, path values, or completeness claim.'}
    with zipfile.ZipFile(a.apk) as archive:
        entries = archive.infolist()
        if len(entries) > MAX_ENTRIES or len(entries) != len({entry.filename for entry in entries}):
            raise ValueError('unexpected APK inventory')
        for entry in entries:
            if entry.is_dir(): continue
            if entry.file_size > MAX_ENTRY or result['bytes_scanned'] + entry.file_size > MAX_TOTAL:
                result['incomplete'].append({'entry': '[size-limited]', 'reason': 'size limit'})
                continue
            try:
                data = archive.read(entry)
            except (OSError, RuntimeError, zipfile.BadZipFile) as exc:
                result['incomplete'].append({'entry': '[unreadable]', 'reason': type(exc).__name__})
                continue
            result['entries_scanned'] += 1
            result['bytes_scanned'] += len(data)
            hits = {}
            # Replace embedded UTF-16 markers with ASCII for the same path grammar.
            for encoding in ('ascii', 'utf16le', 'utf16le_odd', 'utf16be', 'utf16be_odd'):
                # UTF-16 channels are masked by requiring zero high bytes, rather
                # than interpreting arbitrary alternating binary bytes as text.
                sample = data
                if encoding != 'ascii':
                    offset = 1 if encoding.endswith('_odd') else 0
                    pairs = data[offset:]
                    if encoding.startswith('utf16le'):
                        sample = bytes(pairs[i] if pairs[i + 1] == 0 else 0 for i in range(0, len(pairs) - 1, 2))
                    else:
                        sample = bytes(pairs[i + 1] if pairs[i] == 0 else 0 for i in range(0, len(pairs) - 1, 2))
                for kind, pattern in MARKERS.items():
                    for match in pattern.finditer(sample):
                        subject = 'local_username' if match.group(1).lower() == username else 'other_or_placeholder'
                        if encoding == 'ascii':
                            position = match.start()
                        else:
                            position = offset + 2 * match.start() + (1 if encoding.startswith('utf16be') else 0)
                        hits.setdefault((kind, subject), set()).add(position)
            for (kind, subject), positions in sorted(hits.items()):
                # Use index rather than names, as an entry name itself may be private.
                result['markers'].append({'entry_index': result['entries_scanned'] - 1,
                                          'kind': kind, 'subject': subject, 'occurrences': len(positions)})
    with a.report.open('x', encoding='utf-8') as f:
        json.dump(result, f, indent=2); f.write('\n')
    summary = Counter()
    for row in result['markers']: summary[(row['kind'], row['subject'])] += row['occurrences']
    print('Entries scanned:', result['entries_scanned'], 'incomplete:', len(result['incomplete']))
    for (kind, subject), count in sorted(summary.items()): print(kind, subject, count)
    print('No path values recorded; this is not a complete privacy audit.')

if __name__ == '__main__': main()
