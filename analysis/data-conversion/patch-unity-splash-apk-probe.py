#!/usr/bin/env python3
"""Make an unsigned local APK probe with a reconstructed splash asset.

The output retains all other game, Unity and third-party content. It is not a
release, and replacing a texture does not settle asset or code rights.
"""
import hashlib
from pathlib import Path
import sys
import zipfile

mode = sys.argv[4] if len(sys.argv) == 5 else ''
if len(sys.argv) not in (4, 5) or mode not in ('', '--unsigned', '--blank', '--restore-blank'): raise SystemExit('usage: patch-unity-splash-apk-probe.py INPUT.apk ASSET OUTPUT.apk [--unsigned|--blank|--restore-blank]')
unsigned_input = bool(mode)
src, asset, out = map(Path, sys.argv[1:4])
def sha(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda: f.read(1048576), b''): h.update(b)
    return h.hexdigest()
if out.exists() or not out.parent.is_dir(): raise ValueError('output must be new and parent must exist')
asset_hash = '2e652dbff59cf547e44753d7236d52ff8f0dca03fa51fc38f2ed5edc10e93620' if mode == '--blank' else '6c4768f32e18a14ce3a9d945bd96b417db33df5bbe4a336d9ee26c38ea4bde58'
if (not unsigned_input and sha(src) != '7cd9dc21d007dd7d2443bc6d070d9f7f1b3f0c812e7376f3df9bf11a92d996f2') or sha(asset) != asset_hash: raise ValueError('incompatible inputs')
replacement = asset.read_bytes()
prefix = 'assets/bin/Data/globalgamemanagers.assets.split'
created = False
try:
    with zipfile.ZipFile(src) as source:
        names = source.namelist()
        if len(names) != len(set(names)): raise ValueError('duplicate APK entry')
        if unsigned_input:
            with src.open('rb') as f:
                f.seek(max(0, source.start_dir - 16)); magic = f.read(16)
            if magic == b'APK Sig Block 42' or any(n.upper().startswith('META-INF/') for n in names): raise ValueError('input must be unsigned')
        parts = sorted((n for n in names if n.startswith(prefix)), key=lambda n: int(n[len(prefix):]))
        if not parts or [int(n[len(prefix):]) for n in parts] != list(range(len(parts))): raise ValueError('invalid asset fragments')
        if sum(source.getinfo(n).file_size for n in parts) != len(replacement): raise ValueError('unexpected asset length')
        expected_hash = '2e652dbff59cf547e44753d7236d52ff8f0dca03fa51fc38f2ed5edc10e93620' if mode == '--restore-blank' else '2e08863a7daea9815089d2fec220e3d44334d15af1d4f4c074488f3be480869e'
        if hashlib.sha256(b''.join(source.read(n) for n in parts)).hexdigest() != expected_hash: raise ValueError('incompatible original splash asset')
        chunks = {}; offset = 0
        for name in parts:
            size = source.getinfo(name).file_size; chunks[name] = replacement[offset:offset+size]; offset += size
        with zipfile.ZipFile(out, 'x', allowZip64=True) as dest:
            created = True
            for info in source.infolist():
                if info.filename.upper().startswith('META-INF/'): continue
                dest.writestr(info, chunks.get(info.filename, source.read(info)))
    with zipfile.ZipFile(src) as before, zipfile.ZipFile(out) as after:
        expected = {n for n in before.namelist() if not n.upper().startswith('META-INF/')}
        if set(after.namelist()) != expected or after.testzip() is not None: raise ValueError('invalid output archive')
        for name in expected:
            if after.read(name) != (chunks[name] if name in chunks else before.read(name)): raise ValueError('unexpected change: ' + name)
    print('Replaced', len(parts), 'splash asset fragments; output is unsigned and not distributable.')
except Exception:
    if created: out.unlink(missing_ok=True)
    raise
