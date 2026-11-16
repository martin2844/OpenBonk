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
    for name in ('steam-data', 'old-obb', 'output'):
        parser.add_argument('--'+name, required=True, type=Path)
    parser.add_argument('--particle-obb', type=Path)
    parser.add_argument('--reference-obb', type=Path)
    parser.add_argument('--old-apk', type=Path, help='regenerate both intermediate OBBs using this pinned APK')
    args = parser.parse_args()
    if bool(args.old_apk) == bool(args.particle_obb or args.reference_obb) or (not args.old_apk and not (args.particle_obb and args.reference_obb)):
        parser.error('provide --old-apk or both --particle-obb and --reference-obb')
    steam, old = (p.resolve(strict=True) for p in (args.steam_data, args.old_obb))
    apk = args.old_apk.resolve(strict=True) if args.old_apk else None
    particle = args.particle_obb.resolve(strict=True) if args.particle_obb else None
    reference = args.reference_obb.resolve(strict=True) if args.reference_obb else None
    output = args.output.expanduser().absolute()
    inputs = (old, *([apk] if apk else [particle, reference]))
    if not steam.is_dir() or not all(p.is_file() for p in inputs) or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (steam, *inputs, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    for path, digest in ((old, 'baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d'),):
        check(path, digest)
    if apk:
        check(apk, '850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5')
    with tempfile.TemporaryDirectory(prefix='.openbonk-scene-manifests-', dir=output.parent) as directory:
        tmp = Path(directory)
        particles, pointers, suffixes = tmp/'particles.json', tmp/'pointers.json', tmp/'suffixes.json'
        helper.run('analysis/data-conversion/inventory-scene-particles.py', steam, old, particles)
        helper.run('analysis/data-conversion/study-scene-particle-pointers.py', steam, old, particles, pointers)
        particle_template = tmp/'scene-particle-bodies-template'
        helper.run('analysis/data-conversion/scene-particle-bodies-template.py', 'template', steam, old, particle_template,
                   '--inventory', particles, '--old-split-obb')
        check(particle_template/'manifest.json', '733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be')
        if apk:
            hashed, blank = tmp/'hashed', tmp/'blank.apk'
            helper.run('tools/generate-legacy-hashed-manifests.py', '--steam-data', steam, '--expanded-obb', old,
                       '--include-private-templates', '--output', hashed)
            helper.run('analysis/port-inventory/scrub-base-apk-prototype.py', apk, blank)
            options = []
            for flag, folder in (('--hashed-mesh-template','hashed-mesh-template'),
                                 ('--hashed-animation-template','hashed-animation-template'),
                                 ('--hashed-texture-template','hashed-exact-textures-template'),
                                 ('--hashed-matched-texture-template','hashed-matched53-template'),
                                 ('--hashed-rgba-template','hashed-excluded-rgba-template'),
                                 ('--hashed-extra-template','hashed-extra-candidates-template'),
                                 ('--hashed-final-template','hashed-final-candidates-template'),
                                 ('--hashed-audio-template','hashed-audio-template')):
                options.extend((flag, hashed/folder))
            particle_base = tmp/'particle-base'
            helper.run('analysis/port-inventory/scrub-base-pair-prototype.py', blank, old, particle_base,
                       *options, '--scene-particle-template', particle_template)
            particle = particle_base/'base.obb'
        check(particle, 'bada442aa38b729666292968dac5b7771eeb620e045782356d7a1cf3ea9b7176')
        reference_template = tmp/'scene-particle-references-template'
        helper.run('analysis/data-conversion/scene-particle-references-template.py', 'template', steam, particle,
                   reference_template, '--inventory', pointers)
        check(reference_template/'manifest.json', 'cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0')
        if apk:
            reference_base = tmp/'reference-base'
            helper.run('analysis/port-inventory/scrub-base-pair-prototype.py', blank, old, reference_base,
                       *options, '--scene-particle-template', particle_template,
                       '--scene-particle-references-template', reference_template)
            reference = reference_base/'base.obb'
        check(reference, 'baf36d26d1ed49517c68ce02f92ceb12a29ea7971185ac3ac7db2182d8ade7fd')
        helper.run('analysis/data-conversion/inventory-scene-component-suffixes.py', steam, reference, suffixes)
        mono = tmp/'scene-monobehaviour-bodies-template'
        helper.run('analysis/data-conversion/scene-monobehaviour-bodies-template.py', 'template', steam, reference, mono,
                   '--inventory', suffixes)
        check(mono/'manifest.json', 'b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4')
        output.mkdir(mode=0o700)
        try:
            for folder in ('scene-particle-bodies-template', 'scene-particle-references-template', 'scene-monobehaviour-bodies-template'):
                (output/folder).mkdir(mode=0o700)
                shutil.copyfile(tmp/folder/'manifest.json', output/folder/'manifest.json')
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated three pinned scene category manifests; keep them private.')


if __name__ == '__main__':
    main()
