#!/usr/bin/env python3
"""Build deterministic launcher-compatible ZIPs for the experimental Lua mods."""
import argparse
import hashlib
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent
MODS = ('instant-collect', 'unlock-everything')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for mod in MODS:
        target = args.output / (mod + '.zip')
        with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_STORED) as archive:
            for name in ('manifest.json', 'main.lua'):
                info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
                info.create_system = 3
                info.external_attr = 0o100644 << 16
                archive.writestr(info, (ROOT / 'mods' / mod / name).read_bytes())
        data = target.read_bytes()
        print(f'{target} {len(data)} bytes sha256={hashlib.sha256(data).hexdigest()}')


if __name__ == '__main__':
    main()
