#!/usr/bin/env python3
"""Bind a research reconstruction plan to a byte-different, entry-identical ZIP.

Inputs contain game data and stay local. The output plan contains only metadata.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parent.parent


def hash_stream(stream):
    return hashlib.file_digest(stream, 'sha256').hexdigest()


def hash_file(path):
    with path.open('rb') as stream:
        return hash_stream(stream)


def rebind(plan, original, canonical, output):
    data = json.loads(plan.read_text())
    if (data.get('schema') != 1 or data.get('kind') not in ('experimental-steam-audio-offsets', 'experimental-packed-steam-audio-offsets', 'experimental-converted-steam-texture-offsets', 'experimental-steam-mesh-buffer-offsets', 'experimental-converted-steam-cubemap-offsets') or
            data.get('template_sha256') != hash_file(original)):
        raise ValueError('plan does not match original template')
    with zipfile.ZipFile(original) as a, zipfile.ZipFile(canonical) as b:
        names = a.namelist()
        if len(names) != len(set(names)) or names != b.namelist():
            raise ValueError('archive entry lists differ')
        for name in names:
            with a.open(name) as x, b.open(name) as y:
                if hash_stream(x) != hash_stream(y):
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
    args = p.parse_args()
    inputs = [x.expanduser().resolve(strict=True) for x in (args.plan, args.original_template, args.canonical_template)]
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (*inputs, ROOT)): p.error('output must be separate from inputs and source checkout')
    rebind(*inputs, output)
    print('Rebound plan to entry-identical canonical template; no game data was copied to the plan.')


if __name__ == '__main__':
    main()
