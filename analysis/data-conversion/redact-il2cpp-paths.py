#!/usr/bin/env python3
"""Redact pinned local paths in diagnostic strings in one research build."""
import argparse
import hashlib
from pathlib import Path
import re

PINS = {
    'd9ee86b3710971bab9ac82b4031ff00f21f562c5f6e500e447676d8866a49b1c':
        ('badd7f169b112450f0676a9e7f396feb661e44bc9f76c44694ec493ff4da4931', 0x9703c0, 0x3dfd2b),
    'd06bb509d494b07eb2534154622a75788ee283d9022256baacbc96adfac5f1a7':
        ('aa9f72691e59988ffa31484de25f683f51d49b325c37af7610017240bba83d62', 0x970d50, 0x3dfeeb),
    'd2fdb70e11aeb49c0c7a24402c22c24eecbeef86e78d3584d1ed29efe07bd59b':
        ('bcf9025aada495df557d02342020700722d870024c28dd5dc998658bf439be97', 0x927c00, 0x3cc08b),
    '58104e9fc1d98319233817586bd985012e8b9e9ee9e28d8dabe4d6bd99106755':
        ('3ba53aa588ab8093b76507449209bb48ccdaf0e319ae7bff7b68c4bbc7285bfe', 0x927c60, 0x3cc0a3),
}
OLD = b'/home/your-user/'
NEW = b'/redacted___/'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    data = args.source.read_bytes()
    pin = PINS.get(hashlib.sha256(data).hexdigest())
    if pin is None or args.output.exists() or args.output.is_symlink():
        raise ValueError('source differs or output exists')
    result_sha, rodata_start, rodata_size = pin
    hits = list(re.finditer(re.escape(OLD), data))
    if len(OLD) != len(NEW) or len(hits) != 26:
        raise ValueError('unexpected path inventory')
    for hit in hits:
        offset = hit.start()
        if not rodata_start <= offset < rodata_start + rodata_size:
            raise ValueError('path outside pinned read-only data')
        start, end = data.rfind(b'\0', 0, offset) + 1, data.find(b'\0', offset)
        text = data[start:end]
        if end < 0 or not text.startswith(OLD) or b'Unsupported internal call for IL2CPP:' not in text:
            raise ValueError('path is not a pinned diagnostic message')
    result = data.replace(OLD, NEW)
    if len(result) != len(data) or hashlib.sha256(result).hexdigest() != result_sha:
        raise ValueError('redaction output differs')
    with args.output.open('xb') as output:
        output.write(result)
    print('Redacted 26 pinned diagnostic paths; no other bytes changed.')


if __name__ == '__main__':
    main()
