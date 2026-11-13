#!/usr/bin/env python3
"""Inventory bounded embedded launcher templates and exact Steam-file matches.

This is research evidence, not a license or provenance determination. Reports
contain paths, lengths and hashes only; they never contain asset contents.
"""
import argparse
from collections import defaultdict
import hashlib
import io
import json
from pathlib import Path
from zipfile import BadZipFile, ZipFile
import zlib

BLOCK = 1024 * 1024
BASE = 'assets/openbonk-diagnostic/base.obb'
PLANS = 'assets/openbonk-diagnostic/plans.zip'

def digest_stream(stream):
    h = hashlib.sha256()
    size = nonzero = 0
    while block := stream.read(BLOCK):
        size += len(block); nonzero += len(block) - block.count(0); h.update(block)
    return size, nonzero, h.hexdigest()

def read_zip_blob(archive, name, cap):
    entry = archive.getinfo(name)
    if entry.file_size > cap or entry.is_dir():
        raise ValueError('invalid nested ZIP size: ' + name)
    with archive.open(entry) as src: return src.read()

def check_names(archive):
    names = [x.filename for x in archive.infolist()]
    if len(names) != len(set(names)): raise ValueError('duplicate ZIP entries')

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path); p.add_argument('steam_zip', type=Path); p.add_argument('report', type=Path)
    a = p.parse_args()
    if not a.apk.is_file() or not a.steam_zip.is_file() or a.report.exists() or a.report.is_symlink() or not a.report.parent.is_dir():
        p.error('inputs must exist and report must be new in an existing directory')
    with a.apk.open('rb') as f: apk_hash = digest_stream(f)[2]
    steam = defaultdict(list)
    with ZipFile(a.steam_zip) as z:
        check_names(z)
        for e in z.infolist():
            if e.is_dir(): continue
            with z.open(e) as f: size, _, digest = digest_stream(f)
            steam[(size, digest)].append(e.filename)
    with ZipFile(a.apk) as apk:
        check_names(apk)
        base = read_zip_blob(apk, BASE, 16 * BLOCK)
        plans = read_zip_blob(apk, PLANS, 32 * BLOCK)
    entries = []
    unreadable = []
    with ZipFile(io.BytesIO(base)) as z:
        check_names(z)
        for e in z.infolist():
            if e.is_dir(): continue
            try:
                with z.open(e) as f: size, nonzero, digest = digest_stream(f)
            except (BadZipFile, zlib.error, EOFError, OSError, ValueError) as error:
                unreadable.append({'entry': e.filename, 'declared_size': e.file_size,
                                   'compressed_size': e.compress_size, 'error': type(error).__name__})
                continue
            entries.append({'entry': e.filename, 'size': size, 'nonzero_bytes': nonzero,
                            'sha256': digest, 'exact_steam_entries': steam.get((size, digest), [])})
    plan_entries = []
    with ZipFile(io.BytesIO(plans)) as z:
        check_names(z)
        for e in z.infolist():
            if e.is_dir(): continue
            with z.open(e) as f: size, nonzero, digest = digest_stream(f)
            plan_entries.append({'entry': e.filename, 'size': size, 'nonzero_bytes': nonzero, 'sha256': digest})
    matches = [e for e in entries if e['exact_steam_entries']]
    report = {'apk_sha256': apk_hash, 'base_zip_sha256': hashlib.sha256(base).hexdigest(),
              'plan_zip_sha256': hashlib.sha256(plans).hexdigest(),
              'steam_file_count': sum(map(len, steam.values())),
              'base_entries': len(entries), 'base_unreadable_entries': unreadable,
              'base_uncompressed_bytes': sum(e['size'] for e in entries),
              'base_nonzero_bytes': sum(e['nonzero_bytes'] for e in entries),
              'exact_steam_matches': len(matches), 'exact_steam_match_bytes': sum(e['size'] for e in matches),
              'plan_entries': plan_entries, 'entries': entries,
              'limitations': 'Only readable whole files were checked for exact equality to the supplied Steam ZIP; unreadable template entries were not scanned. Unmatched data may still derive from the game or third parties. No rights are determined.'}
    with a.report.open('x', encoding='utf-8') as f: json.dump(report, f, indent=2); f.write('\n')
    print('base entries:', len(entries), 'nonzero bytes:', report['base_nonzero_bytes'],
          'exact Steam matches:', len(matches), 'unreadable entries:', len(unreadable), 'plan entries:', len(plan_entries))

if __name__ == '__main__': main()
