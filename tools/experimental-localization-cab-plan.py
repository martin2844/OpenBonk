#!/usr/bin/env python3
"""Build a private exact-fragment experiment from two local UnityFS bundles.

The output contains game-derived data and must not be shared. This experimental
plan targets an uncompressed Android bundle and is not part of the launcher.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parent.parent
LIMIT = 32 * 1024 * 1024

def digest(data): return hashlib.sha256(data).digest()

def load_bundle(path):
    import UnityPy
    data = path.read_bytes()
    if not data or len(data) > LIMIT: raise ValueError('bundle size outside limit')
    env = UnityPy.load(data)
    if len(env.files) != 1: raise ValueError('expected one UnityFS bundle')
    bundle = next(iter(env.files.values()))
    if bundle.signature != 'UnityFS' or bundle.version not in (7, 8) or bundle.version_engine != '2023.2.22f1' or len(bundle.files) != 1:
        raise ValueError('unsupported bundle layout')
    name, cab = next(iter(bundle.files.items()))
    if not name.startswith('CAB-') or not hasattr(cab, 'reader'): raise ValueError('expected serialized CAB')
    raw = bytes(cab.reader.bytes)
    if not raw or len(raw) > LIMIT: raise ValueError('CAB size outside limit')
    return data, bundle, name, raw

def plain_bundle(bundle, name, raw):
    def cstring(s):
        value = s.encode('ascii')
        if not value or b'\0' in value or len(value) > 255: raise ValueError('invalid bundle string')
        return value + b'\0'
    info = (bytes(16) + struct.pack('>I', 1) + struct.pack('>IIH', len(raw), len(raw), 0) +
            struct.pack('>IQQI', 1, 0, len(raw), bundle.files[name].flags) + cstring(name))
    prefix = b'UnityFS\0' + struct.pack('>I', bundle.version) + cstring(bundle.version_player) + cstring(bundle.version_engine)
    header_len = (len(prefix) + 20 + 15) & ~15
    size = header_len + len(info) + len(raw)
    if size > LIMIT: raise ValueError('output exceeds limit')
    header = prefix + struct.pack('>QIII', size, len(info), len(info), 64)
    output = header + bytes(header_len-len(header)) + info + raw
    return output, header_len + len(info)

def build(android, steam, small_fragments=False):
    _, target, name, target_cab = load_bundle(android)
    source_bytes, _, _, source_cab = load_bundle(steam)
    target_bytes, offset = plain_bundle(target, name, target_cab)
    spec = importlib.util.spec_from_file_location('exact_objects', ROOT / 'tools/experimental-exact-object-plan.py')
    matcher = importlib.util.module_from_spec(spec); spec.loader.exec_module(matcher)
    rows = list(matcher.common_fragments(target_cab, [('source', 0, source_cab)], minimum=12 if small_fragments else 32))
    if not rows or len(rows) > 10000: raise ValueError('fragment count outside limit')
    template = bytearray(target_bytes); records = bytearray(); removed = 0
    for target_pos, payload, _, source_pos in rows:
        start = offset + target_pos
        if template[start:start+len(payload)] != payload or source_cab[source_pos:source_pos+len(payload)] != payload: raise ValueError('fragment mismatch')
        removed += sum(v != 0 for v in payload)
        template[start:start+len(payload)] = bytes(len(payload))
        records += struct.pack('>III', start, source_pos, len(payload)) + digest(payload)
    plan = b'OBL1' + digest(source_bytes) + digest(template) + digest(target_bytes) + struct.pack('>I', len(rows)) + records
    return bytes(template), plan, target_bytes, len(rows), removed

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--android-bundle', required=True, type=Path)
    parser.add_argument('--steam-bundle', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--small-fragments', action='store_true', help='experimentally match exact fragments as small as 12 bytes')
    a = parser.parse_args()
    android = a.android_bundle.resolve(strict=True); steam = a.steam_bundle.resolve(strict=True)
    out = a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir() or out.resolve().is_relative_to(ROOT) or android.is_relative_to(out.resolve()) or steam.is_relative_to(out.resolve()):
        parser.error('output must be a new directory outside the checkout and inputs')
    template, plan, target_bytes, count, removed = build(android, steam, a.small_fragments)
    out.mkdir(mode=0o700)
    (out/'template.bundle').write_bytes(template)
    (out/'plan.bin').write_bytes(plan)
    (out/'expected.bundle').write_bytes(target_bytes)
    print(f'Private prototype: {count} fragments; {removed} matching nonzero CAB bytes masked. Output still contains assets.')

if __name__ == '__main__': main()
