#!/usr/bin/env python3
"""Reconstruct the 18 pinned Linux localization bundles from Windows data.

Local research only: output contains original game assets. This handles only
localization and does not make Windows game data or the port distributable.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT/'build/android/windows-localization/manifest.json'
PATCH = ROOT/'build/android/windows-localization/thai.bsdiff'
MANIFEST_SHA = '534dffa02f063ce4b814df55effd971318086ac6b9e0a7f39091d7ba3bc0a549'
PATCH_SHA = 'e80b9e69d671b853cb8a4207ab2a93777619a14add8c761530ebcc0fee7c34a5'


def digest(data): return hashlib.sha256(data).hexdigest()


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('windows_data', type=Path, help='Windows Megabonk_Data directory')
    p.add_argument('output', type=Path, help='new directory for reconstructed locale bundles')
    a = p.parse_args(); source = a.windows_data.resolve(strict=True); out = a.output.absolute()
    if not source.is_dir() or not out.parent.is_dir() or out.exists() or out.is_symlink(): p.error('input must exist and output must be new')
    if out.resolve().is_relative_to(source) or source.is_relative_to(out.resolve()): p.error('output must be separate from input')
    raw = MANIFEST.read_bytes(); patch = PATCH.read_bytes()
    if digest(raw) != MANIFEST_SHA or digest(patch) != PATCH_SHA: raise ValueError('local conversion manifest or patch differs')
    rows = json.loads(raw)['bundles']; original = load('original', ROOT/'tools/prepare-original-data.py')
    expected = dict(zip(original.BUNDLES, original.BUNDLE_SHA256))
    if len(rows) != len(expected) or set(row['name'] for row in rows) != set(expected): raise ValueError('localization inventory differs')
    bsdiff = load('bsdiff', ROOT/'tools/apply-bsdiff-pinned.py'); results = {}
    for row in rows:
        name = row['name']; path = source/'StreamingAssets/aa/StandaloneWindows64'/name
        if row['linux_sha256'] != expected[name] or not path.is_file() or path.stat().st_size != row['windows_size'] or path.stat().st_size > 8*1024*1024:
            raise ValueError('unsupported or missing Windows bundle: '+name)
        data = path.read_bytes()
        if digest(data) != row['windows_sha256']: raise ValueError('Windows bundle differs: '+name)
        if row['mode'] == 'platform_byte':
            data = bytearray(data); offset = row['offset']
            if not isinstance(offset, int) or not 0 <= offset < len(data) or data[offset] != 19: raise ValueError('unexpected platform marker')
            data[offset] = 24; data = bytes(data)
        elif row['mode'] == 'bsdiff' and row['patch_sha256'] == PATCH_SHA:
            data = bsdiff.apply(data, patch, row['linux_size'], row['linux_sha256'])
        else: raise ValueError('unsupported conversion mode')
        if len(data) != row['linux_size'] or digest(data) != expected[name]: raise ValueError('converted bundle differs: '+name)
        results[name] = data
    out.mkdir(mode=0o700)
    try:
        for name, data in results.items(): (out/name).write_bytes(data)
        (out/'report.json').write_text(json.dumps({'kind':'local research only, not for distribution', 'bundles':len(results), 'limitations':['Only localization is converted.', 'Output contains original game content.', 'Full Windows conversion and Android runtime are not established.']},indent=2)+'\n')
    except BaseException:
        shutil.rmtree(out); raise
    print('Reconstructed',len(results),'localization bundles; local research only.')


if __name__ == '__main__': main()
