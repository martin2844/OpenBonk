#!/usr/bin/env python3
"""Generate eight legacy hashed-resource manifests from local Steam and OBB data.

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
    ('hashed-exact-textures-template', 'hashed-exact-textures-prototype.py', '72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9'),
    ('hashed-matched53-template', 'hashed-matched-textures-prototype.py', '7e4bd817c3138434082496ba797c5ef216798357b7f8ef7bc6ecf1fed02061a7'),
    ('hashed-excluded-rgba-template', 'hashed-excluded-rgba-template.py', '828fde47398ad39c9a0a42d2afba85111dd577f704f3fd5740c4ddcb5e79c76d'),
    ('hashed-extra-candidates-template', 'hashed-extra-candidates-template.py', '34e87d8502957407489faca56ae44db02c1ed7d7eed9b2363ffdb44f44504f44'),
    ('hashed-final-candidates-template', 'hashed-final-candidates-template.py', 'e2d7f81ff5d42db9b8e698b063212e3abf0cffdcbe27d12737fa78739370e6cd'),
    ('hashed-audio-template', 'hashed-audio-template.py', '6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f'),
)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--steam-data', required=True, type=Path)
    parser.add_argument('--expanded-obb', required=True, type=Path)
    parser.add_argument('--include-private-templates', action='store_true', help='retain regenerated template payloads alongside the manifests')
    parser.add_argument('--output', required=True, type=Path, help='new private directory')
    args = parser.parse_args()
    steam, obb = args.steam_data.resolve(strict=True), args.expanded_obb.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if not steam.is_dir() or not obb.is_file() or output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('inputs must exist and output must be new')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(path) or path.is_relative_to(output) for path in (steam, obb, ROOT)):
        parser.error('output must be separate from checkout and inputs')
    with obb.open('rb') as stream:
        if hashlib.file_digest(stream, 'sha256').hexdigest() not in ('ca049f82bb20220dd34ef636938f95e0f09ef0bdf4a0bf59795e0b03b87b9ce3', 'baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d'):
            raise ValueError('incompatible expanded shared OBB')
    with tempfile.TemporaryDirectory(prefix='.openbonk-hashed-manifests-', dir=output.parent) as directory:
        tmp = Path(directory)
        comparison, audio, unmatched, study = (tmp/'comparison.json', tmp/'audio.json', tmp/'unmatched.json', tmp/'study.json')
        helper.run('analysis/data-conversion/compare-hashed-textures.py', steam, obb, comparison)
        helper.run('analysis/data-conversion/inventory-hashed-audio.py', steam, obb, audio)
        helper.run('analysis/data-conversion/investigate-unmatched-hashed-textures.py', steam, obb, unmatched, comparison)
        helper.run('analysis/data-conversion/study-unmatched-hashed-mips.py', steam, obb, unmatched, study)
        for path, expected in ((comparison, '886affe779061c77fd857434b19b9ff2e267a8788801448a346c3d11acb17aae'),
                               (audio, 'edfeb7a5b75bd70c9d5a0ae1fc6b6606e7ca89eec989d3d121198544b0dc1ba0'),
                               (study, '2d88b1e912c887c7d7bbbe4b354d5eb1c4d537808bbde929d3363ff156833b79')):
            if hashlib.sha256(path.read_bytes()).hexdigest() != expected:
                raise ValueError('generated comparison differs: '+path.name)
        for folder, script, expected in TASKS:
            generated = tmp/folder
            extra = (['--comparison', comparison] if folder in ('hashed-exact-textures-template', 'hashed-matched53-template', 'hashed-excluded-rgba-template')
                     else ['--inventory', audio] if folder == 'hashed-audio-template'
                     else ['--study', study] if folder == 'hashed-extra-candidates-template' else [])
            helper.run('analysis/data-conversion/'+script, 'template', steam, obb, generated, '--expanded-shared-obb', *extra)
            if hashlib.sha256((generated/'manifest.json').read_bytes()).hexdigest() != expected:
                raise ValueError('generated manifest differs: '+folder)
        output.mkdir(mode=0o700)
        try:
            for folder, _, _ in TASKS:
                if args.include_private_templates:
                    shutil.copytree(tmp/folder, output/folder)
                else:
                    (output/folder).mkdir(mode=0o700)
                    shutil.copyfile(tmp/folder/'manifest.json', output/folder/'manifest.json')
        except BaseException:
            shutil.rmtree(output)
            raise
    print('Generated eight pinned legacy manifests; keep them private.')


if __name__ == '__main__':
    main()
