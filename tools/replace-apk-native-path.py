#!/usr/bin/env python3
"""Replace a fixed-length build-path prefix in native APK libraries.

This invalidates APK signatures. Inspect and test the result, then align and
sign it. It is a targeted cleanup, not a complete privacy audit.
"""
import argparse
from pathlib import Path
import re
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path)
    parser.add_argument('output', type=Path, help='new, unsigned APK')
    parser.add_argument('--source-prefix', required=True)
    parser.add_argument('--replacement-prefix', required=True)
    args = parser.parse_args()
    source = args.source_prefix.encode('utf-8')
    replacement = args.replacement_prefix.encode('utf-8')
    if not source or len(source) != len(replacement) or source == replacement:
        parser.error('prefixes must be distinct, nonempty and equal-length in UTF-8 bytes')
    if not args.input.is_file():
        parser.error('input APK does not exist')
    signatures = re.compile(r'^META-INF/(?:MANIFEST\.MF|[^/]+\.(?:SF|RSA|DSA|EC))$', re.I)
    modified = 0
    try:
        with zipfile.ZipFile(args.input) as src, args.output.open('xb') as raw:
            try:
                with zipfile.ZipFile(raw, 'w', allowZip64=True) as dst:
                    dst.comment = src.comment
                    names = set()
                    for entry in src.infolist():
                        if entry.filename in names:
                            raise ValueError('duplicate APK entry')
                        names.add(entry.filename)
                        if signatures.fullmatch(entry.filename):
                            continue
                        data = src.read(entry)
                        if entry.filename.startswith('lib/') and entry.filename.endswith('.so'):
                            modified += data.count(source)
                            data = data.replace(source, replacement)
                        dst.writestr(entry, data)
                if not modified:
                    raise ValueError('prefix not found in native libraries')
            except BaseException:
                raw.close()
                args.output.unlink(missing_ok=True)
                raise
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        parser.exit(1, f'APK path replacement failed: {exc}\n')
    print(f'Replaced {modified} occurrences in native libraries; output must be signed and tested.')


if __name__ == '__main__':
    main()
