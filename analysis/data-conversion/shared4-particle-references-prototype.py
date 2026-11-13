#!/usr/bin/env python3
"""Rebuild three particle bodies while preserving Android subemitter pointers.

Local research only; the template contains other unreviewed game content.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import struct
import UnityPy

PC = 'cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3'
WINDOWS_PC = '896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9'
BASE = 'fb3896524cca9e8b137eb5d4c7c6f17eaca57d02185b8f1a31fcf83ab0fd99a0'
PAIRS = {132: (122, 126, 133), 145: (124, 149, 132), 146: (124, 150, 132)}
REF_OFFSET = 5372


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
            raise ValueError('incompatible base asset')
        manifest = None
    else:
        if not a.manifest:
            p.error('restore requires --manifest')
        manifest = json.loads(a.manifest.read_text())
        if manifest.get('version') != 1 or manifest.get('base_sha256') != BASE or digest(raw_file) != manifest.get('template_sha256'):
            raise ValueError('invalid template or manifest')
        if len(manifest['particles']) != len(PAIRS):
            raise ValueError('invalid particle count')
    targets = objects(UnityPy.load(raw_file))
    sources = objects(UnityPy.load(str(pc)))
    work, records = bytearray(raw_file), []
    for tid, (sid, target_ref, source_ref) in PAIRS.items():
        target, source = targets[tid], sources[sid]
        if target.type.name != 'ParticleSystem' or source.type.name != 'ParticleSystem':
            raise ValueError('unexpected particle type')
        raw, source_raw = target.get_raw_data(), source.get_raw_data()
        if len(raw) != len(source_raw) or len(raw) < REF_OFFSET + 12:
            raise ValueError('particle size mismatch')
        ptr, source_ptr = struct.pack('<iq', 0, target_ref), struct.pack('<iq', 0, source_ref)
        if raw[REF_OFFSET:REF_OFFSET+12] != ptr or source_raw[REF_OFFSET:REF_OFFSET+12] != source_ptr:
            raise ValueError('subemitter pointer mismatch')
        src = source.read()
        se = src.SubModule.subEmitters
        if len(se) != 1:
            raise ValueError('unexpected subemitter count')
        referenced_file, referenced_gameobject = struct.unpack('<iq', targets[target_ref].get_raw_data()[:12])
        if ((se[0].emitter.m_FileID, se[0].emitter.m_PathID) != (0, source_ref)
                or referenced_file != 0 or targets[referenced_gameobject].type.name != 'GameObject'
                or targets[referenced_gameobject].read().m_Name != 'Trail'
                or se[0].emitter.read().m_GameObject.read().m_Name != 'Trail'):
            raise ValueError('subemitter metadata mismatch')
        if manifest is None:
            de = target.read().SubModule.subEmitters
            if (len(de) != 1 or (de[0].emitter.m_FileID, de[0].emitter.m_PathID) != (0, target_ref)
                    or (de[0].type, de[0].properties, de[0].emitProbability) != (se[0].type, se[0].properties, se[0].emitProbability)):
                raise ValueError('Android subemitter metadata mismatch')
        body = bytearray(source_raw[12:])
        body[REF_OFFSET-12:REF_OFFSET] = ptr
        blank = bytearray(len(body))
        blank[REF_OFFSET-12:REF_OFFSET] = ptr
        start = target.byte_start + 12
        rec = {'target_id': tid, 'source_id': sid, 'offset': start, 'size': len(body),
               'pointer_offset': REF_OFFSET, 'android_path_id': target_ref, 'steam_path_id': source_ref,
               'body_sha256': digest(body), 'pointer_sha256': digest(ptr)}
        if start + len(body) > len(work):
            raise ValueError('invalid particle range')
        if manifest is None:
            if raw[12:] != body:
                raise ValueError('particle body mismatch outside pointer')
            work[start:start+len(body)] = blank
        else:
            if manifest['particles'][len(records)] != rec or raw[12:] != blank:
                raise ValueError('particle template mismatch')
            work[start:start+len(body)] = body
        records.append(rec)
    if manifest is not None and digest(work) != BASE:
        raise ValueError('restored asset differs')
    check = objects(UnityPy.load(bytes(work)))
    if set(check) != set(targets):
        raise ValueError('object inventory changed')
    for i, obj in targets.items():
        old, new = obj.get_raw_data(), check[i].get_raw_data()
        if (obj.type.name != check[i].type.name or (i not in PAIRS and old != new)
                or (i in PAIRS and (old[:12] != new[:12] or old[REF_OFFSET:REF_OFFSET+12] != new[REF_OFFSET:REF_OFFSET+12]))):
            raise ValueError('unrelated content or pointer changed')
    a.output.mkdir()
    try:
        (a.output/'sharedassets4.assets').write_bytes(work)
        report = {'version': 1, 'base_sha256': BASE,
                  'template_sha256': digest(work) if manifest is None else manifest['template_sha256'],
                  'particles': records, 'blanked_bytes': sum(r['size'] - 12 for r in records),
                  'unchanged_objects': len(targets) - len(records),
                  'limitations': 'Local diagnostic only; other game content and code remain.'}
        (a.output/'manifest.json').write_text(json.dumps(report, indent=2)+'\n')
        print(a.action, len(records), 'particles;', report['blanked_bytes'], 'body bytes; SHA256', digest(work))
    except BaseException:
        shutil.rmtree(a.output)
        raise


if __name__ == '__main__':
    main()
