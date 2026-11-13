#!/usr/bin/env python3
"""Blank and restore selected hashed object bodies from pinned Steam assets.

Research prototype only. Retains pointers, metadata and other game material;
neither the templates nor output are cleared for distribution.
"""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import shutil
import zipfile

import UnityPy

PRELOAD = 'fb3896524cca9e8b137eb5d4c7c6f17eaca57d02185b8f1a31fcf83ab0fd99a0'
OBB = 'ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2'
STEAM = {'sharedassets1.assets': 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
         'sharedassets2.assets': 'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da'}
WINDOWS = {'sharedassets1.assets': '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a',
           'sharedassets2.assets': 'cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'}
EXTRA_TYPES = {'ParticleSystemRenderer', 'Rigidbody', 'SphereCollider', 'TrailRenderer', 'Transform'}


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read_pinned(path, expected):
    data = path.read_bytes()
    if digest(data) != expected:
        raise ValueError('incompatible input: ' + str(path))
    return data


def preload_targets(data):
    objs = [o for o in UnityPy.load(data).objects if o.type.name == 'PreloadData']
    if len(objs) != 1:
        raise ValueError('unexpected preload')
    obj = objs[0]
    names = [str(x.path).replace('\\', '/').rsplit('/', 1)[-1].lower() for x in obj.assets_file.externals]
    if len(names) != len(set(names)):
        raise ValueError('ambiguous external names')
    result = defaultdict(set)
    for ref in obj.read_typetree()['m_Assets']:
        file_id = ref['m_FileID']
        if file_id <= 0 or file_id > len(names):
            raise ValueError('invalid preload reference')
        name = names[file_id - 1]
        if re.fullmatch('[0-9a-f]{32}', name):
            if ref['m_PathID'] in result[name]:
                raise ValueError('duplicate preload reference')
            result[name].add(ref['m_PathID'])
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('template', 'restore'))
    p.add_argument('steam_data', type=Path)
    p.add_argument('preload', type=Path)
    p.add_argument('input', type=Path, help='pinned OBB for template, template directory for restore')
    p.add_argument('output', type=Path)
    p.add_argument('--include-gameobjects', action='store_true', help='also reconstruct matching GameObject bodies')
    p.add_argument('--include-extra-components', action='store_true', help='also reconstruct other exact matching component bodies; requires --include-gameobjects')
    p.add_argument('--windows-source', action='store_true', help='use pinned Windows inputs for restore')
    a = p.parse_args()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if a.include_extra_components and not a.include_gameobjects:
        p.error('--include-extra-components requires --include-gameobjects')
    included = {'ParticleSystem'} | ({'GameObject'} if a.include_gameobjects else set()) | (EXTRA_TYPES if a.include_extra_components else set())
    version = 3 if a.include_extra_components else 2 if a.include_gameobjects else 1
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be a new directory')
    out = a.output.resolve()
    for src in (a.steam_data, a.preload, a.input):
        path = src.resolve()
        if out == path or out.is_relative_to(path) or path.is_relative_to(out):
            p.error('output must be separate from inputs')
    targets = preload_targets(read_pinned(a.preload, PRELOAD))
    source_index, source_ids = defaultdict(list), {}
    for name, expected in (WINDOWS if a.windows_source else STEAM).items():
        env = UnityPy.load(read_pinned(a.steam_data / name, expected))
        for obj in env.objects:
            if obj.type.name in included:
                raw = obj.get_raw_data()
                if len(raw) < 12:
                    raise ValueError('short source particle')
                source_index[(obj.type.name, len(raw), digest(raw[12:]))].append((name, obj.path_id, raw[12:]))
                source_ids[(name, obj.path_id)] = (obj.type.name, raw[12:])
    original_manifest = None
    if a.action == 'template':
        read_pinned(a.input, OBB)
        archive = zipfile.ZipFile(a.input)
        if len(archive.namelist()) != len(set(archive.namelist())):
            raise ValueError('duplicate OBB entry')
        filenames = sorted(targets)
    else:
        archive = None
        original_manifest = json.loads((a.input / 'manifest.json').read_text())
        if original_manifest.get('version') != version or original_manifest.get('preload_sha256') != PRELOAD or original_manifest.get('obb_sha256') != OBB:
            raise ValueError('invalid manifest')
        files = original_manifest['files']
        filenames = sorted(files)
        if len(filenames) != len(set(filenames)) or not set(filenames) <= targets.keys():
            raise ValueError('invalid template filenames')
    output_files, records, count, nonzero = {}, {}, 0, 0
    rows_key = 'objects' if a.include_gameobjects else 'particles'
    types = defaultdict(int)
    try:
        for filename in filenames:
            if a.action == 'template':
                raw_file = archive.read('assets/bin/Data/' + filename)
            else:
                raw_file = read_pinned(a.input / filename, files[filename]['template_sha256'])
            env = UnityPy.load(raw_file)
            objs = {o.path_id: o for o in env.objects}
            if len(objs) != len(env.objects) or not targets[filename] <= objs.keys():
                raise ValueError('missing or duplicated target objects')
            work, items = bytearray(raw_file), []
            for path_id in sorted(targets[filename]):
                obj = objs[path_id]
                if obj.type.name not in included:
                    continue
                raw = obj.get_raw_data()
                if len(raw) < 12:
                    raise ValueError('short target particle')
                if a.action == 'template':
                    candidates = source_index.get((obj.type.name, len(raw), digest(raw[12:])), [])
                    if not candidates:
                        if obj.type.name != 'ParticleSystem':
                            continue
                        raise ValueError('unmatched particle body')
                    name, source_id, body = sorted(candidates, key=lambda x: (x[0], x[1]))[0]
                else:
                    prior = {x['target_id']: x for x in files[filename][rows_key]}
                    if path_id not in prior:
                        if obj.type.name != 'ParticleSystem':
                            continue
                        raise ValueError('missing particle record')
                    name, source_id = prior[path_id]['steam_file'], prior[path_id]['steam_id']
                    source_type, body = source_ids[(name, source_id)]
                    if source_type != obj.type.name:
                        raise ValueError('source object type mismatch')
                start = obj.byte_start + 12
                if start < 12 or start + len(body) > len(work) or len(raw) != len(body) + 12:
                    raise ValueError('invalid particle range')
                rec = {'target_id': path_id, 'steam_file': name, 'steam_id': source_id,
                       'body_size': len(body), 'body_sha256': digest(body)}
                if a.include_gameobjects:
                    rec['type'] = obj.type.name
                if a.action == 'template':
                    if raw[12:] != body:
                        raise ValueError('particle body differs')
                    nonzero += sum(x != 0 for x in body)
                    work[start:start + len(body)] = bytes(len(body))
                else:
                    if prior[path_id] != rec or any(raw[12:]):
                        raise ValueError('particle template mismatch')
                    work[start:start + len(body)] = body
                items.append(rec)
                count += 1
                types[obj.type.name] += 1
            if not items:
                continue
            result = bytes(work)
            if a.action == 'restore' and (len(items) != len(files[filename][rows_key]) or digest(result) != files[filename]['original_sha256']):
                raise ValueError('restored asset mismatch')
            check = {o.path_id: o for o in UnityPy.load(result).objects}
            selected = {x['target_id'] for x in items}
            if set(check) != set(objs):
                raise ValueError('object inventory changed')
            for oid, old in objs.items():
                before, after = old.get_raw_data(), check[oid].get_raw_data()
                if check[oid].type.name != old.type.name or (oid not in selected and before != after) or (oid in selected and before[:12] != after[:12]):
                    raise ValueError('unrelated object or pointer changed')
            output_files[filename] = result
            records[filename] = {'original_sha256': digest(raw_file) if a.action == 'template' else files[filename]['original_sha256'],
                                 'template_sha256': digest(result) if a.action == 'template' else files[filename]['template_sha256'],
                                 rows_key: items}
        expected_types = {'ParticleSystem': 101}
        if a.include_gameobjects: expected_types['GameObject'] = 503
        if a.include_extra_components: expected_types.update({'ParticleSystemRenderer': 1, 'Rigidbody': 3, 'SphereCollider': 2, 'TrailRenderer': 2, 'Transform': 23})
        if dict(types) != expected_types:
            raise ValueError('unexpected particle count')
        report = {'version': version, 'preload_sha256': PRELOAD, 'obb_sha256': OBB,
                  ('objects' if a.include_gameobjects else 'particles'): count, 'files': records,
                  'limitations': 'Local research only; other game content, metadata, code and dependencies remain.'}
        if a.include_gameobjects:
            report['object_counts'] = dict(types)
        if a.action == 'restore' and records != files:
            raise ValueError('manifest mismatch')
        a.output.mkdir()
        try:
            for name, data in output_files.items():
                (a.output / name).write_bytes(data)
            (a.output / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n')
        except BaseException:
            shutil.rmtree(a.output)
            raise
        print(a.action, count, 'object bodies across', len(records), 'files;', nonzero, 'nonzero bytes blanked')
    finally:
        if archive:
            archive.close()


if __name__ == '__main__':
    main()
