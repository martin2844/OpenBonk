#!/usr/bin/env python3
"""Rebuild additional exact scene component bodies from pinned Steam scenes.

Keeps the Android component headers. Matches can be ambiguous and neither the
template nor the restored files establish redistribution rights.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
INVENTORY=ROOT/'build/android/no-rewired-scene-component-suffixes.json'
INVENTORY_SHA='61e581117f2fe2c87871090145acc40184af5603e32303a56bc636571a55bd64'
FIRST=ROOT/'build/android/no-rewired-scene-template'
FIRST_MANIFEST_SHA='a77039c682ed1651c2b9a2d195dffff33b2bfad49cd1da42278bc2152bfa854f'
EXPECTED={'level1':1341,'level2':1179,'level4':1090}
spec=importlib.util.spec_from_file_location('scene_base',Path(__file__).with_name('no-rewired-scene-bodies-prototype.py'));base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)


def digest(b):return hashlib.sha256(b).hexdigest()
def require(p,h):
    if not p.is_file() or digest(p.read_bytes())!=h:raise ValueError('incompatible input: '+str(p))


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();out=a.output.resolve();pc=a.pc_data.resolve();source=a.input.resolve()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    if out.is_relative_to(pc) or pc.is_relative_to(out) or out.is_relative_to(source) or source.is_relative_to(out):p.error('output must be separate from inputs')
    require(INVENTORY,INVENTORY_SHA);require(FIRST/'manifest.json',FIRST_MANIFEST_SHA)
    for name,h in base.PC_PINS.items():require(pc/name,h)
    inv=json.loads(INVENTORY.read_text());first=json.loads((FIRST/'manifest.json').read_text())
    if inv['errors'] or {r['scene'] for r in inv['scenes']}!=set(EXPECTED):raise ValueError('invalid inventory')
    prior=None if a.action=='template' else json.loads((a.input/'extra-manifest.json').read_text())
    if prior is not None and (prior.get('version')!=1 or set(prior.get('scenes',{}))!=set(EXPECTED)):raise ValueError('invalid extra manifest')
    a.output.mkdir();summary={}
    try:
        for row in inv['scenes']:
            scene=row['scene'];first_scene=first['scenes'][scene]
            require(FIRST/scene,first_scene['template_sha256'])
            source_path=a.input/scene
            require(source_path,first_scene['template_sha256'] if prior is None else prior['scenes'][scene]['template_sha256'])
            raw_file=source_path.read_bytes();targets=base.objs(UnityPy.load(raw_file));sources=base.objs(UnityPy.load(str(pc/scene)));work=bytearray(raw_file)
            previous={r['target_id'] for r in first_scene['records'] if r['kind']=='mono'};records=[];ambiguous=0
            candidates=[r for r in row['rows'] if r['type']=='MonoBehaviour' and r['target_id'] not in previous]
            prior_rows=None if prior is None else {x['target_id']:x for x in prior['scenes'][scene]['records']}
            if prior_rows is not None and len(prior_rows)!=len(prior['scenes'][scene]['records']):raise ValueError('duplicate selected component')
            for r in candidates:
                if prior_rows is not None and r['target_id'] not in prior_rows:continue
                target=targets[r['target_id']];source_obj=sources[min(r['source_ids'])];raw=target.get_raw_data();src=source_obj.get_raw_data();prefix=28
                if target.type.name!='MonoBehaviour' or source_obj.type.name!='MonoBehaviour' or r['prefix']!=prefix or len(raw)!=r['size'] or len(src)!=len(raw):raise ValueError('component identity mismatch')
                body=src[prefix:];offset=target.byte_start+prefix
                if prior_rows is None:
                    if not any(raw[prefix:]):continue
                    if digest(raw)!=r['target_sha256'] or raw[prefix:]!=body:raise ValueError('component body mismatch')
                    work[offset:offset+len(body)]=bytes(len(body))
                else:
                    if any(raw[prefix:]):raise ValueError('template body not empty')
                    work[offset:offset+len(body)]=body
                ambiguous+=len(r['source_ids'])>1
                records.append({'target_id':r['target_id'],'source_id':source_obj.path_id,'offset':offset,'size':len(body),'body_sha256':digest(body),'candidate_count':len(r['source_ids'])})
            if len(records)!=EXPECTED[scene]:raise ValueError('unexpected component count in '+scene)
            records.sort(key=lambda x:x['offset'])
            if any(x['offset']+x['size']>y['offset'] for x,y in zip(records,records[1:])):raise ValueError('overlapping body ranges')
            if prior is not None:
                if records!=prior['scenes'][scene]['records'] or digest(work)!=first_scene['template_sha256']:raise ValueError('restored intermediate template differs')
            (a.output/scene).write_bytes(work)
            summary[scene]={'template_sha256':digest(work) if prior is None else prior['scenes'][scene]['template_sha256'],'count':len(records),'ambiguous':ambiguous,'body_bytes':sum(x['size'] for x in records),'records':records}
        if prior is None:
            (a.output/'extra-manifest.json').write_text(json.dumps({'version':1,'kind':'local research only, not distributable','scenes':summary},indent=2)+'\n')
        else:
            shutil.copyfile(FIRST/'manifest.json',a.output/'manifest.json')
        print(a.action,json.dumps({s:{k:v for k,v in r.items() if k!='records'} for s,r in summary.items()}),flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise


if __name__=='__main__':main()
