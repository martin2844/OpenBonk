#!/usr/bin/env python3
"""Generate pinned Android shared1 particle masks without particle values."""
import hashlib
import json
from pathlib import Path
import sys

EXPECTED = '8565e5ae34615fa270c5984cbbb56e84c84127492b9d488f7809328d6fbfcf5c'
if len(sys.argv) != 3: raise ValueError('usage: SCRIPT MASK_JSON NEW_JAVA')
source, dest = map(Path, sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir(): raise ValueError('output must be new')
raw = source.read_bytes()
if hashlib.sha256(raw).hexdigest() != EXPECTED: raise ValueError('particle masks differ')
plan = json.loads(raw); rows = plan['objects']
if len(rows) != 196 or plan['changed_nonzero'] != 307832: raise ValueError('invalid particle inventory')
def ints(key): return ', '.join(str(r[key]) for r in rows)
def strings(key): return ',\n        '.join('"' + r[key] + '"' for r in rows)
text = '''package com.openbonk.dataprep;
// Generated from position masks; does not contain particle values.
final class SteamShared1ParticleMasks {
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
    private SteamShared1ParticleMasks() {}
}
''' % (ints('target_offset'), ints('size'), ints('linux_offset'), ints('windows_offset'),
       strings('linux_sha256'), strings('windows_sha256'), strings('mask'))
with dest.open('x', encoding='utf-8') as f: f.write(text)
print('Generated masks for', len(rows), 'shared1 particle systems')
