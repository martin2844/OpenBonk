#!/usr/bin/env python3
"""Build reproducible ZIPs for the optional UI mods (no catalog publication)."""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
MODS = {
    'chest-tracker': 'chest_tracker',
    'minimap-zoom': 'minimap_zoom',
    'toggle-everything': 'toggle_everything',
}


def package(root, output):
    output.mkdir(parents=True, exist_ok=True)
    results = []
    for name, hook in MODS.items():
        source = root / 'mods' / name
        manifest = json.loads((source / 'manifest.json').read_text(encoding='utf-8'))
        if (manifest.get('id') != name or manifest.get('hook') != hook or
                manifest.get('api') != 1 or manifest.get('enabled') is not False):
            raise ValueError('invalid or enabled manifest: ' + name)
        path = output / (name + '.zip')
        with zipfile.ZipFile(path, 'w', zipfile.ZIP_STORED) as archive:
            for filename in ('manifest.json', 'main.lua'):
                info = zipfile.ZipInfo(filename, (1980, 1, 1, 0, 0, 0))
                info.external_attr = 0o100644 << 16
                archive.writestr(info, (source / filename).read_bytes())
        results.append((path, hashlib.sha256(path.read_bytes()).hexdigest()))
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    for path, digest in package(ROOT, args.output):
        print(f'{digest}  {path} ({path.stat().st_size} bytes)')


if __name__ == '__main__':
    main()
