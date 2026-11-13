#!/usr/bin/env python3
"""Generate pinned Android particle masks without copying particle values."""
import hashlib
import json
from pathlib import Path
import sys

EXPECTED = '23d25425ba8c5c4697fd5fd8b62fe2b7f712568134dac579c11f7719741cee80'
if len(sys.argv) != 3: raise ValueError('usage: SCRIPT MASK_JSON NEW_JAVA')
source, dest = map(Path, sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir(): raise ValueError('output must be new')
raw = source.read_bytes()
if hashlib.sha256(raw).hexdigest() != EXPECTED: raise ValueError('particle masks differ')
plan = json.loads(raw); rows = plan['objects']
if len(rows) != 398 or plan['changed_nonzero'] != 623394: raise ValueError('invalid particle inventory')
def ints(key): return ', '.join(str(r[key]) for r in rows)
def strings(key): return ',\n        '.join('"' + r[key] + '"' for r in rows)
text = '''package com.openbonk.dataprep;
// Generated from position masks; does not contain particle values.
final class SteamShared2ParticleMasks {
    static final int[] TARGET = {%s};
    static final int[] SIZE = {%s};
    static final int[] LINUX = {%s};
    static final int[] WINDOWS = {%s};
    static final String[] LINUX_SHA = {
        %s};
    static final String[] WINDOWS_SHA = {
        %s};
    static final String[] MASKS = {
        %s};
    private SteamShared2ParticleMasks() {}
}
''' % (ints('target_offset'), ints('size'), ints('linux_offset'), ints('windows_offset'),
       strings('linux_sha256'), strings('windows_sha256'), strings('mask'))
with dest.open('x', encoding='utf-8') as f: f.write(text)
print('Generated masks for', len(rows), 'particle systems')
