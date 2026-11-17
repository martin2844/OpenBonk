#!/usr/bin/env python3
"""Bind a research reconstruction plan to a byte-different, entry-identical ZIP.

Inputs contain game data and stay local. The output plan contains only metadata.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import zipfile

ROOT = Path(__file__).resolve().parent.parent


def hash_stream(stream):
    return hashlib.file_digest(stream, 'sha256').hexdigest()


def hash_file(path):
    with path.open('rb') as stream:
        return hash_stream(stream)


LOCALIZATION = re.compile(r'assets/aa/Android/localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\.bundle\Z')


def localization_equivalent(a, b):
    """Compare serialized CABs and relevant UnityFS metadata, not raw bundles."""
    import UnityPy
    def identity(data):
        env = UnityPy.load(data)
        if len(env.files) != 1: raise ValueError('unexpected localization bundle')
        bundle = next(iter(env.files.values()))
        if bundle.signature != 'UnityFS' or bundle.version not in (7, 8) or bundle.version_engine != '2023.2.22f1' or len(bundle.files) != 1:
            raise ValueError('unsupported localization bundle')
        name, cab = next(iter(bundle.files.items()))
        if not name.startswith('CAB-') or not hasattr(cab, 'reader'): raise ValueError('invalid localization CAB')
        return bundle.version, bundle.version_player, bundle.version_engine, name, cab.flags, bytes(cab.reader.bytes)
    return identity(a) == identity(b)


def references_localization(value):
    if isinstance(value, dict):
        if isinstance(value.get('entry'), str) and LOCALIZATION.fullmatch(value['entry']): return True
        return any(references_localization(v) for v in value.values())
    if isinstance(value, list): return any(references_localization(v) for v in value)
    return False


def rebind(plan, original, canonical, output, normalize_localization=False):
    data = json.loads(plan.read_text())
    if (data.get('schema') != 1 or data.get('kind') not in ('experimental-steam-audio-offsets', 'experimental-packed-steam-audio-offsets', 'experimental-converted-steam-texture-offsets', 'experimental-steam-mesh-buffer-offsets', 'experimental-converted-steam-cubemap-offsets', 'experimental-animation-fragment-mask', 'experimental-exact-steam-object-offsets', 'experimental-partial-rgba-texture-offsets') or
            data.get('template_sha256') != hash_file(original)):
        raise ValueError('plan does not match original template')
    if normalize_localization and references_localization(data.get('records')):
        raise ValueError('plan modifies Android localization bundles')
    with zipfile.ZipFile(original) as a, zipfile.ZipFile(canonical) as b:
        names = a.namelist()
        if len(names) != len(set(names)) or names != b.namelist():
            raise ValueError('archive entry lists differ')
        for name in names:
            with a.open(name) as x, b.open(name) as y:
                if hash_stream(x) != hash_stream(y):
                    if not normalize_localization or not LOCALIZATION.fullmatch(name) or a.getinfo(name).file_size > 32*1024*1024 or b.getinfo(name).file_size > 32*1024*1024 or not localization_equivalent(a.read(name), b.read(name)):
                        raise ValueError('archive content differs')
    data['template_sha256'] = hash_file(canonical)
    with output.open('x', encoding='utf-8') as target:
        json.dump(data, target, separators=(',', ':'))
        target.write('\n')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--plan', required=True, type=Path)
    p.add_argument('--original-template', required=True, type=Path)
    p.add_argument('--canonical-template', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    p.add_argument('--normalize-localization', action='store_true', help='allow equivalent normalized Android localization bundles after checking their CAB and metadata')
    args = p.parse_args()
    inputs = [x.expanduser().resolve(strict=True) for x in (args.plan, args.original_template, args.canonical_template)]
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (*inputs, ROOT)): p.error('output must be separate from inputs and source checkout')
    rebind(*inputs, output, normalize_localization=args.normalize_localization)
    print('Rebound plan after checking archive entries; no game data was copied to the plan.')


if __name__ == '__main__':
    main()
