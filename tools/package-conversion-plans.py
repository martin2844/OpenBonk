#!/usr/bin/env python3
"""Package the pinned local conversion inputs for the launcher prototype."""
import argparse
import hashlib
from pathlib import Path
import zipfile


ROOT = Path(__file__).resolve().parent.parent
METADATA_DIR = ROOT / 'analysis/data-conversion/metadata-plans'
METADATA_PINS = {
    'exact.bin': '052c62fc74526a35408e24ea1e32bcf9af7bd9b7f40bf47423bcdedee288b992',
    'audio.bin': 'ae6023707a925ce13f6b222adcc691906f0e161c4d47c026a7ddf0fdad4c206a',
}
SHADER_PINS = {
    'builtin-shader-pack.zip': 'a5547ee588ac42c825e10d74443f84ce2a17981de9560fd7b993e5da4a38c079',
    'shared1-shader-pack.zip': '96b0ea3bc74303b5b4455b1a99e4d875b4d9611af03368b1e39bf79138f3ce7b',
}
EXPECTED_NAMES = frozenset('''
animation-direct.bin animation-private.bin audio.bin builtin-shaders.bin converted.bin exact.bin
extra-derived.bin localization-obb.bin localization.bin map.bin mesh.bin partial.bin preload.bin scene.bin
shared-audio1.bin shared-audio2.bin shared-audio4.bin shared0-atlas.bin shared0-final.bin shared0-font.bin
shared0-metadata.bin shared1-alias.bin shared1-alpha.bin shared1-animation.bin shared1-bc-rgba.bin
shared1-converted.bin shared1-cubemap.bin shared1-etc.bin shared1-exact.bin shared1-final.bin
shared1-large.bin shared1-last.bin shared1-layout.bin shared1-matched.bin shared1-material.bin
shared1-mesh-flags.bin shared1-mesh-metadata.bin shared1-mesh-stream.bin shared1-mesh.bin
shared1-normal.bin shared1-outline.bin shared1-renamed.bin shared1-rgba.bin shared1-shaders.bin
shared2-animation.bin shared2-final.bin shared2-mesh.bin shared2-texture.bin
builtin-shader-pack.zip shared1-shader-pack.zip
'''.split())


def pinned(path, expected):
    if path.is_symlink() or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
        raise ValueError('invalid conversion input: ' + path.name)


def inputs(source, shader_dir=None):
    if not source.is_dir():
        raise ValueError('source directory must exist')
    files = {path.name: path for path in source.iterdir()}
    for name, expected in METADATA_PINS.items():
        path = files.get(name, METADATA_DIR / name)
        pinned(path, expected)
        files[name] = path
    for name, expected in SHADER_PINS.items():
        path = shader_dir / name if shader_dir is not None else files.get(name, source / name)
        pinned(path, expected)
        files[name] = path
    if set(files) != EXPECTED_NAMES or any(not path.is_file() or path.is_symlink() for path in files.values()):
        raise ValueError('conversion input names differ from the expected 50 files')
    return files


def package(source, output, shader_dir=None):
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        raise ValueError('output must be a new file')
    files = inputs(source, shader_dir)
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, path in sorted(files.items()):
            archive.write(path, name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path, nargs='?')
    parser.add_argument('--shader-dir', type=Path)
    parser.add_argument('--check', action='store_true', help='validate inputs without writing output')
    args = parser.parse_args()
    if args.check:
        if args.output is not None:
            parser.error('do not supply output with --check')
        print(len(inputs(args.source, args.shader_dir)), 'conversion inputs found; shader and public metadata hashes verified')
    elif args.output is None:
        parser.error('output is required unless --check is used')
    else:
        package(args.source, args.output, args.shader_dir)


if __name__ == '__main__':
    main()
