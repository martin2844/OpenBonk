#!/usr/bin/env python3
"""Remove three diagnostic shader entries from the pinned private base ZIP.

Copies all remaining local ZIP records without decompressing them: the font
entry is intentionally incomplete and cannot be recompressed. This produces
an intermediate research template, not a distributable artifact.
"""
import hashlib
from pathlib import Path
import struct
import sys
from zipfile import ZipFile

EXPECTED = {'29af2d72a6982f2cf8905020498127a384a467ef1c56ddeb57dd80e17961055c',
            'ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'}
PREFIX = 'assets/bin/Data/'
REMOVE = {PREFIX + n for n in ('df272285d21fd527096b3c5d8a8c9adf', 'baf39c96675258361b63538c61a6f54e', 'f85e78285b9fb8553bc9aff72231fd4d')}
FONT = PREFIX + 'a4611dd71c390bc48af432c727d2e32c'


def main():
    if len(sys.argv) != 3: raise ValueError('usage: SCRIPT INPUT_BASE NEW_BASE')
    src, dst = map(Path, sys.argv[1:])
    if not src.is_file() or dst.exists() or dst.is_symlink() or not dst.parent.is_dir(): raise ValueError('invalid input or output')
    data = src.read_bytes()
    if hashlib.sha256(data).hexdigest() not in EXPECTED: raise ValueError('unexpected template')
    with ZipFile(src) as z:
        infos = z.infolist(); names = [i.filename for i in infos]; start = z.start_dir; comment = z.comment
        if len(infos) != 946 or len(names) != len(set(names)) or not REMOVE <= set(names): raise ValueError('unexpected inventory')
        local = sorted(infos, key=lambda x: x.header_offset)
        if local[0].header_offset != 0: raise ValueError('unexpected ZIP prefix')
        font_offset = z.getinfo(FONT).header_offset
        if any(z.getinfo(n).header_offset <= font_offset for n in REMOVE): raise ValueError('font offset would change')
        records = {}
        for i, entry in enumerate(local):
            end = local[i + 1].header_offset if i + 1 < len(local) else start
            if data[entry.header_offset:entry.header_offset + 4] != b'PK\x03\x04' or end <= entry.header_offset: raise ValueError('invalid local header')
            records[entry.filename] = data[entry.header_offset:end]
        central = []
        pos = start
        for entry in infos:
            if data[pos:pos+4] != b'PK\x01\x02': raise ValueError('invalid central header')
            length = 46 + sum(struct.unpack_from('<HHH', data, pos + 28))
            record = data[pos:pos + length]
            if len(record) != length or struct.unpack_from('<I', record, 42)[0] != entry.header_offset: raise ValueError('bad central offset')
            central.append((entry, record)); pos += length
        if data[pos:pos+4] != b'PK\x05\x06' or len(data) != pos + 22 + len(comment): raise ValueError('unsupported ZIP end record')
        end_record = data[pos:pos+22]
        if data[pos+22:] != comment or struct.unpack_from('<H', end_record, 20)[0] != len(comment): raise ValueError('unexpected ZIP comment')
        count = len(infos) - len(REMOVE)
        body = bytearray(); offsets = {}
        for entry in local:
            if entry.filename in REMOVE: continue
            offsets[entry.filename] = len(body); body.extend(records[entry.filename])
        if offsets[FONT] != font_offset: raise ValueError('font location changed')
        central_start = len(body)
        for entry, original in central:
            if entry.filename in REMOVE: continue
            rec = bytearray(original); struct.pack_into('<I', rec, 42, offsets[entry.filename]); body.extend(rec)
        central_size = len(body) - central_start
        end = bytearray(end_record)
        struct.pack_into('<HHII', end, 8, count, count, central_size, central_start)
        body.extend(end); body.extend(comment)
    try:
        with dst.open('xb') as out: out.write(body)
        with ZipFile(dst) as z:
            if len(z.infolist()) != count or set(z.namelist()) != set(names) - REMOVE or z.getinfo(FONT).header_offset != font_offset: raise ValueError('output inventory differs')
            for entry in z.infolist():
                segment = records[entry.filename]
                if body[entry.header_offset:entry.header_offset + len(segment)] != segment: raise ValueError('local record changed')
    except BaseException:
        dst.unlink(missing_ok=True); raise
    print('Private scrubbed base SHA-256:', hashlib.sha256(body).hexdigest())

if __name__ == '__main__': main()
