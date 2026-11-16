#!/usr/bin/env python3
"""Generate pinned scene-category manifests from local Steam and Android data.

Intermediate and output data are game-derived and must remain private.
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


def check(path, expected):
    with path.open('rb') as stream:
        if hashlib.file_digest(stream, 'sha256').hexdigest() != expected:
            raise ValueError('pinned input differs: '+path.name)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('steam-data', 'old-obb', 'particle-obb', 'reference-obb', 'output'):
        parser.add_argument('--'+name, required=True, type=Path)
    args = parser.parse_args()
    steam, old, particle, reference = (p.resolve(strict=True) for p in (args.steam_data, args.old_obb, args.particle_obb, args.reference_obb))
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not all(p.is_file() for p in (old, particle, reference)) or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (steam, old, particle, reference, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    for path, digest in ((old, 'baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d'),
                         (particle, 'bada442aa38b729666292968dac5b7771eeb620e045782356d7a1cf3ea9b7176'),
                         (reference, 'baf36d26d1ed49517c68ce02f92ceb12a29ea7971185ac3ac7db2182d8ade7fd')):
        check(path, digest)
    with tempfile.TemporaryDirectory(prefix='.openbonk-scene-manifests-', dir=output.parent) as directory:
        tmp = Path(directory)
        particles, pointers, suffixes = tmp/'particles.json', tmp/'pointers.json', tmp/'suffixes.json'
        helper.run('analysis/data-conversion/inventory-scene-particles.py', steam, old, particles)
        helper.run('analysis/data-conversion/study-scene-particle-pointers.py', steam, old, particles, pointers)
        helper.run('analysis/data-conversion/inventory-scene-component-suffixes.py', steam, reference, suffixes)
        tasks = (
            ('scene-particle-bodies-template', 'scene-particle-bodies-template.py', old, particles,
             '733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be', ['--old-split-obb']),
            ('scene-particle-references-template', 'scene-particle-references-template.py', particle, pointers,
             'cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0', []),
            ('scene-monobehaviour-bodies-template', 'scene-monobehaviour-bodies-template.py', reference, suffixes,
             'b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4', []),
        )
        for folder, script, source, inventory, expected, extra in tasks:
            helper.run('analysis/data-conversion/'+script, 'template', steam, source, tmp/folder, '--inventory', inventory, *extra)
            check(tmp/folder/'manifest.json', expected)
        output.mkdir(mode=0o700)
        try:
            for folder, *_ in tasks:
                (output/folder).mkdir(mode=0o700)
                shutil.copyfile(tmp/folder/'manifest.json', output/folder/'manifest.json')
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated three pinned scene category manifests; keep them private.')


if __name__ == '__main__':
    main()
