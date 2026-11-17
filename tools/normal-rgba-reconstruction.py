"""Deterministically unpack DXT5nm RGBA to the corresponding RGBA normal.

The input is already decoded RGBA, with X in alpha and Y in green. This is a
candidate generator: callers must copy only bytes that exactly match their
reference data. The transform itself is not evidence of visual parity.
"""
from math import isqrt


def reconstruct(data):
    if not isinstance(data, bytes) or len(data) % 4:
        raise ValueError('expected complete RGBA pixels')
    out = bytearray(len(data))
    for i in range(0, len(data), 4):
        green, alpha = data[i + 1], data[i + 3]
        x, y = 2 * alpha - 255, 2 * green - 255
        z = isqrt(max(0, 65025 - x*x - y*y))
        out[i:i + 4] = bytes((alpha, green, (255 + z) // 2, 255))
    return bytes(out)
