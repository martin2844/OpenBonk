#!/usr/bin/env python3
"""Rebuild two final-map animation streams from pinned Steam data.

Steam and Android curves differ numerically; this is experimental and requires
runtime validation. Other game data remains; output is not distributable.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import shutil
import struct

import UnityPy

PC='e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da'
WINDOWS_PC='cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9'
BASE='e4d95de778e37ec5422ea5394aaf8e028ad5a11e145ec40dc1d1fb7a83f78d62'
PAIRS=((455,247,'MonkeBreakdance'),(456,249,'MovingBushQuest'))


def digest(data):return hashlib.sha256(data).hexdigest()


def objects(env):
    result={o.path_id:o for o in env.objects}
    if len(result)!=len(env.objects):raise ValueError('duplicate object ID')
    return result


def stream(tree):return tree['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']


def pack(words):return struct.pack('<%dI'%len(words),*words)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output',type=Path);p.add_argument('--manifest',type=Path)
    p.add_argument('--windows-source',action='store_true',help='restore from the pinned Windows asset')
    a=p.parse_args()
    if a.windows_source and a.action!='restore':p.error('--windows-source requires restore')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve()
    for path in (a.pc_data,a.input_asset):
        path=path.resolve()
        if out.is_relative_to(path) or path.is_relative_to(out):p.error('output must be separate from inputs')
    source=a.pc_data/'sharedassets2.assets'
    if digest(source.read_bytes())!=(WINDOWS_PC if a.windows_source else PC):raise ValueError('incompatible Steam input')
    original=a.input_asset.read_bytes();manifest=None
    if a.action=='template':
        if digest(original)!=BASE:raise ValueError('incompatible Android input')
    else:
        if not a.manifest:p.error('restore requires --manifest')
        manifest=json.loads(a.manifest.read_text())
        if manifest.get('version')!=1 or manifest.get('base_sha256')!=BASE or digest(original)!=manifest.get('template_sha256') or len(manifest.get('animations',[]))!=len(PAIRS):raise ValueError('invalid template or manifest')
    targets=objects(UnityPy.load(original));sources=objects(UnityPy.load(str(source)));work=bytearray(original);records=[]
    for tid,sid,name in PAIRS:
        target,src=targets[tid],sources[sid]
        if target.type.name!='AnimationClip' or src.type.name!='AnimationClip':raise ValueError('unexpected object type')
        x,y=target.read_typetree(),src.read_typetree()
        if x['m_Name']!=name or y['m_Name']!=name:raise ValueError('animation name mismatch')
        xb,yb=pack(stream(x)),pack(stream(y))
        xmeta,ymeta=copy.deepcopy(x),copy.deepcopy(y)
        xmeta['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
        ymeta['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
        if xmeta!=ymeta or len(xb)!=len(yb) or not xb:raise ValueError('animation metadata mismatch')
        if manifest is None:
            raw=target.get_raw_data()
            if raw.count(xb)!=1 or not any(xb):raise ValueError('ambiguous or empty stream')
            offset=target.byte_start+raw.index(xb)
        else:
            rec=manifest['animations'][len(records)];offset=rec['offset']
            if offset<target.byte_start or offset+len(xb)>target.byte_start+target.byte_size:raise ValueError('stream outside object')
        record={'target_id':tid,'source_id':sid,'name':name,'offset':offset,'size':len(yb),'source_sha256':digest(yb)}
        if manifest is None:
            if work[offset:offset+len(xb)]!=xb:raise ValueError('target stream mismatch')
            work[offset:offset+len(xb)]=bytes(len(xb))
        else:
            if manifest['animations'][len(records)]!=record or any(xb) or work[offset:offset+len(xb)]!=xb:raise ValueError('animation template mismatch')
            work[offset:offset+len(yb)]=yb
        records.append(record)
    check=objects(UnityPy.load(bytes(work)))
    selected={x[0] for x in PAIRS}
    if set(check)!=set(targets) or any(check[i].type.name!=o.type.name or (i not in selected and check[i].get_raw_data()!=o.get_raw_data()) for i,o in targets.items()):raise ValueError('unrelated object changed')
    for rec in records:
        b=pack(stream(check[rec['target_id']].read_typetree()))
        if (manifest is None and any(b)) or (manifest is not None and digest(b)!=rec['source_sha256']):raise ValueError('saved animation stream mismatch')
    a.output.mkdir()
    try:
        result=a.output/'sharedassets2.assets';result.write_bytes(work)
        report={'version':1,'base_sha256':BASE,'template_sha256':digest(work) if manifest is None else manifest['template_sha256'],
                'result_sha256':digest(work),'animations':records,'unchanged_objects':len(targets)-len(PAIRS),
                'selected_stream_bytes':sum(r['size'] for r in records),
                'limitations':'Streams differ numerically; other game content remains, local research only.'}
        (a.output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
        print(a.action,len(records),'animation streams;',report['selected_stream_bytes'],'bytes; SHA256',digest(work),flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise


if __name__=='__main__':main()
