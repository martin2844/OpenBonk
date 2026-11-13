#!/usr/bin/env python3
"""Hash APK payload and relevant ZIP metadata independently of APK signatures.

This is an integrity comparison, not a signature or provenance check.
"""
import hashlib
import json
import zipfile


def jar_signature(name):
    upper = name.upper()
    if not upper.startswith('META-INF/'):
        return False
    leaf = upper[len('META-INF/'):]
    return '/' not in leaf and (leaf == 'MANIFEST.MF' or leaf.endswith(('.SF', '.RSA', '.DSA', '.EC')) or leaf.startswith('SIG-'))


def digest(path):
    with zipfile.ZipFile(path) as z:
        infos = z.infolist()
        names = [i.filename for i in infos]
        if len(names) != len(set(names)):
            raise ValueError('duplicate APK entries')
        rows = []
        for info in sorted(infos, key=lambda i: i.filename):
            if jar_signature(info.filename):
                continue
            with z.open(info) as f:
                payload = hashlib.file_digest(f, 'sha256').hexdigest()
            rows.append([info.filename, info.file_size, info.compress_type, info.external_attr,
                         info.flag_bits, payload])
        if not rows:
            raise ValueError('empty APK payload')
    encoded = json.dumps(rows, ensure_ascii=True, separators=(',', ':')).encode('ascii')
    return hashlib.sha256(encoded).hexdigest()
