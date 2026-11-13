#!/usr/bin/env python3
"""Rebuild exact particle bodies in the final-map asset from pinned Steam data.

Preserves Android object headers. A byte match does not establish authorship;
this experimental template retains other content and is not distributable.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import UnityPy

PC = 'cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3'
WINDOWS_PC = '896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9'
BASE = '868c4bc827c0f21293c2252660ef9b51931e931018e1af44240c5b4594b4db09'
EXPECTED = 33


def digest(data):
    return hashlib.sha256(data).hexdigest()


def objects(env):
    result = {o.path_id: o for o in env.objects}
    if len(result) != len(env.objects):
        raise ValueError('duplicate object ID')
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('action', choices=('template', 'restore'))
    p.add_argument('pc_data', type=Path)
    p.add_argument('input_asset', type=Path)
    p.add_argument('output', type=Path)
    p.add_argument('--manifest', type=Path)
    p.add_argument('--windows-source', action='store_true')
    a = p.parse_args()
    if a.windows_source and a.action != 'restore': p.error('--windows-source requires restore')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():
        p.error('output must be new')
    out = a.output.resolve()
    for source in (a.pc_data, a.input_asset):
        source = source.resolve()
        if out.is_relative_to(source) or source.is_relative_to(out):
            p.error('output must be separate from inputs')
    pc = a.pc_data / 'sharedassets4.assets'
    if digest(pc.read_bytes()) != (WINDOWS_PC if a.windows_source else PC):
        raise ValueError('incompatible Steam asset')
    raw_file = a.input_asset.read_bytes()
    if a.action == 'template':
        if digest(raw_file) != BASE:
            raise ValueError('incompatible input asset')
        manifest = None
    else:
        if not a.manifest:
            p.error('restore requires --manifest')
        manifest = json.loads(a.manifest.read_text())
        if (manifest.get('version') != 1 or manifest.get('base_sha256') != BASE
                or digest(raw_file) != manifest.get('template_sha256')):
            raise ValueError('invalid template or manifest')
        records = manifest['particles']
        if len(records) != EXPECTED or len({r['target_id'] for r in records}) != EXPECTED:
            raise ValueError('invalid particle inventory')
    targets = objects(UnityPy.load(raw_file))
    sources = objects(UnityPy.load(str(pc)))
    source_index = {}
    for o in sources.values():
        if o.type.name == 'ParticleSystem':
            b = o.get_raw_data()
            source_index.setdefault((len(b), digest(b[12:])), []).append(o.path_id)
    work = bytearray(raw_file)
    result = []
    selected = {} if manifest is None else {r['target_id']: r for r in records}
    for obj in targets.values():
        if obj.type.name != 'ParticleSystem':
            continue
        raw = obj.get_raw_data()
        if manifest is None:
            matches = sorted(source_index.get((len(raw), digest(raw[12:])), []))
            if not matches:
                continue
            source_id = matches[0]
        else:
            rec = selected.get(obj.path_id)
            if rec is None:
                continue
            source_id = rec['source_id']
            matches = sorted(source_index.get((len(raw), rec['body_sha256']), []))
        source = sources.get(source_id)
        if source is None or source.type.name != 'ParticleSystem' or not matches or source_id != matches[0]:
            raise ValueError('invalid source match')
        src = source.get_raw_data()
        body, offset = src[12:], obj.byte_start + 12
        rec = {'target_id': obj.path_id, 'source_id': source_id, 'offset': offset,
               'size': len(body), 'body_sha256': digest(body), 'candidate_count': len(matches)}
        if len(src) != len(raw) or offset + len(body) > len(work):
            raise ValueError('particle size mismatch')
        if manifest is None:
            if raw[12:] != body or not any(body):
                raise ValueError('particle body mismatch')
            work[offset:offset + len(body)] = bytes(len(body))
        else:
            if selected[obj.path_id] != rec or any(raw[12:]):
                raise ValueError('particle template mismatch')
            work[offset:offset + len(body)] = body
        result.append(rec)
    result.sort(key=lambda r: r['offset'])
    if len(result) != EXPECTED or any(x['offset'] + x['size'] > y['offset'] for x, y in zip(result, result[1:])):
        raise ValueError('unexpected or overlapping particle inventory')
    if manifest is not None and (result != records or digest(work) != BASE):
        raise ValueError('restored asset differs')
    check = objects(UnityPy.load(bytes(work)))
    if set(check) != set(targets):
        raise ValueError('object inventory changed')
    selected_ids = {r['target_id'] for r in result}
    for i, obj in targets.items():
        before, after = obj.get_raw_data(), check[i].get_raw_data()
        if obj.type.name != check[i].type.name or (i not in selected_ids and before != after) or (i in selected_ids and before[:12] != after[:12]):
            raise ValueError('unrelated bytes changed')
    a.output.mkdir()
    try:
        (a.output / 'sharedassets4.assets').write_bytes(work)
        report = {'version': 1, 'base_sha256': BASE,
                  'template_sha256': digest(work) if manifest is None else manifest['template_sha256'],
                  'particles': result, 'blanked_bytes': sum(r['size'] for r in result),
                  'ambiguous': sum(r['candidate_count'] > 1 for r in result),
                  'unchanged_objects': len(targets) - len(result),
                  'limitations': 'Local research only; other game content and code remain.'}
        (a.output / 'manifest.json').write_text(json.dumps(report, indent=2) + '\n')
        print(a.action, len(result), 'particles;', report['blanked_bytes'], 'body bytes; SHA256', digest(work))
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__ == '__main__':
    main()
