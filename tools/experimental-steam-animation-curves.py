#!/usr/bin/env python3
"""Study close Steam animation streams in a private reconstructed OBB.

Only streams with matching metadata and tightly bounded curve differences are
substituted. The output still contains game assets and must remain local. Curve
comparison does not establish equality of Unity poses, blending or rendering.
"""
import argparse
import copy
from collections import Counter, defaultdict
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import struct
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


animation = load('animation_mask', ROOT / 'tools/experimental-animation-mask.py')
objects = load('animation_objects', ROOT / 'tools/experimental-exact-object-plan.py')
curve = load('animation_curves', ROOT / 'analysis/data-conversion/analyze-shared2-animation-streams.py')
extrema = load('animation_extrema', ROOT / 'analysis/data-conversion/verify-hybrid-animation-extrema.py').extrema
LIMIT = 1e-6


def stream(tree):
    return tree['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']


def compatible_frames(left, right, limit=LIMIT):
    """Compare key identity and cubic extrema; reject changed sentinel keys."""
    if len(left) != len(right):
        return None
    worst, segments = 0.0, 0
    for i, ((lt, lk), (rt, rk)) in enumerate(zip(left, right)):
        if lt != rt or [key for key, _ in lk] != [key for key, _ in rk]:
            return None
        if i + 1 == len(left):
            if lk != rk:
                return None
            continue
        dt = left[i + 1][0] - lt
        if lt < 0 or not math.isfinite(dt) or dt <= 0 or dt > 60:
            if lk != rk:
                return None
            continue
        for (_, lc), (_, rc) in zip(lk, rk):
            value, _ = extrema(lc, rc, dt)
            if not math.isfinite(value) or value > limit:
                return None
            worst = max(worst, value)
            segments += 1
    return worst if segments else None


def candidate(target, source):
    left, right = target.read_typetree(), source.read_typetree()
    lw, rw = stream(left), stream(right)
    if not lw or len(lw) != len(rw):
        return None
    a, b = copy.deepcopy(left), copy.deepcopy(right)
    a['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    b['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    if a != b:
        return None
    difference = compatible_frames(curve.frames(lw), curve.frames(rw))
    if difference is None:
        return None
    before = struct.pack('<%dI' % len(lw), *lw)
    after = struct.pack('<%dI' % len(rw), *rw)
    if before == after:
        return None
    raw = target.get_raw_data()
    if raw.count(before) != 1:
        return None
    return raw.index(before), before, after, difference


def generate(args):
    source = args.steam_data.resolve(strict=True)
    obb = args.obb.resolve(strict=True)
    output = animation.regular_output(args.output, (source, obb))
    sources = defaultdict(list)
    # Keep UnityPy environments alive while their object readers are in use.
    environments = []
    for name in animation.SOURCE_NAMES:
        path = source / name
        if path.is_symlink() or not path.is_file():
            raise ValueError('missing Steam animation source')
        env = UnityPy.load(str(path)); environments.append(env)
        for obj in env.objects:
            if obj.type.name == 'AnimationClip':
                sources[(obj.read().m_Name, obj.byte_size)].append((name, obj))
    changes, results, outcomes = {}, [], Counter()
    with zipfile.ZipFile(obb) as archive:
        infos = animation.zip_entries(archive)
        groups = objects.asset_groups(archive)
        for group, parts in groups.items():
            data = b''.join(archive.read(part) for part in parts)
            try:
                target_objects = UnityPy.load(data).objects
            except (ValueError, TypeError, EOFError, OSError):
                outcomes['unreadable_group'] += 1
                continue
            modified = bytearray(data)
            for obj in target_objects:
                if obj.type.name != 'AnimationClip':
                    continue
                try:
                    name = obj.read().m_Name
                    found = []
                    for source_name, source_obj in sources.get((name, obj.byte_size), []):
                        item = candidate(obj, source_obj)
                        if item is not None:
                            found.append((source_name, item))
                    fingerprints = {hashlib.sha256(item[2]).digest() for _, item in found}
                    if len(fingerprints) != 1:
                        outcomes['not_selected'] += 1
                        continue
                    source_name, (position, before, after, difference) = sorted(found, key=lambda x: x[0])[0]
                    offset = obj.byte_start + position
                    if data[offset:offset + len(before)] != before:
                        raise ValueError('animation stream offset differs')
                    modified[offset:offset + len(before)] = after
                    results.append({'name': name, 'group': group, 'path_id': obj.path_id,
                                    'source': source_name, 'stream_bytes': len(before),
                                    'changed_bytes': sum(a != b for a, b in zip(before, after)),
                                    'max_curve_difference': difference})
                except (ValueError, TypeError, KeyError, AttributeError, OverflowError, struct.error):
                    outcomes['unreadable_or_incompatible'] += 1
            if modified != data:
                offset = 0
                for part in parts:
                    changes[part.filename] = bytes(modified[offset:offset + part.file_size])
                    offset += part.file_size
        output.mkdir(mode=0o700)
        with zipfile.ZipFile(output / 'converted.obb', 'x') as dest:
            for info in infos:
                data = changes[info.filename] if info.filename in changes else archive.read(info)
                dest.writestr(info, data)
    report = {'schema': 1, 'threshold': LIMIT, 'selected': results, 'outcomes': dict(outcomes),
              'limitations': ['Curve extrema use double precision, not Unity runtime pose evaluation.',
                              'Sentinel intervals are not sampled; differing sentinel keys are rejected.',
                              'Other game assets remain in the output.']}
    (output / 'report.json').write_text(json.dumps(report, indent=2) + '\n')
    print('Selected', len(results), 'near-identical Steam animation streams; output remains private.')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--steam-data', required=True, type=Path)
    p.add_argument('--obb', required=True, type=Path)
    p.add_argument('--output', required=True, type=Path)
    generate(p.parse_args())


if __name__ == '__main__':
    main()
