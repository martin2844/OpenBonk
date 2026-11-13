#!/usr/bin/env python3
"""Prepare a local Steam data ZIP for OpenBonk diagnostics.

This is not the complete game-data package and does not make the APK distributable.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
import zipfile

BUNDLES = (
    'localization-assets-shared_assets_all.bundle',
    'localization-string-tables-chinese(simplified)(zh-hans)_assets_all.bundle',
    'localization-string-tables-chinese(traditional)(zh-hant)_assets_all.bundle',
    'localization-string-tables-czech(cs)_assets_all.bundle',
    'localization-string-tables-english(en)_assets_all.bundle',
    'localization-string-tables-french(fr)_assets_all.bundle',
    'localization-string-tables-german(de)_assets_all.bundle',
    'localization-string-tables-italian(it)_assets_all.bundle',
    'localization-string-tables-japanese(ja)_assets_all.bundle',
    'localization-string-tables-korean(ko)_assets_all.bundle',
    'localization-string-tables-polish(pl)_assets_all.bundle',
    'localization-string-tables-portuguese(pt)_assets_all.bundle',
    'localization-string-tables-russian(ru)_assets_all.bundle',
    'localization-string-tables-spanish(es)_assets_all.bundle',
    'localization-string-tables-spanish_latam_assets_all.bundle',
    'localization-string-tables-thai(th)_assets_all.bundle',
    'localization-string-tables-turkish(tr)_assets_all.bundle',
    'localization-string-tables-ukrainian(uk)_assets_all.bundle',
)
BUNDLE_SHA256 = (
    '10c446c917a0e3a038c37f9a1ae132e76bc65a9011ebdd07716f146641741ffe',
    '7a9bfd7308ed62d88e1ab70d923e843e1f420e81d2b33e0dde501587a2f96200',
    'efa0165ffd5fb551715e34e7e902c0c0213fec71455b93e53c63b47d8373a5ce',
    '68f67f7281a4bad9565d167b7ddf5022b842a37d7859f6925457222dcac46e51',
    '88b425367cc9b0387a4e431e69fe7f3c24f58ba1122d014b98319363112ef3af',
    'e63d85084285fa9da2458218efbce22090c08bbc483f6924871f1facf414065a',
    'f5d05f48753ad9f21294ec967401066df96d767fddda954aa769e9e9ef08a375',
    '4784cc3573d98960888680774b170d2afc38d34d022059506583a8327dd04d15',
    '4bcaf60d4c9e0da02e53ad15c04addf1255e4d3bbc4eacff935f643fc3131534',
    '06d1f3c2aa4001b87fb49456b498806f25bc2bda14b3e46f12246fcf102f0098',
    'd988faab45ec3d515d719f9df6d65eed4100c3775ee235def7456125facf9951',
    '80433690e7b08685d1557bac19c5e894618cdd3cc923141983786489314c5bdb',
    '7556fe1c725138e48b643e89667c9a675ddc21976516ee26d0fb072e94896d83',
    'c094b4860376ee9719511a285bf4d04dfab7acb4350570675da81511019c2a94',
    '23d625d1c8d354d697c973ed726329a6ba851710853e444b7469f398ad9cd0b4',
    '05fef478dfcf7c835b076153e3815db9b5677aca42b678daa1026268c44e15a6',
    'a41cf3082be7bf09208d5798461d44635a476817a48ad684c253d8b15affec4b',
    '0af172bcc79c66dcb8da78b524ab95a04ea1a4f75c226364a75102afa664db78',
)
PINNED = {
    'sharedassets1.assets': (56239204, 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'),
    'sharedassets1.assets.resS': (113115008, '43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'),
}

def sha256(path):
    digest = hashlib.sha256()
    with path.open('rb') as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b''):
            digest.update(chunk)
    return digest.hexdigest()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('data_dir', type=Path, help='Megabonk_Data from the supported Linux Steam build')
    parser.add_argument('output', type=Path, help='new ZIP path; existing files are never overwritten')
    parser.add_argument('--complete', action='store_true', help='include the additional pinned files used by the local desktop diagnostic builder')
    args = parser.parse_args()
    root = args.data_dir.resolve(strict=True)
    output = args.output.absolute()
    if not root.is_dir() or not output.parent.is_dir():
        parser.error('input data and output parent must be existing directories')
    if output.exists() or output.is_symlink():
        parser.error('output already exists')
    if output.resolve().is_relative_to(root):
        parser.error('output must be outside the Steam data directory')
    platforms = root / 'StreamingAssets/aa'
    if (platforms / 'StandaloneWindows64').is_dir() and not (platforms / 'StandaloneLinux64').is_dir():
        parser.error('esta copia de Megabonk es de Windows; por ahora la conversión requiere la versión Linux de Steam')
    files = []
    for name, (length, digest) in PINNED.items():
        path = root / name
        if not path.is_file() or path.stat().st_size != length or sha256(path) != digest:
            parser.error(f'unsupported or missing original data: {name}')
        files.append((path, 'Megabonk_Data/' + name, length, digest))
    if args.complete:
        inventory_path = Path(__file__).resolve().with_name('steam-linux-diagnostic-inputs.json')
        inventory = json.loads(inventory_path.read_text())
        for name, spec in sorted(inventory.items()):
            if name in PINNED:
                if (spec['size'], spec['sha256']) != PINNED[name]:
                    raise ValueError('inconsistent supported Steam inventory: ' + name)
                continue
            parts = name.split('/')
            if any(part in ('', '.', '..') or '\\' in part or ':' in part for part in parts):
                raise ValueError('invalid supported Steam inventory path: ' + name)
            path = root.joinpath(*parts)
            length, digest = spec['size'], spec['sha256']
            if not path.is_file() or path.stat().st_size != length or sha256(path) != digest:
                parser.error(f'unsupported or missing original data: {name}')
            files.append((path, 'Megabonk_Data/' + name, length, digest))
    prefix = 'StreamingAssets/aa/StandaloneLinux64/'
    if len(BUNDLES) != len(BUNDLE_SHA256) or len(set(BUNDLES)) != len(BUNDLES):
        raise ValueError('invalid supported localization inventory')
    for name, expected in zip(BUNDLES, BUNDLE_SHA256):
        path = root / prefix / name
        if not path.is_file() or not 0 < path.stat().st_size <= 8 * 1024 * 1024 or sha256(path) != expected:
            parser.error(f'unsupported or missing localization bundle for this game version: {name}')
        files.append((path, 'Megabonk_Data/' + prefix + name, path.stat().st_size, expected))
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(prefix='.openbonk-', suffix='.tmp', dir=output.parent, delete=False) as tmp:
            temporary = Path(tmp.name)
        with zipfile.ZipFile(temporary, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as archive:
            for path, name, _, _ in files:
                archive.write(path, name)
        # Verify the bytes that will actually be delivered, including CRC checks.
        # Inputs can change after the initial validation while the ZIP is written.
        with zipfile.ZipFile(temporary, 'r') as archive:
            for _, name, length, expected in files:
                info = archive.getinfo(name)
                digest = hashlib.sha256()
                total = 0
                with archive.open(info) as source:
                    for chunk in iter(lambda: source.read(1024 * 1024), b''):
                        total += len(chunk)
                        digest.update(chunk)
                if total != length or digest.hexdigest() != expected:
                    raise ValueError(f'input changed while creating ZIP: {name}')
        # A hard link fails atomically if another process created the destination.
        os.link(temporary, output)
        print(f'Created {output} ({len(files)} files). This ZIP contains game data for your own local use.')
        if args.complete:
            print('Compatible with the local diagnostic builder and the launcher optional text/texture import.')
            print('Desktop conversion is still required; this is not a standalone or distributable package.')
        else:
            print('It provides optional text and textures only; it is not a standalone or distributable game-data package.')
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)

if __name__ == '__main__':
    main()
