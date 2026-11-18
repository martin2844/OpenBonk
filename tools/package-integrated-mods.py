#!/usr/bin/env python3
"""Build byte-reproducible launcher ZIPs for the independently written mods."""
import argparse
import hashlib
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[1]
MODS = ('auto-pots', 'item-lab', 'lucky-tome')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    for mod in MODS:
        target = args.output / (mod + '.zip')
        with zipfile.ZipFile(target, 'w') as archive:
            for name in ('manifest.json', 'main.lua'):
                info = zipfile.ZipInfo(name, (1980, 1, 1, 0, 0, 0))
                info.create_system = 3
                info.external_attr = 0o100644 << 16
                info.compress_type = zipfile.ZIP_STORED
                archive.writestr(info, (ROOT / 'mods' / mod / name).read_bytes())
        print(hashlib.sha256(target.read_bytes()).hexdigest(), target)


if __name__ == '__main__':
    main()
