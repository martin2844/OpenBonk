#!/usr/bin/env python3
"""Regenerate the legacy localization inventory from the two Steam depots.

The resulting file contains metadata and hashes, not locale payloads. The
Thai binary patch can be generated separately by generate-thai-localization-patch.py.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('windows_localization', ROOT / 'tools/normalize-windows-localization.py')
normalizer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(normalizer)
THAI = 'localization-string-tables-thai(th)_assets_all.bundle'


def build_inventory(inventory, linux, windows, patch_hash):
    platforms = {'linux': ('StandaloneLinux64', linux), 'windows': ('StandaloneWindows64', windows)}
    tables = {}
    for platform, (folder, root) in platforms.items():
        prefix = f'StreamingAssets/aa/{folder}/'
        entries = inventory[platform]
        names = {n[len(prefix):] for n in entries if n.startswith(prefix + 'localization-')}
        if len(names) != 18 or THAI not in names:
            raise ValueError('unexpected localization inventory')
        tables[platform] = (prefix, entries, root, names)
    if tables['linux'][3] != tables['windows'][3]:
        raise ValueError('localization inventory names differ')
    rows = []
    for name in sorted(tables['linux'][3]):
        data = {}
        for platform, (prefix, entries, root, _) in tables.items():
            path = root / prefix / name
            if not path.is_file() or not path.resolve().is_relative_to(root):
                raise ValueError('missing localization input: ' + name)
            raw = path.read_bytes()
            pinned = entries[prefix + name]
            if len(raw) != pinned['size'] or hashlib.sha256(raw).hexdigest() != pinned['sha256']:
                raise ValueError('unsupported localization input: ' + name)
            data[platform] = raw
        left, right = data['windows'], data['linux']
        if name == THAI:
            row = {'mode': 'bsdiff', 'patch_sha256': patch_hash}
        else:
            if len(left) != len(right):
                raise ValueError('unexpected localization size difference: ' + name)
            offsets = [i for i, (a, b) in enumerate(zip(left, right)) if a != b]
            if len(offsets) != 1:
                raise ValueError('unexpected localization byte differences: ' + name)
            row = {'mode': 'platform_byte', 'offset': offsets[0]}
        row.update(name=name, windows_size=len(left), windows_sha256=hashlib.sha256(left).hexdigest(),
                   linux_size=len(right), linux_sha256=hashlib.sha256(right).hexdigest())
        rows.append(row)
    return {'kind': 'local research only, not for distribution', 'build_id': '21750826', 'bundles': rows}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--linux-data', type=Path, required=True, help='Linux Megabonk_Data directory')
    p.add_argument('--windows-data', type=Path, required=True, help='Windows Megabonk_Data directory')
    p.add_argument('--output', type=Path, required=True, help='new local inventory file')
    a = p.parse_args()
    linux, windows = a.linux_data.resolve(strict=True), a.windows_data.resolve(strict=True)
    output = a.output.expanduser().absolute()
    if not linux.is_dir() or not windows.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        p.error('input directories must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (linux, windows, ROOT)):
        p.error('output must be separate from the checkout and inputs')
    inventory = json.loads((ROOT / 'tools/launcher-steam-inputs.json').read_text())['platforms']
    document = build_inventory(inventory, linux, windows, normalizer.PATCH_SHA)
    raw = (json.dumps(document, indent=2) + '\n').encode()
    if hashlib.sha256(raw).hexdigest() != normalizer.MANIFEST_SHA:
        raise ValueError('generated localization inventory differs from the pinned version')
    with output.open('xb') as stream:
        stream.write(raw)
    print('Generated pinned localization inventory; the Thai patch is not included.')


if __name__ == '__main__':
    main()
