#!/usr/bin/env python3
"""Count ASCII email-like strings in outer APK entries without recording them.

Matches are classified by exact presence in three local build inputs. This is
not an identity check, nested archive scan, or complete privacy audit.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parent.parent
UNITY = Path.home() / 'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/Variations/il2cpp/Release/Libs/arm64-v8a/libunity.so'
REFERENCES = {
    'lib/arm64-v8a/libunity.so': UNITY,
    'assets/openbonk-diagnostic/notices/etcpak-BSD-3-Clause.txt': ROOT / 'third_party/openbonk-texture-notices/etcpak-BSD-3-Clause.txt',
    'assets/openbonk-diagnostic/notices/zlib-1.3.2-license.txt': ROOT / 'third_party/openbonk-texture-notices/zlib-1.3.2-license.txt',
}
PATTERN = re.compile(rb'(?i)(?<![a-z0-9._%+-])[a-z0-9][a-z0-9._%+-]{1,63}@[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)*\.[a-z]{2,24}(?![a-z0-9._%+-])')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path); p.add_argument('report', type=Path)
    a = p.parse_args()
    if not a.apk.is_file() or a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():
        p.error('APK must exist and report must be new')
    reference_bytes = {name: path.read_bytes() for name, path in REFERENCES.items()}
    rows = []; total = 0
    with zipfile.ZipFile(a.apk) as z:
        entries = z.infolist()
        if len(entries) > 3000 or len({entry.filename for entry in entries}) != len(entries):
            raise ValueError('unexpected APK inventory')
        for index, entry in enumerate(entries):
            if entry.is_dir(): continue
            if entry.file_size > 256*1024*1024 or total + entry.file_size > 1024*1024*1024:
                raise ValueError('scan size limit')
            data = z.read(entry); total += len(data)
            hits = list(PATTERN.finditer(data))
            if not hits: continue
            reference = reference_bytes.get(entry.filename)
            in_reference = sum(reference is not None and match.group() in reference for match in hits)
            rows.append({'entry_index': index, 'matches': len(hits), 'present_in_reference': in_reference,
                         'unmatched': len(hits)-in_reference})
    report = {'apk_sha256': hashlib.file_digest(a.apk.open('rb'), 'sha256').hexdigest(),
              'entries_scanned': len(entries), 'bytes_scanned': total, 'entries_with_matches': rows,
              'scope': 'ASCII patterns in outer APK entries; no nested ZIPs or other encodings. Presence in a local reference does not establish identity or provenance.'}
    with a.report.open('x', encoding='utf-8') as f: json.dump(report, f, indent=2); f.write('\n')
    print('Entries:',len(entries),'email-like patterns:',sum(x['matches'] for x in rows),
          'also in corresponding local references:',sum(x['present_in_reference'] for x in rows),
          'unmatched:',sum(x['unmatched'] for x in rows))
    print('Values not recorded; this is not a complete privacy audit.')

if __name__ == '__main__': main()
