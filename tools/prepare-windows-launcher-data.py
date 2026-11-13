#!/usr/bin/env python3
"""Prepare a private launcher ZIP from the pinned Windows Steam build.

The launcher source must include direct Windows ZIP support. This
experimental data is game content and is not for redistribution.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
INVENTORY = ROOT/'tools/steam-windows-diagnostic-inputs.json'


def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('windows_data', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args(); src = a.windows_data.resolve(); out = a.output.absolute()
    if not src.is_dir() or not out.parent.is_dir() or out.exists() or out.is_symlink(): p.error('input must exist and output must be new')
    if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()): p.error('output must be separate from Steam data')
    inventory = json.loads(INVENTORY.read_text())
    spec = importlib.util.spec_from_file_location('original_data', ROOT/'tools/prepare-original-data.py')
    original = importlib.util.module_from_spec(spec); spec.loader.exec_module(original)
    try:
        spec = importlib.util.spec_from_file_location('windows_locales', ROOT/'tools/normalize-windows-localization.py')
        locale = importlib.util.module_from_spec(spec); spec.loader.exec_module(locale)
        if sha(locale.MANIFEST) != locale.MANIFEST_SHA: raise ValueError('incompatible Windows localization inventory')
        rows = json.loads(locale.MANIFEST.read_text())['bundles']
        if len(rows) != len(original.BUNDLES) or {row['name'] for row in rows} != set(original.BUNDLES): raise ValueError('inconsistent Windows localization inventory')
        files = []
        for name in ('sharedassets1.assets', 'sharedassets1.assets.resS'):
            path = src/name; row = inventory[name]
            if not path.is_file() or path.stat().st_size != row['size'] or sha(path) != row['sha256']:
                raise ValueError('unsupported Windows Steam file: '+name)
            files.append((path, 'Megabonk_Data/'+name, row['size'], row['sha256']))
        with tempfile.TemporaryDirectory(prefix='.openbonk-windows-launcher-', dir=out.parent) as tmp:
            tmp = Path(tmp)
            for row in rows:
                name, digest, length = row['name'], row['windows_sha256'], row['windows_size']
                path = src/'StreamingAssets/aa/StandaloneWindows64'/name
                if not path.is_file() or path.stat().st_size != length or sha(path) != digest: raise ValueError('Windows locale differs: '+name)
                files.append((path, 'Megabonk_Data/StreamingAssets/aa/StandaloneWindows64/'+name, length, digest))
            staged = tmp/'launcher.zip'
            with zipfile.ZipFile(staged, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as z:
                for path, name, _, _ in files: z.write(path, name)
            with zipfile.ZipFile(staged) as z:
                if len(z.infolist()) != len(files): raise ValueError('unexpected output ZIP entries')
                for _, name, length, expected in files:
                    info = z.getinfo(name)
                    if info.file_size != length: raise ValueError('input changed during ZIP creation: '+name)
                    with z.open(info) as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != expected: raise ValueError('input changed during ZIP creation: '+name)
            os.link(staged, out)
    except (OSError, ValueError, zipfile.BadZipFile) as exc:
        p.error(str(exc))
    print('Prepared experimental Windows launcher ZIP:', out, '; private game data, not for distribution')


if __name__ == '__main__': main()
