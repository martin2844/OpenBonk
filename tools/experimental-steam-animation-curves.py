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
reorder = load('animation_binding_reorder', ROOT / 'tools/animation_binding_reorder.py')
LIMIT = 1e-6
RELAXED_LIMIT = 1e-4
EXTENDED_LIMIT = 1e-3


def stream(tree):
    return tree['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']


def compatible_frames(left, right, limit=LIMIT):
    """Bound each curve until its next key, not merely the next global frame."""
    if len(left) != len(right):
        return None
    for (lt, lk), (rt, rk) in zip(left, right):
        if lt != rt or [key for key, _ in lk] != [key for key, _ in rk]:
            return None
    worst, segments = 0.0, 0
    next_key_time = {}
    for (lt, lk), (_, rk) in reversed(list(zip(left, right))):
        for (key, lc), (_, rc) in zip(lk, rk):
            next_time = next_key_time.get(key)
            next_key_time[key] = lt
            dt = next_time - lt if next_time is not None else float('nan')
            if lt < 0 or not math.isfinite(dt) or dt <= 0 or dt > 60:
                if lc != rc:
                    return None
                continue
            value, _ = extrema(lc, rc, dt)
            if not math.isfinite(value) or value > limit:
                return None
            worst = max(worst, value)
            segments += 1
    return worst if segments else None


def candidate(target, source, limit=LIMIT):
    left, right = target.read_typetree(), source.read_typetree()
    lw, rw = stream(left), stream(right)
    if not lw or len(lw) != len(rw):
        return None
    a, b = copy.deepcopy(left), copy.deepcopy(right)
    a['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    b['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    if a != b:
        return None
    difference = compatible_frames(curve.frames(lw), curve.frames(rw), limit=limit)
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


def hybrid_candidate(target, source, limit=LIMIT):
    """Use Steam coefficients only for individually bounded curve segments."""
    left, right = target.read_typetree(), source.read_typetree()
    lw, rw = stream(left), stream(right)
    if not lw or len(lw) != len(rw):
        return None
    a, b = copy.deepcopy(left), copy.deepcopy(right)
    a['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    b['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    if a != b:
        return None
    lf, rf = curve.frames(lw), curve.frames(rw)
    if len(lf) != len(rf):
        return None
    offsets, pos = [], 0
    for (lt, lk), (rt, rk) in zip(lf, rf):
        if lt != rt or [key for key, _ in lk] != [key for key, _ in rk]:
            return None
        offsets.append([pos + 3 + 5 * i for i in range(len(lk))])
        pos += 2 + 5 * len(lk)
    if pos != len(lw):
        return None
    result, next_key_time, worst = list(lw), {}, 0.0
    for frame in range(len(lf) - 1, -1, -1):
        lt, lk = lf[frame]; _, rk = rf[frame]
        for index, ((key, lc), (_, rc)) in enumerate(zip(lk, rk)):
            next_time = next_key_time.get(key)
            next_key_time[key] = lt
            dt = next_time - lt if next_time is not None else float('nan')
            if lt < 0 or not math.isfinite(dt) or dt <= 0 or dt > 60:
                continue
            difference, _ = extrema(lc, rc, dt)
            if not math.isfinite(difference) or difference > limit:
                continue
            offset = offsets[frame][index]
            result[offset:offset + 4] = rw[offset:offset + 4]
            worst = max(worst, difference)
    before = struct.pack('<%dI' % len(lw), *lw)
    after = struct.pack('<%dI' % len(result), *result)
    if before == after:
        return None
    raw = target.get_raw_data()
    if raw.count(before) != 1:
        return None
    # Independently recheck the final mixed stream, including sparse keys.
    verified = compatible_frames(lf, curve.frames(result), limit=limit)
    if verified is None:
        return None
    return raw.index(before), before, after, max(worst, verified)


def reordered_hybrid_candidate(target, source, limit=LIMIT):
    """Substitute bounded curve coefficients when transform bindings reorder.

    Metadata is checked by binding identity and channel, and unchecked sentinel
    keys are preserved. This changes only the target stream's coefficient words;
    its frame ordering, curve indices and serialized metadata remain untouched.
    """
    left, right = target.read_typetree(), source.read_typetree()
    lw, rw = stream(left), stream(right)
    if not lw or not rw:
        return None
    try:
        mapping = reorder.binding_map(left, right)
        if mapping == list(range(len(mapping))):
            return None
        pairs = reorder.coefficient_pairs(lw, rw, mapping)
    except (ValueError, TypeError, KeyError, AttributeError, OverflowError, struct.error):
        return None
    result, next_key_time, worst = list(lw), {}, 0.0
    for key, time, target_offset, source_offset in reversed(pairs):
        next_time = next_key_time.get(key)
        next_key_time[key] = time
        dt = next_time - time if next_time is not None else float('nan')
        if time < 0 or not math.isfinite(dt) or dt <= 0 or dt > 60:
            continue
        target_coeff = tuple(curve.floating(x) for x in lw[target_offset:target_offset + 4])
        source_coeff = tuple(curve.floating(x) for x in rw[source_offset:source_offset + 4])
        if not all(math.isfinite(value) for value in (*target_coeff, *source_coeff)):
            return None
        difference, _ = extrema(target_coeff, source_coeff, dt)
        if not math.isfinite(difference) or difference > limit:
            continue
        result[target_offset:target_offset + 4] = rw[source_offset:source_offset + 4]
        worst = max(worst, difference)
    before = struct.pack('<%dI' % len(lw), *lw)
    after = struct.pack('<%dI' % len(result), *result)
    if before == after:
        return None
    verified = compatible_frames(curve.frames(lw), curve.frames(result), limit=limit)
    if verified is None:
        return None
    raw = target.get_raw_data()
    if raw.count(before) != 1:
        return None
    return raw.index(before), before, after, max(worst, verified)


def generate(args):
    if getattr(args, 'extended', False) and getattr(args, 'relaxed', False):
        raise ValueError('animation curve bounds cannot be combined')
    limit = EXTENDED_LIMIT if getattr(args, 'extended', False) else RELAXED_LIMIT if getattr(args, 'relaxed', False) else LIMIT
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
                        item = candidate(obj, source_obj, limit=limit)
                        if item is None and getattr(args, 'hybrid', False):
                            item = hybrid_candidate(obj, source_obj, limit=limit)
                        if item is None and getattr(args, 'reordered_hybrid', False):
                            item = reordered_hybrid_candidate(obj, source_obj, limit=limit)
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
    report = {'schema': 1, 'threshold': limit, 'selected': results, 'outcomes': dict(outcomes),
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
    mode = p.add_mutually_exclusive_group()
    mode.add_argument('--relaxed', action='store_true', help='use an experimental 1e-4 per-segment curve bound')
    mode.add_argument('--extended', action='store_true', help='use an experimental 1e-3 per-segment curve bound')
    p.add_argument('--hybrid', action='store_true', help='also try bounded per-curve partial substitution')
    p.add_argument('--reordered-hybrid', action='store_true', help='also try bounded coefficients in reordered Transform curves')
    generate(p.parse_args())


if __name__ == '__main__':
    main()
