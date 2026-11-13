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


def pinned(path, expected):
    if path.is_symlink() or not path.is_file() or hashlib.sha256(path.read_bytes()).hexdigest() != expected:
        raise ValueError('invalid conversion input: ' + path.name)


def package(source, output, shader_dir=None):
    if not source.is_dir() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        raise ValueError('source must exist and output must be a new file')
    files = {path.name: path for path in source.iterdir()}
    for name, expected in METADATA_PINS.items():
        path = files.get(name, METADATA_DIR / name)
        pinned(path, expected)
        files[name] = path
    if shader_dir is not None:
        for name, expected in SHADER_PINS.items():
            path = shader_dir / name
            pinned(path, expected)
            files[name] = path
    if len(files) != 50 or any(not path.is_file() or path.is_symlink() for path in files.values()):
        raise ValueError('expected 50 regular local conversion inputs')
    with zipfile.ZipFile(output, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, path in sorted(files.items()):
            archive.write(path, name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--shader-dir', type=Path)
    args = parser.parse_args()
    package(args.source, args.output, args.shader_dir)


if __name__ == '__main__':
    main()
