#!/usr/bin/env python3
"""Reconstruct six TMP font metadata objects from pinned Steam data.

Difference patches retain Android-derived bytes and need provenance review.
The template retains other game data and is not a distribution package.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

import UnityPy

PC='01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a'
BASE='009ad342794b4b5289efe4ab9e814de2374caf70248f05e17719d59d576cc80a'
PAIRS=tuple(zip(range(131,137),range(133,139)))
NAMES=('NotoSans-Bold SDF','NotoSansJP-Bold SDF','NotoSansKR-Bold SDF','NotoSansSC-Bold SDF','NotoSansTC-Bold SDF','NotoSansThai_Condensed-Bold SDF')


def digest(b):return hashlib.sha256(b).hexdigest()


def objects(env):
    result={o.path_id:o for o in env.objects}
    if len(result)!=len(env.objects):raise ValueError('duplicate object ID')
    return result


def name(raw):
    n=int.from_bytes(raw[28:32],'little')
    if n>128 or 32+n>len(raw):raise ValueError('invalid object name')
    return raw[32:32+n].decode('utf-8')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input_asset',type=Path);p.add_argument('output',type=Path);p.add_argument('--manifest',type=Path)
    a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve()
    for source in (a.pc_data,a.input_asset):
        source=source.resolve()
        if out.is_relative_to(source) or source.is_relative_to(out):p.error('output must be separate from inputs')
    steam=a.pc_data/'sharedassets0.assets'
    if digest(steam.read_bytes())!=PC:raise ValueError('incompatible Steam input')
    data=a.input_asset.read_bytes();manifest=None
    if a.action=='template':
        if digest(data)!=BASE:raise ValueError('incompatible Android asset')
    else:
        if not a.manifest:p.error('restore requires --manifest')
        manifest=json.loads(a.manifest.read_text())
        if manifest.get('version')!=1 or manifest.get('base_sha256')!=BASE or digest(data)!=manifest.get('template_sha256') or len(manifest.get('objects',[]))!=len(PAIRS):raise ValueError('invalid template or manifest')
    targets=objects(UnityPy.load(data));sources=objects(UnityPy.load(str(steam)));work=bytearray(data);records=[]
    for i,((tid,sid),expected_name) in enumerate(zip(PAIRS,NAMES)):
        target,source=targets[tid],sources[sid]
        if target.type.name!='MonoBehaviour' or source.type.name!='MonoBehaviour':raise ValueError('font metadata type mismatch')
        old,src=target.get_raw_data(),source.get_raw_data();offset=target.byte_start
        if len(old)!=len(src) or name(src)!=expected_name or offset+len(old)>len(work):raise ValueError('font metadata mismatch')
        if manifest is None:
            if name(old)!=expected_name:raise ValueError('Android font name mismatch')
            edits=[[n,x] for n,(x,y) in enumerate(zip(old,src)) if x!=y]
            if not 1<=len(edits)<=32:raise ValueError('unexpected reference patch count')
            rec={'target_id':tid,'source_id':sid,'name':expected_name,'offset':offset,'size':len(old),
                 'source_sha256':digest(src),'target_sha256':digest(old),'edits':edits}
            work[offset:offset+len(old)]=bytes(len(old))
        else:
            rec=manifest['objects'][i]
            if (rec['target_id'],rec['source_id'],rec['name'],rec['offset'],rec['size'],rec['source_sha256'])!=(tid,sid,expected_name,offset,len(src),digest(src)) or any(old):raise ValueError('font template mismatch')
            rebuilt=bytearray(src);last=-1
            for pos,value in rec['edits']:
                if not isinstance(pos,int) or pos<=last or pos>=len(rebuilt) or not isinstance(value,int) or not 0<=value<=255 or rebuilt[pos]==value:raise ValueError('invalid reference patch')
                rebuilt[pos]=value;last=pos
            if not 1<=len(rec['edits'])<=32 or digest(rebuilt)!=rec['target_sha256']:raise ValueError('font reconstruction mismatch')
            work[offset:offset+len(rebuilt)]=rebuilt
        records.append(rec)
    if manifest is not None and digest(work)!=BASE:raise ValueError('restored asset differs')
    check=objects(UnityPy.load(bytes(work)));selected={t for t,_ in PAIRS}
    if set(check)!=set(targets) or any(check[i].type.name!=o.type.name or (i not in selected and check[i].get_raw_data()!=o.get_raw_data()) for i,o in targets.items()):raise ValueError('unrelated objects changed')
    if manifest is None and any(any(check[i].get_raw_data()) for i in selected):raise ValueError('font object not blank')
    a.output.mkdir()
    try:
        (a.output/'sharedassets0.assets').write_bytes(work)
        report={'version':1,'base_sha256':BASE,'template_sha256':digest(work) if manifest is None else manifest['template_sha256'],
                'objects':records,'selected_bytes':sum(r['size'] for r in records),'patch_bytes':sum(len(r['edits']) for r in records),
                'unchanged_objects':len(targets)-len(PAIRS),'limitations':'Android-derived patch bytes and other game content remain; not distributable.'}
        (a.output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
        print(a.action,len(records),'font objects;',report['selected_bytes'],'bytes;',report['patch_bytes'],'patch bytes; SHA256',digest(work),flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise


if __name__=='__main__':main()
