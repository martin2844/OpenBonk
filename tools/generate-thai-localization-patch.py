#!/usr/bin/env python3
"""Recreate the pinned local Thai localization patch from two Steam depots.

The patch contains game-derived data and must remain private.
"""
import argparse
import bz2
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
NAME = 'localization-string-tables-thai(th)_assets_all.bundle'
PATCH_SHA = 'e80b9e69d671b853cb8a4207ab2a93777619a14add8c761530ebcc0fee7c34a5'


def signed(value):
    return (abs(value) | ((1 << 63) if value < 0 else 0)).to_bytes(8, 'little')


def make_patch(old, new):
    # These ranges reproduce the BSDIFF40 control stream used by the beta.
    if len(old) != 34973 or len(new) != 34972:
        raise ValueError('unexpected Thai bundle sizes')
    control = b''.join(signed(value) for value in (11903, 3, 4, 23066, 0, -23066))
    diff = bytes((new[i] - old[i]) & 255 for i in range(11903))
    diff += bytes((new[11906 + i] - old[11907 + i]) & 255 for i in range(23066))
    control, diff, extra = (bz2.compress(value, compresslevel=9)
                            for value in (control, diff, new[11903:11906]))
    return b'BSDIFF40' + signed(len(control)) + signed(len(diff)) + signed(len(new)) + control + diff + extra


def load_input(root, platform, inventory):
    folder = {'linux': 'StandaloneLinux64', 'windows': 'StandaloneWindows64'}[platform]
    relative = f'StreamingAssets/aa/{folder}/{NAME}'
    path = root / relative
    if not path.is_file() or not path.resolve().is_relative_to(root):
        raise ValueError('missing or unsafe Thai bundle: ' + platform)
    expected = inventory[platform][relative]
    if path.stat().st_size != expected['size']:
        raise ValueError('unsupported Thai bundle: ' + platform)
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected['sha256']:
        raise ValueError('unsupported Thai bundle: ' + platform)
    return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--linux-data', required=True, type=Path)
    parser.add_argument('--windows-data', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path, help='new private patch file')
    args = parser.parse_args()
    linux, windows = args.linux_data.resolve(strict=True), args.windows_data.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not linux.is_dir() or not windows.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('input directories must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (linux, windows, ROOT)):
        parser.error('output must be separate from the checkout and inputs')
    inventory = json.loads((ROOT / 'tools/launcher-steam-inputs.json').read_text())['platforms']
    old, new = load_input(windows, 'windows', inventory), load_input(linux, 'linux', inventory)
    patch = make_patch(old, new)
    if hashlib.sha256(patch).hexdigest() != PATCH_SHA:
        raise ValueError('generated patch differs from pinned beta patch')
    spec = importlib.util.spec_from_file_location('apply_bsdiff', ROOT / 'tools/apply-bsdiff-pinned.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    if module.apply(old, patch, len(new), hashlib.sha256(new).hexdigest()) != new:
        raise ValueError('generated patch did not restore Linux bundle')
    with output.open('xb') as stream:
        stream.write(patch)
    print('Generated and verified the pinned Thai patch; keep it private.')


if __name__ == '__main__':
    main()
