#!/usr/bin/env python3
"""Rebuild a pinned local scene base from Steam scenes and Android archives.

Requires three pinned scene category manifests. Output contains private data.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('legacy_builder', ROOT/'tools/rebuild-legacy-shader-base.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'apk', 'obb', 'manifest-root', 'output'):
        parser.add_argument('--'+name, required=True, type=Path)
    args = parser.parse_args()
    steam, apk, obb, manifests = (p.resolve(strict=True) for p in (args.steam_data, args.apk, args.obb, args.manifest_root))
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not apk.is_file() or not obb.is_file() or not manifests.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (steam, apk, obb, manifests, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    with tempfile.TemporaryDirectory(prefix='.openbonk-scene-base-', dir=output.parent) as directory:
        tmp = Path(directory)
        inventory, first, extra = tmp/'inventory.json', tmp/'first', tmp/'extra'
        helper.run('analysis/data-conversion/inventory-scene-component-suffixes.py', steam, obb, inventory)
        if hashlib.sha256(inventory.read_bytes()).hexdigest() != '61e581117f2fe2c87871090145acc40184af5603e32303a56bc636571a55bd64':
            raise ValueError('scene component inventory differs')
        helper.run('analysis/data-conversion/no-rewired-scene-bodies-prototype.py', 'template', steam, obb, first,
                   '--manifest-root', manifests)
        helper.run('analysis/data-conversion/no-rewired-scene-extra-mono-prototype.py', 'template', steam, first, extra,
                   '--inventory', inventory, '--first-dir', first)
        helper.run('analysis/port-inventory/scrub-no-rewired-scenes-prototype.py', apk, obb, output,
                   '--first-dir', first, '--extra-dir', extra)
    print('Rebuilt the pinned scene base; keep the output private.')


if __name__ == '__main__':
    main()
