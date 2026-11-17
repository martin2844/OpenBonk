"""Validate reordered transform bindings in two serialized animation clips.

This helper describes corresponding coefficient word offsets. It does not
modify clips, establish visual equivalence, or produce a redistribution plan.
"""
import copy
import json
import math
import struct


def _binding_channels(bindings):
    channels = []
    for binding in bindings:
        attribute = binding.get('attribute')
        # Limit this study to known Unity Transform float curves. Other types
        # can have different widths and are deliberately rejected.
        widths = {1: 3, 2: 4, 3: 3, 4: 3}
        if (binding.get('typeID') != 4 or attribute not in widths or
                binding.get('customType') != (4 if attribute == 4 else 0) or
                any(binding.get(field) != 0 for field in
                    ('isPPtrCurve', 'isIntCurve', 'isSerializeReferenceCurve'))):
            raise ValueError('unsupported animation binding')
        identity = json.dumps(binding, sort_keys=True, separators=(',', ':'))
        channels.extend((identity, component) for component in range(widths[attribute]))
    if len(set(channels)) != len(channels):
        raise ValueError('ambiguous animation bindings')
    return channels


def binding_map(target, source):
    """Map target channel indices to source indices after strict metadata checks."""
    left, right = copy.deepcopy(target), copy.deepcopy(source)
    left_bindings = left['m_ClipBindingConstant']['genericBindings']
    right_bindings = right['m_ClipBindingConstant']['genericBindings']
    lc, rc = _binding_channels(left_bindings), _binding_channels(right_bindings)
    if len(lc) != len(rc) or set(lc) != set(rc):
        raise ValueError('animation bindings differ')
    source_index = {channel: index for index, channel in enumerate(rc)}
    mapping = [source_index[channel] for channel in lc]
    left_delta = left['m_MuscleClip']['m_ValueArrayDelta']
    right_delta = right['m_MuscleClip']['m_ValueArrayDelta']
    if (len(left_delta) != len(mapping) or len(right_delta) != len(mapping) or
            any(left_delta[i] != right_delta[j] for i, j in enumerate(mapping))):
        raise ValueError('animation value metadata differs')
    left['m_ClipBindingConstant']['genericBindings'] = []
    right['m_ClipBindingConstant']['genericBindings'] = []
    left['m_MuscleClip']['m_ValueArrayDelta'] = []
    right['m_MuscleClip']['m_ValueArrayDelta'] = []
    left['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    right['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'] = []
    if left != right:
        raise ValueError('other animation metadata differs')
    return mapping


def _frames(words):
    frames, offset = [], 0
    while offset < len(words):
        if offset + 2 > len(words):
            raise ValueError('truncated animation frame')
        time = struct.unpack('<f', struct.pack('<I', words[offset]))[0]
        count = words[offset + 1]
        # Unity may use infinite sentinel times. NaN cannot be compared safely.
        if math.isnan(time) or count > 100000 or offset + 2 + 5 * count > len(words):
            raise ValueError('invalid animation frame')
        keys = {}
        for i in range(count):
            position = offset + 2 + 5 * i
            index = words[position]
            if index in keys:
                raise ValueError('duplicate curve in frame')
            keys[index] = position + 1
        frames.append((time, keys))
        offset += 2 + 5 * count
    return frames


def coefficient_pairs(target_words, source_words, mapping):
    """Return (target curve, time, target offset, source offset) for each key.

    Offsets point to four coefficient words. Require a bijection in each frame;
    callers must independently check numerical differences and sentinel keys.
    """
    if any(type(index) is not int for index in mapping) or sorted(mapping) != list(range(len(mapping))):
        raise ValueError('animation channel mapping is not a bijection')
    left, right = _frames(target_words), _frames(source_words)
    if len(left) != len(right):
        raise ValueError('animation frame count differs')
    pairs = []
    for (lt, lk), (rt, rk) in zip(left, right):
        if lt != rt or any(index >= len(mapping) for index in lk):
            raise ValueError('animation frame timing or index differs')
        if len(lk) != len(rk) or {mapping[index] for index in lk} != set(rk):
            raise ValueError('animation frame curves differ')
        pairs.extend((index, lt, position, rk[mapping[index]]) for index, position in lk.items())
    return pairs
