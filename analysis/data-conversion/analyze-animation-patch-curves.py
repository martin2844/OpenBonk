#!/usr/bin/env python3
"""Characterize patched animation curves; report statistics, never curve values."""
import argparse
import collections
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import zipfile

import UnityPy

spec = importlib.util.spec_from_file_location('curve', Path(__file__).with_name('analyze-shared2-animation-streams.py'))
curve = importlib.util.module_from_spec(spec)
spec.loader.exec_module(curve)
spec = importlib.util.spec_from_file_location('extrema', Path(__file__).with_name('verify-hybrid-animation-extrema.py'))
extrema = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extrema)


def digest(value):
    return hashlib.sha256(value).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('plan', 'base', 'steam', 'manifest'):
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    plan, steam = args.plan.read_bytes(), args.steam.read_bytes()
    if digest(plan) != '72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975' or digest(steam) != 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35':
        raise ValueError('unexpected plan or Steam asset')
    if digest(args.base.read_bytes()) != 'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d':
        raise ValueError('unexpected template')
    manifest = args.manifest.read_bytes()
    if digest(manifest) != '716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1':
        raise ValueError('unexpected manifest')
    records = {r['file']: r for r in json.loads(manifest)['animations']}
    source = {o.path_id: o for o in UnityPy.load(str(args.steam)).objects}
    results = []
    pos = 268
    with zipfile.ZipFile(args.base) as archive:
        for _ in range(108):
            start = pos
            name = plan[pos:pos + 32].decode('ascii')
            target, size, file_index, source_offset, _, _ = struct.unpack_from('>IIIQIQ', plan, pos + 128)
            count = struct.unpack_from('>I', plan, pos + 160)[0]
            pos += 164
            if count:
                if name not in records or file_index != 0 or pos + count * 5 > len(plan):
                    raise ValueError('unexpected record')
                template = bytearray(archive.read('assets/bin/Data/' + name))
                payload = bytearray(steam[source_offset:source_offset + size])
                if digest(template) != plan[start + 32:start + 64].hex() or digest(payload) != plan[start + 96:start + 128].hex():
                    raise ValueError('input record differs')
                if len(payload) != size or target + size > len(template) or any(template[target:target + size]):
                    raise ValueError('invalid template range')
                for off in range(pos, pos + count * 5, 5):
                    index, value = struct.unpack_from('<IB', plan, off)
                    if index >= size: raise ValueError('invalid patch offset')
                    payload[index] = value
                template[target:target + size] = payload
                if digest(template) != plan[start + 64:start + 96].hex(): raise ValueError('reconstruction differs')
                original = UnityPy.load(bytes(template)).objects[0].read_typetree()
                steam_clip = source[records[name]['source_id']].read_typetree()
                get = lambda t: curve.frames(t['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'])
                a, b = get(original), get(steam_clip)
                if len(a) != len(b): raise ValueError('frame count differs')
                changed = collections.Counter(); all_keys = set(); maxima = collections.defaultdict(float); frames = 0
                for frame, ((at, ak), (bt, bk)) in enumerate(zip(a, b)):
                    if at != bt or [k for k, _ in ak] != [k for k, _ in bk]: raise ValueError('curve mapping differs')
                    if not math.isfinite(at) or at < 0: continue
                    frames += 1
                    for (index, x), (_, y) in zip(ak, bk):
                        all_keys.add(index)
                        if x != y: changed[index] += 1
                        if frame + 1 < len(a):
                            dt = a[frame + 1][0] - at
                            if math.isfinite(dt) and 0 < dt <= 60:
                                maxima[index] = max(maxima[index], extrema.extrema(x, y, dt)[0])
                # Test a simple fixed 2-D rotation/scale between pairs of
                # significant curves. This is a diagnostic, not a converter.
                significant = {k for k, v in maxima.items() if v > 0.001}
                fits = []
                for first in sorted(significant):
                    second = first + 3
                    if second not in significant: continue
                    samples = []
                    for (at, ak), (bt, bk) in zip(a, b):
                        if not math.isfinite(at) or at < 0: continue
                        target_keys, source_keys = dict(ak), dict(bk)
                        if first not in target_keys or second not in target_keys: continue
                        sx, sy = source_keys[first][3], source_keys[second][3]
                        tx, ty = target_keys[first][3], target_keys[second][3]
                        samples.append((sx, sy, tx, ty))
                    den = sum(sx*sx + sy*sy for sx, sy, _, _ in samples)
                    if len(samples) < 2 or den <= 1e-15: continue
                    aa = sum(sx*tx + sy*ty for sx, sy, tx, ty in samples) / den
                    bb = sum(sx*ty - sy*tx for sx, sy, tx, ty in samples) / den
                    before = max(math.hypot(tx-sx, ty-sy) for sx, sy, tx, ty in samples)
                    after = max(math.hypot(tx-(aa*sx-bb*sy), ty-(bb*sx+aa*sy)) for sx, sy, tx, ty in samples)
                    fits.append({'curves': [first, second], 'samples': len(samples),
                                 'max_distance_before': before, 'max_distance_after_fit': after})
                results.append({'file': name, 'name': records[name]['name'], 'frames': frames,
                                'curve_count': len(all_keys), 'changed_curve_count': len(changed),
                                'curves_above_0_0001': sum(v > 0.0001 for v in maxima.values()),
                                'curves_above_0_001': sum(v > 0.001 for v in maxima.values()),
                                'curves_above_0_01': sum(v > 0.01 for v in maxima.values()),
                                'curve_ids_above_0_001': sorted(k for k, v in maxima.items() if v > 0.001),
                                'fixed_pair_fit': fits,
                                'changed_curve_frame_occurrences': sum(changed.values()),
                                'max_occurrences_one_curve': max(changed.values(), default=0)})
            pos += count * 5
    if pos != len(plan) or len(results) != 11: raise ValueError('unexpected plan layout')
    print(json.dumps({'clips': results, 'limitations': ['Coefficient differences alone do not measure pose or equivalence.',
          'Fixed pair fit uses key values only; it is fitted on and evaluated against the same samples.',
          'Sentinel frames are excluded.', 'This report describes only the pinned inputs.']}, indent=2))


if __name__ == '__main__':
    main()
