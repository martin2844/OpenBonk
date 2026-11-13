#!/usr/bin/env python3
"""Identify the objects in the private shader pack without exporting their contents."""
import argparse
import hashlib
import json
import struct
import zipfile
from collections import defaultdict
from pathlib import Path

import UnityPy


def digest(value):
    return hashlib.sha256(value).hexdigest()


def shader_name(obj):
    tree = obj.read_typetree()
    return tree['m_ParsedForm']['m_Name'] if obj.type.name == 'Shader' else tree['m_Name']


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('plan', type=Path)
    parser.add_argument('pack', type=Path)
    parser.add_argument('target_asset', type=Path)
    parser.add_argument('shader_cache', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    if args.output.exists() or args.output.is_symlink():
        parser.error('output already exists')
    plan = args.plan.read_bytes()
    if plan[:8] != b'OBS1S001' or len(plan) < 112:
        raise ValueError('unsupported shader plan')
    target_size, count = struct.unpack_from('>II', plan, 8)
    if count != 55 or len(plan) != 112 + count * 112:
        raise ValueError('unexpected shader plan size')
    if digest(args.pack.read_bytes()) != plan[80:112].hex():
        raise ValueError('shader pack does not match plan')
    target = args.target_asset.read_bytes()
    if len(target) != target_size or digest(target) != plan[48:80].hex():
        raise ValueError('target asset does not match plan')
    objects = {obj.byte_start: obj for obj in UnityPy.load(target).objects
               if obj.type.name in ('Shader', 'ComputeShader')}
    sources = defaultdict(list)
    for path in sorted(args.shader_cache.iterdir()):
        if not path.is_file():
            continue
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name in ('Shader', 'ComputeShader'):
                sources[digest(obj.get_raw_data())].append((path.name, obj.type.name, shader_name(obj)))
    rows = []
    with zipfile.ZipFile(args.pack) as archive:
        if len(archive.infolist()) != count:
            raise ValueError('shader pack entry count differs')
        for index in range(count):
            off = 112 + index * 112
            destination, size, lo, hi = struct.unpack_from('>IIII', plan, off)
            source_hash, target_hash = plan[off+16:off+48].hex(), plan[off+48:off+80].hex()
            name = f'shader-{index:03d}.bin'
            value = archive.read(name)
            obj = objects.get(destination)
            if (obj is None or obj.byte_size != size or digest(value) != source_hash
                    or digest(target[destination:destination+size]) != target_hash
                    or not (0 <= lo <= hi <= size)):
                raise ValueError('shader record mismatch: ' + name)
            kind, label = obj.type.name, shader_name(obj)
            matches = sorted(source for source, source_kind, source_name in sources[source_hash]
                             if source_kind == kind and source_name == label)
            if not matches:
                raise ValueError('no matching cache source: ' + name)
            rows.append({'entry': name, 'type': kind, 'name': label, 'size': size,
                         'source_sha256': source_hash, 'cache_bundles': matches,
                         'reference_bytes_preserved': hi - lo})
    report = {'warning': 'Private research inventory; source attribution and distribution rights are not established.',
              'plan_sha256': digest(plan), 'pack_sha256': digest(args.pack.read_bytes()),
              'target_sha256': digest(target), 'objects': rows}
    with args.output.open('x', encoding='utf-8') as output:
        json.dump(report, output, indent=2, ensure_ascii=False)
        output.write('\n')
    print('Inventoried', len(rows), 'shader objects; no shader contents exported.')


if __name__ == '__main__':
    main()
