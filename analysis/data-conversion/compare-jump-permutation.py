#!/usr/bin/env python3
"""Study one private Jump clip after a hypothesized permutation of three curve groups."""
import argparse
import hashlib
import importlib.util
import math
from pathlib import Path
import zipfile
import UnityPy

PC_HASH = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
TARGET_HASH = 'cbf2c639f6708ee055b6e392e37fd762bfbbe5c7642d743471bf0418f23ed8d1'
ENTRY = 'assets/bin/Data/b77dbb5018582534a8ef8e8a1a61cf65'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('pc_asset', type=Path)
    parser.add_argument('android_obb', type=Path)
    args = parser.parse_args()
    pc_data = args.pc_asset.read_bytes()
    if sha(pc_data) != PC_HASH:
        raise ValueError('unexpected Steam asset')
    with zipfile.ZipFile(args.android_obb) as archive:
        if archive.namelist().count(ENTRY) != 1:
            raise ValueError('target entry missing or duplicated')
        target = archive.read(ENTRY)
    if sha(target) != TARGET_HASH:
        raise ValueError('unexpected Android clip')
    pc = next(o for o in UnityPy.load(pc_data).objects if o.path_id == 1994).read_typetree()
    android = UnityPy.load(target).objects[0].read_typetree()
    pc_bindings = pc['m_ClipBindingConstant']['genericBindings']
    android_bindings = android['m_ClipBindingConstant']['genericBindings']
    permutation = list(range(35))
    permutation[30:33] = [32, 30, 31]
    if (pc['m_Name'] != 'Jump' or android['m_Name'] != 'Jump' or len(pc_bindings) != 35
            or len(android_bindings) != 35
            or any(pc_bindings[i] != android_bindings[permutation[i]] for i in range(35))):
        raise ValueError('bindings do not match the hypothesized permutation')
    spec = importlib.util.spec_from_file_location('curves', Path(__file__).with_name('analyze-shared2-animation-streams.py'))
    curves = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(curves)
    def frames(tree):
        return curves.frames(tree['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'])
    def remap(index):
        return index + 6 if 95 <= index <= 97 else index - 3 if 98 <= index <= 103 else index
    a, b = frames(pc), frames(android)
    if len(a) != len(b):
        raise ValueError('frame count differs')
    samples, worst = 0, 0.0
    for i, ((time, keys), (other_time, other_keys)) in enumerate(zip(a, b)):
        mapped = {remap(index): value for index, value in keys}
        other = dict(other_keys)
        if time != other_time or len(mapped) != len(keys) or mapped.keys() != other.keys():
            raise ValueError('frame or curve mapping differs after permutation')
        if i + 1 == len(a):
            continue
        dt = a[i+1][0] - time
        if time < 0 or not math.isfinite(dt) or dt <= 0 or dt > 60:
            continue
        for index, value in mapped.items():
            for fraction in (0, .25, .5, .75, 1):
                worst = max(worst, abs(curves.evaluate(value, dt*fraction) - curves.evaluate(other[index], dt*fraction)))
                samples += 1
    print(f'bindings=35 permuted=3 frames={len(a)} sampled_values={samples} maximum_curve_difference={worst:.12g}')
    print('This tests a hypothesized channel permutation and sampled serialized curves, not Unity poses or equivalence.')


if __name__ == '__main__':
    main()
