#!/usr/bin/env python3
"""Search an APK and bounded nested ZIPs for selected local data, without printing it."""
import argparse
import getpass
import hashlib
import io
import json
from pathlib import Path
import re
import struct
import zipfile
import zlib

BLOCK = 1024 * 1024
MAX_TOTAL = 1024 * 1024 * 1024
MAX_NESTED = 32 * 1024 * 1024
MAX_ENTRIES = 3000
MAX_DEPTH = 3
EMAIL_PATTERN = re.compile(rb'(?i)(?<![a-z0-9._%+-])[a-z0-9][a-z0-9._%+-]{1,63}@[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?)*\.[a-z]{2,24}(?![a-z0-9._%+-])')
SCRUBBED_OBB_SHA = '29af2d72a6982f2cf8905020498127a384a467ef1c56ddeb57dd80e17961055c'
MASKED_OBB_SHAS = {'d1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431',
                   '902dfed8c1dff91fe38b7a75acf72bb94b20fac36b121d145e7bdcce09f58e22'}
BLANK_ENTRY = 'assets/bin/Data/a4611dd71c390bc48af432c727d2e32c'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('apk', type=Path)
    parser.add_argument('report', type=Path, help='new JSON report; contains labels and archive entry names, not profile bytes')
    parser.add_argument('--profile-zip', type=Path, help='optional private profile ZIP containing progression.json and stats.json')
    parser.add_argument('--steam-cloud-root', type=Path, help='optional local Steam CloudDir, to search for its unique nonzero account ID')
    parser.add_argument('--search-local-username', action='store_true', help='also search for the exact local username (may produce false positives)')
    parser.add_argument('--count-email-patterns', action='store_true', help='count scanned files with ASCII email-like patterns without recording values')
    args = parser.parse_args()
    if not args.apk.is_file() or args.report.exists() or args.report.is_symlink() or not args.report.parent.is_dir():
        parser.error('APK must exist and report must be a new file in an existing directory')
    if args.apk.resolve() == args.report.resolve():
        parser.error('report and APK must be separate')
    needles = {'local_home_path': (str(Path.home()) + '/').encode()}
    if args.search_local_username:
        username = getpass.getuser().encode('utf-8')
        if len(username) < 5: raise ValueError('local username is too short for this bounded search')
        needles['local_username'] = username
    if args.profile_zip:
        with zipfile.ZipFile(args.profile_zip) as z:
            for name in ('progression.json', 'stats.json'):
                entries = [i for i in z.infolist() if not i.is_dir() and i.filename.split('/')[-1] == name]
                if len(entries) != 1 or not 0 < entries[0].file_size <= 8 * 1024 * 1024:
                    raise ValueError('profile ZIP does not have two unique bounded files')
                needles['exact_' + name] = z.read(entries[0])
    if args.steam_cloud_root:
        candidates = [p for p in args.steam_cloud_root.iterdir() if p.is_dir() and p.name.isdecimal() and p.name != '0' and (p / 'progression.json').is_file() and (p / 'stats.json').is_file()]
        if len(candidates) != 1:
            raise ValueError('expected exactly one nonzero local Steam account directory')
        needles['steam_account_id'] = candidates[0].name.encode()
    if any(len(value) < (5 if key == 'local_username' else 8) for key, value in needles.items()):
        raise ValueError('search values must be at least 8 bytes')
    # Unity and Android resources may serialize paths or IDs as UTF-16.
    # The two profile files are opaque bytes and remain exact-byte searches.
    for key in ('local_home_path', 'local_username', 'steam_account_id'):
        if key in needles:
            text = needles[key].decode('utf-8')
            for encoding in ('utf-16-le', 'utf-16-be'):
                needles[key + '_' + encoding.replace('-', '_')] = text.encode(encoding)
    keep = max(max(map(len, needles.values())) - 1, 512 if args.count_email_patterns else 0)
    result = {'apk_sha256': None, 'entries_scanned': 0, 'nested_archives': 0, 'bytes_scanned': 0,
              'search_kinds': sorted(needles),
              'matches': [], 'expected_scrubbed_entries': [], 'masked_crc_entries': [], 'incomplete': [], 'limits': {'bytes': MAX_TOTAL, 'nested_entry_bytes': MAX_NESTED, 'entries': MAX_ENTRIES, 'depth': MAX_DEPTH},
              'scope': 'Exact byte matches, including UTF-16 for selected text; not a complete privacy or rights audit.'}
    if args.count_email_patterns:
        result['email_pattern_files'] = {'outer': 0, 'nested': 0}
        result['email_pattern_scope'] = 'ASCII pattern presence only; no values or identities recorded. Extremely long boundary-spanning matches and other encodings may be missed.'

    def safe_name(name):
        encoded = name.encode('utf-8', 'replace')
        for value in needles.values():
            if value in encoded:
                return '[entry name contains searched data]'
        return name

    def scan(label, stream, size, allow_nested):
        tail = b''; collected = bytearray() if allow_nested and size <= MAX_NESTED else None; first = True; zip_like = False; found = set(); email_found = False
        while True:
            chunk = stream.read(BLOCK)
            if not chunk:
                break
            result['bytes_scanned'] += len(chunk)
            if result['bytes_scanned'] > MAX_TOTAL:
                raise ValueError('total scan size exceeds limit')
            sample = tail + chunk
            if args.count_email_patterns and not email_found and EMAIL_PATTERN.search(sample):
                email_found = True
            for key, value in needles.items():
                if value in sample:
                    found.add(key)
            tail = sample[-keep:]
            if first:
                zip_like = chunk.startswith(b'PK\x03\x04')
            if collected is not None:
                if first and not zip_like:
                    collected = None
                else:
                    collected.extend(chunk)
            first = False
        for key in sorted(found):
            result['matches'].append({'entry': safe_name(label), 'kind': key})
        if email_found:
            result['email_pattern_files']['nested' if label.count('!/') > 1 else 'outer'] += 1
        return (bytes(collected) if collected is not None else None), zip_like

    def archive(label, z, depth, known_scrubbed=False, masked_bytes=None):
        if depth > MAX_DEPTH:
            result['incomplete'].append({'entry': safe_name(label), 'reason': 'nested depth limit'})
            return
        entries = z.infolist()
        if len(entries) != len({i.filename for i in entries}):
            result['incomplete'].append({'entry': safe_name(label), 'reason': 'duplicate names'})
        for item in entries:
            result['entries_scanned'] += 1
            if result['entries_scanned'] > MAX_ENTRIES:
                raise ValueError('entry count exceeds limit')
            child = label + '!/' + item.filename
            for key, value in needles.items():
                if value in item.filename.encode('utf-8', 'replace'):
                    result['matches'].append({'entry': safe_name(child), 'kind': key + '_in_name'})
            if item.is_dir():
                continue
            if (known_scrubbed or masked_bytes is not None) and item.filename == BLANK_ENTRY:
                # This pinned slot contains an uncompressed 384-byte prefix,
                # followed by zeros, in place of the original deflate stream.
                # Scan every retained byte rather than silently skipping it.
                try:
                    name_len, extra_len = struct.unpack_from('<HH', masked_bytes, item.header_offset + 26)
                    start = item.header_offset + 30 + name_len + extra_len
                    slot = masked_bytes[start:start + item.compress_size]
                    if (masked_bytes[item.header_offset:item.header_offset + 4] != b'PK\x03\x04'
                        or start != 848444 or item.compress_size != 8502 or item.file_size != 29180
                        or len(slot) != 8502 or any(slot[384:])
                        or hashlib.sha256(slot[:384]).hexdigest() != '62fd958961b088c275cf6f605ebc3463e4904c2b9917a122dcf4e35ee72a98f3'):
                        raise ValueError('unexpected retained font slot')
                    scan(child + ' [retained slot]', io.BytesIO(slot), len(slot), False)
                    result['expected_scrubbed_entries'].append(safe_name(child))
                except (ValueError, TypeError, struct.error):
                    result['incomplete'].append({'entry': safe_name(child), 'reason': 'unexpected scrubbed font slot'})
                continue
            try:
                with z.open(item) as stream:
                    nested, zip_like = scan(child, stream, item.file_size, depth < MAX_DEPTH)
                if zip_like and nested is None:
                    result['incomplete'].append({'entry': safe_name(child), 'reason': 'nested size or depth limit'})
                if nested is not None:
                    if not zipfile.is_zipfile(io.BytesIO(nested)):
                        result['incomplete'].append({'entry': safe_name(child), 'reason': 'ZIP-like data could not be opened'})
                    else:
                        is_scrubbed = hashlib.sha256(nested).hexdigest() == SCRUBBED_OBB_SHA
                        if is_scrubbed:
                            region = nested[848444:848444 + 8502]
                            if len(region) != 8502 or any(region[384:]):
                                raise ValueError('unexpected scrubbed OBB region')
                        with zipfile.ZipFile(io.BytesIO(nested)) as inner:
                            result['nested_archives'] += 1
                            masked = nested if hashlib.sha256(nested).hexdigest() in MASKED_OBB_SHAS | {SCRUBBED_OBB_SHA} else None
                            archive(child, inner, depth + 1, is_scrubbed, masked)
            except (OSError, RuntimeError, zipfile.BadZipFile, zlib.error) as error:
                # The pinned mask template deliberately preserves the original
                # ZIP CRC after replacing certain serialized component bytes.
                # Scan its bounded, raw-deflated split entries despite that CRC;
                # never apply this exception to an unrecognized archive.
                if (masked_bytes is not None and isinstance(error, zipfile.BadZipFile)
                    and item.compress_type == zipfile.ZIP_DEFLATED and item.file_size <= 1048576
                    and re.fullmatch(r'assets/bin/Data/(?:level[124]|sharedassets[12]\.assets)\.split\d+', item.filename)):
                    try:
                        offset = item.header_offset
                        if masked_bytes[offset:offset + 4] != b'PK\x03\x04': raise ValueError('invalid local header')
                        name_len, extra_len = struct.unpack_from('<HH', masked_bytes, offset + 26)
                        start = offset + 30 + name_len + extra_len
                        packed = masked_bytes[start:start + item.compress_size]
                        if len(packed) != item.compress_size: raise ValueError('truncated split')
                        decoder = zlib.decompressobj(-15)
                        raw = decoder.decompress(packed, item.file_size + 1)
                        # Masked templates reserve a fixed compressed slot;
                        # any bytes after the deflate stream must be zero padding.
                        if len(raw) != item.file_size or not decoder.eof or any(decoder.unused_data) or decoder.unconsumed_tail:
                            raise ValueError('invalid split stream')
                        scan(child, io.BytesIO(raw), len(raw), False)
                        result['masked_crc_entries'].append(safe_name(child))
                        continue
                    except (ValueError, struct.error, zlib.error):
                        pass
                result['incomplete'].append({'entry': safe_name(child), 'reason': type(error).__name__})

    with args.apk.open('rb') as f:
        h = hashlib.sha256()
        while chunk := f.read(BLOCK): h.update(chunk)
        result['apk_sha256'] = h.hexdigest()
    with zipfile.ZipFile(args.apk) as z:
        archive(args.apk.name, z, 0)
    with args.report.open('x', encoding='utf-8') as out:
        json.dump(result, out, indent=2); out.write('\n')
    print('Scanned', result['entries_scanned'], 'entries in', result['nested_archives'] + 1, 'archives;', len(result['matches']), 'matches;', len(result['incomplete']), 'incomplete entries.')
    print('Exact matches only, including UTF-16 for selected text; this does not establish privacy, rights or distribution readiness.')
    if result['incomplete']:
        raise SystemExit(2)


if __name__ == '__main__':
    main()
