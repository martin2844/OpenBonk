#!/usr/bin/env python3
"""Blank/rebuild matching scene bodies while preserving new Android pointers.

Research only. Templates and restored scenes retain other game data and are
not distributable. The existing PC-to-Android matches can be ambiguous.
"""
import argparse
import collections
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
BASE=Path(os.environ.get('OPENBONK_MANIFEST_ROOT',str(ROOT/'build/android/share-source-experiment'))).expanduser()
OBB_SHA='ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
PC_PINS={'level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551','level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8','level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600'}
WINDOWS_SCENE_PINS={'level1':'9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','level2':'853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6','level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}
TARGET_PINS={'level1':'164012e485062f572bba37de85aafe42a494810202d80e13d4f72dbf20ce24dc','level2':'bbee6f3c886a795f0029626ed62ccc6d569fca3ce9ee821876619d3009a55dea','level4':'665e167cbcc2fd9c43c46e1d2c84f83c6ca9e0ad6c9756f439e10b90d5758972'}
MANIFESTS={'particle':('scene-particle-bodies-template','733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be'), 'references':('scene-particle-references-template','cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0'), 'mono':('scene-monobehaviour-bodies-template','b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4')}
EXPECTED={'level1':{'particle':46,'references':7,'mono':227},'level2':{'particle':176,'references':10,'mono':14},'level4':{'particle':175,'references':22,'mono':75}}
spec=importlib.util.spec_from_file_location('archive_compare',Path(__file__).with_name('compare-android-asset-objects.py'));mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)


def digest(b):return hashlib.sha256(b).hexdigest()
def require(path,expected):
    if not path.is_file() or digest(path.read_bytes())!=expected:raise ValueError('incompatible input: '+str(path))
def objs(env):
    result={}
    for x in env.objects:
        if x.path_id in result:raise ValueError('duplicate object ID')
        result[x.path_id]=x
    return result


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('action',choices=('template','restore'));p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path)
    p.add_argument('--windows-scenes',action='store_true',help='experimentally restore selected bodies from the pinned Windows scenes')
    a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve();pc=a.pc_data.resolve();source=a.input.resolve()
    if out.is_relative_to(pc) or pc.is_relative_to(out) or out.is_relative_to(source) or source.is_relative_to(out):p.error('output must be separate from input and Steam data')
    if a.windows_scenes and a.action!='restore':p.error('--windows-scenes only supports restoration')
    for scene,h in (WINDOWS_SCENE_PINS if a.windows_scenes else PC_PINS).items():require(a.pc_data/scene,h)
    plans={}
    for kind,(folder,h) in MANIFESTS.items():
        f=BASE/folder/'manifest.json';require(f,h);m=json.loads(f.read_text());plans[kind]={x['scene']:x['objects'] for x in m['scenes']}
        if set(plans[kind])!=set(PC_PINS):raise ValueError('unexpected scene manifest')
    if a.action=='template':
        require(a.input,OBB_SHA);z=zipfile.ZipFile(a.input)
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        prior=None
    else:
        z=None;prior=json.loads((a.input/'manifest.json').read_text())
        if prior.get('version')!=1 or set(prior.get('scenes',{}))!=set(PC_PINS):raise ValueError('invalid template manifest')
    a.output.mkdir(); summaries={}
    try:
        for scene in PC_PINS:
            original=mod.load_archive(z,scene) if z else (a.input/scene).read_bytes()
            if z and digest(original)!=TARGET_PINS[scene]:raise ValueError('unexpected target scene')
            if not z and digest(original)!=prior['scenes'][scene]['template_sha256']:raise ValueError('template hash mismatch')
            targets=objs(UnityPy.load(original));sources=objs(UnityPy.load(str(a.pc_data/scene)));work=bytearray(original);records=[];counts=collections.Counter();skipped=collections.Counter()
            selected=None if z else {(r['kind'],r['target_id']) for r in prior['scenes'][scene]['records']}
            if selected is not None and len(selected)!=len(prior['scenes'][scene]['records']):raise ValueError('duplicate selected object')
            for kind in ('particle','references','mono'):
                for item in plans[kind][scene]:
                    if selected is not None and (kind,item['target_id']) not in selected:continue
                    target=targets.get(item['target_id']);source=sources.get(item['source_id']);prefix=28 if kind=='mono' else 12;typ='MonoBehaviour' if kind=='mono' else 'ParticleSystem'
                    if not target or not source or target.type.name!=typ or source.type.name!=typ:
                        if kind=='mono':skipped['missing_or_type']+=1;continue
                        raise ValueError('particle object missing or wrong type')
                    raw=target.get_raw_data();src=source.get_raw_data()
                    if len(raw)!=len(src) or len(raw)!=prefix+item['size']:
                        if kind=='mono':skipped['size']+=1;continue
                        raise ValueError('particle size mismatch')
                    source_body=bytearray(src[prefix:]); refs=item.get('references',[]); start=target.byte_start+prefix
                    for ref in refs:
                        pos=ref['offset']-prefix;size=ref['size']
                        if size!=12 or pos<0 or pos+size>len(source_body):raise ValueError('invalid preserved pointer')
                        ptr=raw[prefix+pos:prefix+pos+size]
                        if digest(ptr)!=ref['pointer_sha256']:raise ValueError('preserved pointer mismatch')
                        source_body[pos:pos+size]=ptr
                    if digest(bytes(source_body))!=item['body_sha256']:raise ValueError('source body hash mismatch')
                    if z:
                        if raw[prefix:]!=source_body:
                            if kind=='mono':skipped['body']+=1;continue
                            raise ValueError('particle body mismatch')
                        blank=bytearray(len(source_body))
                        for ref in refs:
                            pos=ref['offset']-prefix;blank[pos:pos+ref['size']]=raw[prefix+pos:prefix+pos+ref['size']]
                        work[start:start+len(blank)]=blank
                    else:
                        blank=bytearray(len(source_body))
                        for ref in refs:
                            pos=ref['offset']-prefix;blank[pos:pos+ref['size']]=raw[prefix+pos:prefix+pos+ref['size']]
                        if raw[prefix:]!=blank:raise ValueError('unexpected nonzero template bytes')
                        work[start:start+len(source_body)]=source_body
                    counts[kind]+=1;records.append({'offset':start,'size':len(source_body),'target_id':item['target_id'],'kind':kind})
            if dict(counts)!=EXPECTED[scene]:raise ValueError(f'unexpected reconstructed counts in {scene}: {dict(counts)}')
            records.sort(key=lambda x:x['offset'])
            if any(x['offset']+x['size']>y['offset'] for x,y in zip(records,records[1:])):raise ValueError('overlapping scene bodies')
            if not z and records!=prior['scenes'][scene]['records']:raise ValueError('selected scene bodies differ')
            if not z and digest(work)!=TARGET_PINS[scene]:raise ValueError('restored scene does not match new build')
            (a.output/scene).write_bytes(work)
            summaries[scene]={'template_sha256':digest(work) if z else prior['scenes'][scene]['template_sha256'],'original_sha256':TARGET_PINS[scene],'counts':dict(counts),'skipped_mono':dict(skipped) if z else prior['scenes'][scene]['skipped_mono'],'selected_body_bytes':sum(x['size'] for x in records),'records':records}
        (a.output/'manifest.json').write_text(json.dumps({'version':1,'kind':'local research, not for distribution','scenes':summaries},indent=2)+'\n')
        print(a.action,json.dumps({k:{x:y for x,y in v.items() if x!='records'} for k,v in summaries.items()}),flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise
    finally:
        if z:z.close()


if __name__=='__main__':main()
