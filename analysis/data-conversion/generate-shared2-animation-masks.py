#!/usr/bin/env python3
"""Generate Java position-mask constants; the input contains no clip bytes."""
import hashlib
import json
from pathlib import Path
import sys

EXPECTED = '63b61047b81ffc2029b86fa63153938203dc845123987e0eb3a30917478ab482'
if len(sys.argv) != 3: raise ValueError('usage: SCRIPT MASK_JSON NEW_JAVA')
source, dest = map(Path, sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir(): raise ValueError('output must be new')
raw = source.read_bytes()
if hashlib.sha256(raw).hexdigest() != EXPECTED: raise ValueError('mask inventory differs')
rows = json.loads(raw)['objects']
if len(rows) != 8 or sum(r['preserved_positions'] for r in rows) != 502: raise ValueError('invalid mask inventory')
def ints(key): return ', '.join(str(r[key]) for r in rows)
def strings(key): return ',\n        '.join('"' + r[key] + '"' for r in rows)
text = '''package com.openbonk.dataprep;
// Generated from position masks; does not contain animation byte values.
final class SteamShared2AnimationMasks {
    static final int[] TARGET = {%s};
    static final int[] SIZE = {%s};
    static final int[] LINUX = {%s};
    static final int[] WINDOWS = {%s};
    static final String[] SOURCE_SHA = {
        %s};
    static final String[] POSITIONS = {
        %s};
    private SteamShared2AnimationMasks() {}
}
''' % (ints('target'), ints('size'), ints('linux_offset'), ints('windows_offset'), strings('source_sha256'), strings('mask_base64'))
with dest.open('x', encoding='utf-8') as f: f.write(text)
print('Generated masks for', len(rows), 'objects')
