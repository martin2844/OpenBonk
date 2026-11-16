#!/usr/bin/env python3
"""Generate two legacy hashed-resource manifests from local Steam and OBB data.

The generated animation manifest contains game-derived patches; keep it private.
"""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import shutil
import tempfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('legacy_builder', ROOT/'tools/rebuild-legacy-shader-base.py')
helper = importlib.util.module_from_spec(spec)
spec.loader.exec_module(helper)
TASKS = (
    ('hashed-mesh-template', 'hashed-mesh-buffers-prototype.py', '44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24'),
    ('hashed-animation-template', 'hashed-animation-prototype.py', 'd906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8'),
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--expanded-obb', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path, help='new private directory')
    args = parser.parse_args()
    steam, obb = args.steam_data.resolve(strict=True), args.expanded_obb.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not obb.is_file() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (steam, obb, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    with tempfile.TemporaryDirectory(prefix='.openbonk-hashed-manifests-', dir=output.parent) as directory:
        tmp = Path(directory)
        for folder, script, expected in TASKS:
            generated = tmp/folder
            helper.run('analysis/data-conversion/'+script, 'template', steam, obb, generated, '--expanded-shared-obb')
            if hashlib.sha256((generated/'manifest.json').read_bytes()).hexdigest() != expected:
                raise ValueError('generated manifest differs: '+folder)
        output.mkdir(mode=0o700)
        try:
            for folder, _, _ in TASKS:
                (output/folder).mkdir(mode=0o700)
                shutil.copyfile(tmp/folder/'manifest.json', output/folder/'manifest.json')
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated both pinned legacy manifests; keep them private.')


if __name__ == '__main__':
    main()
