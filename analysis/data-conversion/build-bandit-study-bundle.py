#!/usr/bin/env python3
"""Create a local Unity study bundle using the verified Steam Bandit clip."""
import argparse
import hashlib
from pathlib import Path
import UnityPy

PC_HASH = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
ANDROID_CLIP = '5bf11b0a493558aa4bb780170acade854601fde7e3e3fbffcc3b292122d7cb82'
STEAM_CLIP = 'edcc64ad973957fe19981a82d60325523839127e0f39e0846e8044958e9eabd0'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('bundle', type=Path)
    parser.add_argument('pc_asset', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    if args.output.exists() or args.output.is_symlink() or not args.output.parent.is_dir():
        parser.error('output must be new in an existing directory')
    data = args.pc_asset.read_bytes()
    if sha(data) != PC_HASH:
        raise ValueError('unexpected Steam asset')
    source = next(o for o in UnityPy.load(data).objects if o.path_id == 1994)
    raw = source.get_raw_data()
    if source.type.name != 'AnimationClip' or sha(raw) != STEAM_CLIP:
        raise ValueError('unexpected Steam clip')
    bundle = UnityPy.load(str(args.bundle))
    clips = [o for o in bundle.objects if o.type.name == 'AnimationClip' and sha(o.get_raw_data()) == ANDROID_CLIP]
    if len(clips) != 1 or clips[0].byte_size != len(raw):
        raise ValueError('unexpected reference bundle clip')
    target_id = clips[0].path_id
    before = {(o.type.name, o.path_id): sha(o.get_raw_data()) for o in bundle.objects}
    clips[0].set_raw_data(raw)
    result = bundle.file.save(packer='none')
    check = {(o.type.name, o.path_id): sha(o.get_raw_data()) for o in UnityPy.load(result).objects}
    before[('AnimationClip', target_id)] = STEAM_CLIP
    if check != before:
        raise ValueError('bundle verification failed')
    with args.output.open('xb') as output:
        output.write(result)
    print('Private study bundle created; sha256', sha(result))


if __name__ == '__main__':
    main()
