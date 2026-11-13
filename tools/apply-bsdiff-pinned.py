"""Apply a pinned BSDIFF40 patch with the Python standard library."""
import bz2
import hashlib


def signed_int(data):
    if len(data) != 8: raise ValueError('short BSDIFF integer')
    value = int.from_bytes(data, 'little')
    return -(value & ((1 << 63)-1)) if value >> 63 else value


def apply(old, patch, expected_size, expected_sha):
    if len(patch) < 32 or patch[:8] != b'BSDIFF40': raise ValueError('invalid BSDIFF header')
    ctrl_size, diff_size, new_size = (signed_int(patch[i:i+8]) for i in (8,16,24))
    if ctrl_size < 0 or diff_size < 0 or 32+ctrl_size+diff_size > len(patch) or new_size != expected_size:
        raise ValueError('invalid BSDIFF sizes')
    ctrl = bz2.decompress(patch[32:32+ctrl_size])
    diff = bz2.decompress(patch[32+ctrl_size:32+ctrl_size+diff_size])
    extra = bz2.decompress(patch[32+ctrl_size+diff_size:])
    if len(ctrl) % 24 or len(diff) > new_size or len(extra) > new_size: raise ValueError('invalid BSDIFF data')
    result = bytearray(); oldpos = diffpos = extrapos = 0
    for offset in range(0, len(ctrl), 24):
        count, literal, seek = (signed_int(ctrl[offset+i:offset+i+8]) for i in (0,8,16))
        if count < 0 or literal < 0 or len(result)+count+literal > new_size or diffpos+count > len(diff) or extrapos+literal > len(extra):
            raise ValueError('invalid BSDIFF operation')
        if oldpos < 0 or oldpos+count > len(old): raise ValueError('BSDIFF source range invalid')
        result.extend((old[oldpos+i]+diff[diffpos+i]) & 255 for i in range(count))
        result.extend(extra[extrapos:extrapos+literal])
        oldpos += count+seek; diffpos += count; extrapos += literal
    if len(result) != new_size or diffpos != len(diff) or extrapos != len(extra) or hashlib.sha256(result).hexdigest() != expected_sha:
        raise ValueError('BSDIFF result mismatch')
    return bytes(result)
