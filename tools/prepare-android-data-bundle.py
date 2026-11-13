#!/usr/bin/env python3
"""Prepare a local diagnostic bundle for the experimental Android launcher.

This includes original game content and is not a distributable release.
"""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import stat
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OBB_SHA = 'a3bcca988a39ac3868dc39a2f6384b0e5c633ce95d68b180f73ae3bb66e43400'


def sha_file(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def verify_steam_zip(path, windows_launcher=False, windows_raw=False):
    if windows_raw and not windows_launcher: raise ValueError('Windows localization requires a Windows-aware launcher')
    spec = importlib.util.spec_from_file_location('original_data', ROOT / 'tools/prepare-original-data.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    inventory = json.loads((ROOT / ('tools/steam-windows-diagnostic-inputs.json' if windows_launcher else 'tools/steam-linux-diagnostic-inputs.json')).read_text())
    if windows_launcher:
        inventory = {name: inventory[name] for name in ('sharedassets1.assets', 'sharedassets1.assets.resS')}
    relative = {name: (item['size'], item['sha256']) for name, item in inventory.items()}
    localization = 'StreamingAssets/aa/StandaloneWindows64/' if windows_raw else 'StreamingAssets/aa/StandaloneLinux64/'
    if windows_raw:
        normalizer_spec = importlib.util.spec_from_file_location('windows_locale_inventory', ROOT/'tools/normalize-windows-localization.py')
        normalizer = importlib.util.module_from_spec(normalizer_spec); normalizer_spec.loader.exec_module(normalizer)
        if sha_file(normalizer.MANIFEST) != normalizer.MANIFEST_SHA: raise ValueError('incompatible Windows localization inventory')
        rows = json.loads(normalizer.MANIFEST.read_text())['bundles']
        if len(rows) != len(module.BUNDLES) or {r['name'] for r in rows} != set(module.BUNDLES): raise ValueError('inconsistent Windows localization inventory')
        relative.update({localization + r['name']: (r['windows_size'], r['windows_sha256']) for r in rows})
    else:
        relative.update({localization + name: (None, digest) for name, digest in zip(module.BUNDLES, module.BUNDLE_SHA256)})
    if len(relative) != len(inventory) + len(module.BUNDLES):
        raise ValueError('inconsistent Steam data inventory')
    with zipfile.ZipFile(path) as z:
        entries = z.infolist()
        if len(entries) > 20000:
            raise ValueError('too many ZIP entries')
        seen, files = set(), {}
        for entry in entries:
            name = entry.filename
            parts = name.rstrip('/').split('/')
            if name.startswith('/') or '\\' in name or any(x in ('', '.', '..') or ':' in x for x in parts):
                raise ValueError('unsafe ZIP path')
            key = name.rstrip('/').casefold()
            if key in seen:
                raise ValueError('duplicate ZIP entry')
            seen.add(key)
            if not entry.is_dir(): files[name] = entry
        marker = 'sharedassets1.assets'
        roots = {name[:-len(marker)] for name in files
                 if name == marker or name == 'Megabonk_Data/' + marker
                 or name.endswith('/Megabonk_Data/' + marker)}
        if len(roots) != 1:
            raise ValueError('Steam ZIP must contain exactly one Megabonk_Data folder')
        root = roots.pop()
        platform_root = root + 'StreamingAssets/aa/'
        if (not windows_raw and any(name.startswith(platform_root+'StandaloneWindows64/') for name in files)
                and not any(name.startswith(platform_root+'StandaloneLinux64/') for name in files)):
            raise ValueError('este ZIP contiene datos de Windows; por ahora la conversión requiere la versión Linux de Steam')
        expected = {root + name: spec for name, spec in relative.items()}
        missing = sorted(set(expected) - set(files))
        if missing:
            raise ValueError('Steam ZIP is incomplete or has an unsupported layout; missing: ' + ', '.join(missing))
        # Android's importers also reject a second matching asset or locale in
        # a different accepted root, even if its bytes are identical.
        for name in files:
            for suffix in relative:
                if (name == suffix or name == 'Megabonk_Data/' + suffix
                        or name.endswith('/Megabonk_Data/' + suffix)) and name != root + suffix:
                    raise ValueError('Steam ZIP contains conflicting data roots: ' + name)
        for name, (length, expected_hash) in expected.items():
            entry = files[name]
            if (entry.flag_bits & 1 or stat.S_ISLNK(entry.external_attr >> 16) or entry.file_size <= 0
                    or (length is not None and entry.file_size != length)
                    or (length is None and entry.file_size > 8 * 1024 * 1024)):
                raise ValueError('incompatible ZIP entry: ' + name)
            with z.open(entry) as f:
                if hashlib.file_digest(f, 'sha256').hexdigest() != expected_hash:
                    raise ValueError('incompatible ZIP content: ' + name)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('obb', type=Path, help='OBB produced by the current local diagnostic build')
    p.add_argument('steam_zip', type=Path, help='ZIP containing the supported Linux Steam game data')
    p.add_argument('output', type=Path, help='new local ZIP path')
    p.add_argument('--diagnostic-report', type=Path, help='local diagnostic report that records the target build OBB SHA-256')
    p.add_argument('--windows-launcher', action='store_true', help='experimental Windows-derived launcher ZIP, requires a Windows-aware APK')
    p.add_argument('--windows-raw', action='store_true', help='use original Windows localization bundles; requires direct-ZIP launcher')
    a = p.parse_args()
    obb, steam, output = a.obb.resolve(), a.steam_zip.resolve(), a.output.absolute()
    if not obb.is_file() or not steam.is_file() or not output.parent.is_dir():
        p.error('input files and output parent must exist')
    if output.exists() or output.is_symlink():
        p.error('output must be new')
    expected_obb = OBB_SHA
    if a.diagnostic_report is not None:
        try:
            report = json.loads(a.diagnostic_report.read_text())
            expected_obb = report['obb_sha256']
            if not isinstance(expected_obb, str) or len(expected_obb) != 64 or any(c not in '0123456789abcdef' for c in expected_obb):
                raise ValueError('invalid OBB hash')
        except (OSError, ValueError, KeyError, TypeError) as e:
            p.error('invalid diagnostic report: ' + str(e))
    if not 0 < obb.stat().st_size <= 512 * 1024 * 1024 or sha_file(obb) != expected_obb:
        p.error('incompatible diagnostic OBB')
    if not 0 < steam.stat().st_size <= 1024 * 1024 * 1024:
        p.error('invalid Steam ZIP size')
    try:
        verify_steam_zip(steam, windows_launcher=a.windows_launcher, windows_raw=a.windows_raw)
        steam_sha = sha_file(steam)
        with tempfile.NamedTemporaryFile(prefix='.openbonk-bundle-', dir=output.parent, delete=False) as tmp:
            temporary = Path(tmp.name)
        try:
            with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_STORED, allowZip64=True) as z:
                z.write(obb, 'openbonk-data.obb')
                z.write(steam, 'steam-data.zip')
            with zipfile.ZipFile(temporary) as z:
                for name, expected in (('openbonk-data.obb', expected_obb), ('steam-data.zip', steam_sha)):
                    with z.open(name) as f:
                        if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                            raise ValueError('input changed while bundling: ' + name)
            os.link(temporary, output)
        finally:
            temporary.unlink(missing_ok=True)
    except (OSError, ValueError, zipfile.BadZipFile) as e:
        p.error(str(e))
    print('Local diagnostic bundle created:', output, '; contains original game data and is not for distribution')


if __name__ == '__main__':
    main()
