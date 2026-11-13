#!/usr/bin/env python3
"""Collect pinned reconstruction manifests; local research only.

These files contain game-derived metadata and are not a public distribution.
"""
import hashlib
from pathlib import Path
import shutil

PINS = {'all-cubemaps-template/cubemaps.json': 'c88c880339f06b269fad6b5606f1118b000321f2141040e685cadcf0f9d668d3',
 'alpha8-template/manifest.json': 'c303ad6169e2fe7a9a459f19a485f4dc3992ab7b4911f8507c8309ec68020852',
 'bc-etc-template/manifest.json': '779a342d550dd4d703b5700cd382d55bfd5e1e0a7db214e580f7ec0b9cab1c3f',
 'bc-rgba-template/manifest.json': 'c0b287bee5928fd5fbc4cc36c430f6c9d2cab43b99e0a9e2dc71906f7638280c',
 'direct-texture-template/manifest.json': '3bcf4a4ff496fcdac1520384bd30c49d671e9944b4ec5e7e23cc9e23ae5fe100',
 'etc-mip-template/manifest.json': '64b2c54efaebfc6fafa4b3cdf19e880d0a0067ec32e25b034c22c38ad274c0e0',
 'full-cubemaps-stripped/mesh-manifest.json': 'bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3',
 'full-exact-stripped/exact-renamed.json': '37f2efb47b603148210cd66887b0a57f2c5ad12fd3049ea71d19642531b86984',
 'hashed-audio-template/manifest.json': '6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f',
 'hashed-exact-textures-template/manifest.json': '72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9',
 'hashed-excluded-rgba-template/manifest.json': '828fde47398ad39c9a0a42d2afba85111dd577f704f3fd5740c4ddcb5e79c76d',
 'hashed-extra-candidates-template/manifest.json': '34e87d8502957407489faca56ae44db02c1ed7d7eed9b2363ffdb44f44504f44',
 'hashed-final-candidates-template/manifest.json': 'e2d7f81ff5d42db9b8e698b063212e3abf0cffdcbe27d12737fa78739370e6cd',
 'hashed-matched53-template/manifest.json': '7e4bd817c3138434082496ba797c5ef216798357b7f8ef7bc6ecf1fed02061a7',
 'hashed-mesh-template/manifest.json': '44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24',
 'large-template/manifest.json': '084a8c562538e518bb9bb660d8a8632cc9a262d9a1bd0153b65363a80520ebd8',
 'matched-template/matched.json': 'bcf2bfa4012f9344fcd8e0a22753f6e0e5dcc014dbe2dcad2abe881233ecc075',
 'normal-template/manifest.json': '9523e3d7e2a9aefc74d8c805acc27b861796b701b74926a227f0ae4279231720',
 'rgba-template/manifest.json': 'abbad059169083017f36b6c560faed2ad88c92a17d6b4bc8f76d9816d5327750',
 'scene-monobehaviour-bodies-template/manifest.json': 'b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4',
 'scene-particle-bodies-template/manifest.json': '733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be',
 'scene-particle-references-template/manifest.json': 'cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0',
 'shared0-atlas-template/atlas-manifest.json': '269da944e556073d7b670e6025653c7e02929e115c12f25776c72043346e431d',
 'shared0-font-template/font-manifest.json': '4fd3e38f6c110ebcaff62e5e74da6df53678651f9c3e5a5530670a71c6502272',
 'shared1-last-textures-template/textures-manifest.json': '721bfcb5ee20eb0e713f6ae2c61e21a72ed016d1cb60d8279151b9f2f41e2c63',
 'shared1-terrain-animation-template/objects-manifest.json': '9641811a775b44f61ab6b2148ceb384ea6fa2155acd99a5d8fc44449402a1259',
 'shared2-atlases-template/atlases-manifest.json': 'cad14caa26c75e5dce4efc66f05be00517ac0281f25314f8709ed878dc7f3080',
 'shared2-matched72-template/textures-manifest.json': '9df05620261b30b27f3b64ed050ba3dfdd2fe087087b2c1993974e005048386e'}


def verify(root):
    root = Path(root)
    for name, expected in PINS.items():
        path = root/name
        if not path.is_file() or path.stat().st_size > 2*1024*1024:
            raise ValueError('missing or oversized manifest: '+str(path))
        with path.open('rb') as f:
            if hashlib.file_digest(f, 'sha256').hexdigest() != expected:
                raise ValueError('incompatible manifest: '+str(path))


def collect(source, output):
    source, output = Path(source).resolve(), Path(output).absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        raise ValueError('output must be new')
    if output.resolve().is_relative_to(source) or source.is_relative_to(output.resolve()):
        raise ValueError('output must be separate from input')
    verify(source)
    output.mkdir(mode=0o700)
    try:
        for name in PINS:
            target = output/name; target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(source/name, target)
        verify(output)
    except BaseException:
        shutil.rmtree(output); raise


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path); parser.add_argument('output', type=Path)
    args = parser.parse_args()
    collect(args.source, args.output)
    print(f'Collected {len(PINS)} pinned manifests; local research only.')
