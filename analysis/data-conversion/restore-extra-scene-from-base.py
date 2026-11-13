#!/usr/bin/env python3
"""Restore pinned scene component bodies from Steam into the scrubbed base.

Local diagnostic only: scene files retain other game data and are not cleared
for distribution. This avoids separate full-scene copies and an inventory.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil

import UnityPy

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('scene_base', HERE / 'no-rewired-scene-bodies-prototype.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)
FIRST_SHA = 'a77039c682ed1651c2b9a2d195dffff33b2bfad49cd1da42278bc2152bfa854f'
EXTRA_SHA = '3c2455aaf75c2fe9a9a1a8f649d8c770dd6ae4db16560dce01ee0703034a7775'
COUNTS = {'level1': 1341, 'level2': 1179, 'level4': 1090}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read_pinned(path, expected):
    data = path.read_bytes()
    if digest(data) != expected:
        raise ValueError('incompatible input: ' + str(path))
    return data


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('pc_data', type=Path)
    parser.add_argument('template', type=Path)
    parser.add_argument('first_manifest', type=Path)
    parser.add_argument('extra_manifest', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--windows-scenes', action='store_true', help='restore selected bodies from pinned Windows scenes')
    a = parser.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        parser.error('output must be new')
    out = a.output.resolve()
    for source in (a.pc_data, a.template, a.first_manifest, a.extra_manifest):
        source = source.resolve()
        if out.is_relative_to(source) or source.is_relative_to(out):
            parser.error('output must be separate from inputs')
    first_bytes = read_pinned(a.first_manifest, FIRST_SHA)
    first = json.loads(first_bytes)
    extra = json.loads(read_pinned(a.extra_manifest, EXTRA_SHA))
    if first.get('version') != 1 or extra.get('version') != 1 or set(first['scenes']) != set(COUNTS) or set(extra['scenes']) != set(COUNTS):
        raise ValueError('invalid scene manifests')
    for scene, pin in (base.WINDOWS_SCENE_PINS if a.windows_scenes else base.PC_PINS).items():
        read_pinned(a.pc_data / scene, pin)
    a.output.mkdir()
    summary = {}
    try:
        for scene, count in COUNTS.items():
            recs = extra['scenes'][scene]['records']
            if len(recs) != count or len({r['target_id'] for r in recs}) != count:
                raise ValueError('unexpected component inventory')
            raw_file = read_pinned(a.template / scene, extra['scenes'][scene]['template_sha256'])
            targets = base.objs(UnityPy.load(raw_file))
            sources = base.objs(UnityPy.load(str(a.pc_data / scene)))
            work = bytearray(raw_file)
            prior_end = 0
            for rec in recs:
                target = targets[rec['target_id']]
                source = sources[rec['source_id']]
                if target.type.name != 'MonoBehaviour' or source.type.name != 'MonoBehaviour':
                    raise ValueError('component type mismatch')
                raw, src = target.get_raw_data(), source.get_raw_data()
                offset, size = rec['offset'], rec['size']
                if (offset < prior_end or offset != target.byte_start + 28 or size != len(raw) - 28
                        or len(src) != len(raw) or offset + size > len(work)
                        or any(raw[28:]) or digest(src[28:]) != rec['body_sha256']):
                    raise ValueError('component body or range mismatch')
                work[offset:offset + size] = src[28:]
                prior_end = offset + size
            if digest(work) != first['scenes'][scene]['template_sha256']:
                raise ValueError('restored intermediate scene mismatch')
            (a.output / scene).write_bytes(work)
            summary[scene] = count
        (a.output / 'manifest.json').write_bytes(first_bytes)
        print('restored extra scene components:', summary, flush=True)
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__ == '__main__':
    main()
