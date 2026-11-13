#!/usr/bin/env python3
"""Generate offset-only plans for pinned scene reconstruction, local research."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import struct
import sys
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'build/android/localization-shared-scrubbed-base'
LINUX=ROOT/'build/android/linux-steam-regression.zip'
WINDOWS=ROOT/'build/android/windows-steam-full-input.zip'
OUTPUT=ROOT/'build/android/scene-android-copy-plan/scene-plan.bin'
SCENES=('level1','level2','level4')
WINDOWS_HASHES={'level1':'9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c','level2':'853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6','level4':'d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2'}

def load(name,path):
    s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def sha(data):return hashlib.sha256(data).hexdigest()
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--base',type=Path,default=BASE);p.add_argument('--linux-zip',type=Path,default=LINUX)
    p.add_argument('--windows-zip',type=Path,default=WINDOWS);p.add_argument('--output',type=Path,default=OUTPUT)
    p.add_argument('--complete-scenes',action='store_true',help='include scene component and particle body stages')
    a=p.parse_args();base_dir=a.base.resolve(strict=True);linux_zip=a.linux_zip.resolve(strict=True);windows_zip=a.windows_zip.resolve(strict=True);output=a.output.absolute()
    if output.exists() or output.is_symlink():raise ValueError('output exists')
    if output.resolve().is_relative_to(base_dir) or output.resolve() in (linux_zip,windows_zip):raise ValueError('output must be separate from inputs')
    if sha((base_dir/'base.obb').read_bytes())!='ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d':raise ValueError('base mismatch')
    game=load('game',ROOT/'analysis/data-conversion/scene-gameobject-values-template.py')
    trans=load('trans',ROOT/'analysis/data-conversion/scene-transform-values-template.py')
    gm=json.loads((base_dir/'gameobject-manifest.json').read_text())['scenes']
    tm=json.loads((base_dir/'transform-manifest.json').read_text())['scenes']
    pins=json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())
    if a.complete_scenes:
        body=load('scene_bodies',ROOT/'analysis/data-conversion/no-rewired-scene-bodies-prototype.py')
        first_path=ROOT/'build/android/no-rewired-scene-template/manifest.json'
        extra_path=ROOT/'build/android/no-rewired-scene-extra-template/extra-manifest.json'
        if sha(first_path.read_bytes())!='a77039c682ed1651c2b9a2d195dffff33b2bfad49cd1da42278bc2152bfa854f' or sha(extra_path.read_bytes())!='3c2455aaf75c2fe9a9a1a8f649d8c770dd6ae4db16560dce01ee0703034a7775':raise ValueError('scene body manifest mismatch')
        first=json.loads(first_path.read_bytes())['scenes'];extra=json.loads(extra_path.read_bytes())['scenes'];body_plans={}
        for kind,(folder,expected_hash) in body.MANIFESTS.items():
            path=ROOT/'build/android/local-research-manifests'/folder/'manifest.json';raw=path.read_bytes()
            if sha(raw)!=expected_hash:raise ValueError('scene body mapping mismatch: '+kind)
            doc=json.loads(raw);body_plans[kind]={x['scene']:{o['target_id']:o for o in x['objects']} for x in doc['scenes']}
    result=bytearray((b'OBSCN002' if a.complete_scenes else b'OBSCN001')+struct.pack('>I',len(SCENES)))
    with zipfile.ZipFile(base_dir/'base.obb') as base,zipfile.ZipFile(linux_zip) as linux,zipfile.ZipFile(windows_zip) as windows:
        for scene in SCENES:
            prefix='assets/bin/Data/'+scene+'.split'
            parts=sorted((n for n in base.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
            if [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('bad scene parts')
            target=b''.join(base.read(n) for n in parts)
            sources=[linux.read('Megabonk_Data/'+scene),windows.read('Megabonk_Data/'+scene)]
            if sha(target)!=game.PARTIAL_BLANK[scene] or sha(sources[0])!=pins[scene]['sha256'] or sha(sources[1])!=WINDOWS_HASHES[scene]:raise ValueError('source pin mismatch')
            targets={o.path_id:o for o in UnityPy.load(target).objects}
            source_objects=[{o.path_id:o for o in UnityPy.load(data).objects} for data in sources]
            stages=[];work=bytearray(target);all_spans=[]
            for records,kind in ((gm[scene]['objects'],'game'),(tm[scene]['objects'],'transform')):
                plan=[]
                for record in records:
                    dst=targets[record['target_id']];src=[x[record['steam_id']] for x in source_objects]
                    if kind=='game':
                        if dst.type.name!='GameObject' or any(x.type.name!='GameObject' for x in src):raise ValueError('type mismatch')
                        values=[game.value(x) for x in src];raw=dst.get_raw_data();offset=4+12*struct.unpack_from('<I',raw)[0]
                        if values[0][1]!=values[1][1] or sha(values[0][1])!=record['value_sha256'] or offset+len(values[0][1])!=len(raw):raise ValueError('value mismatch')
                        spans=[(dst.byte_start+offset,src[0].byte_start+values[0][0],src[1].byte_start+values[1][0],len(values[0][1]))]
                    else:
                        if dst.type.name!=record['type'] or any(x.type.name!=record['type'] for x in src):raise ValueError('type mismatch')
                        values=[trans.audit.vectors(x) for x in src]
                        if values[0]!=values[1] or sha(b''.join(values[0]))!=record['value_sha256']:raise ValueError('transform mismatch')
                        spans=[(dst.byte_start+12,src[0].byte_start+12,src[1].byte_start+12,40)]
                        if record['type']=='RectTransform':spans.append((dst.byte_start+dst.byte_size-40,src[0].byte_start+src[0].byte_size-40,src[1].byte_start+src[1].byte_size-40,40))
                    for dest,l,w,length in spans:
                        if length<=0 or dest+length>len(work) or l+length>len(sources[0]) or w+length>len(sources[1]) or any(work[dest:dest+length]):raise ValueError('invalid span')
                        value=sources[0][l:l+length]
                        if value!=sources[1][w:w+length]:raise ValueError('cross-platform span mismatch')
                        work[dest:dest+length]=value;plan.append((dest,l,w,length));all_spans.append((dest,dest+length))
                expected=game.PARTIAL_RESTORED[scene] if kind=='game' else trans.PARTIAL_RESTORED[scene]
                if sha(work)!=expected:raise ValueError('restored scene mismatch')
                stages.append(plan)
            if a.complete_scenes:
                if sha(work)!=extra[scene]['template_sha256']:raise ValueError('unexpected extra component template')
                plan=[]
                for record in extra[scene]['records']:
                    dst=targets[record['target_id']];src=[x[record['source_id']] for x in source_objects]
                    if dst.type.name!='MonoBehaviour' or any(o.type.name!='MonoBehaviour' for o in src):raise ValueError('extra component type mismatch')
                    off=record['offset'];length=record['size'];data=[x.get_raw_data()[28:] for x in src]
                    if off!=dst.byte_start+28 or length!=dst.byte_size-28 or data[0]!=data[1] or len(data[0])!=length or sha(data[0])!=record['body_sha256'] or any(work[off:off+length]):raise ValueError('extra component mismatch')
                    l=src[0].byte_start+28;w=src[1].byte_start+28
                    work[off:off+length]=data[0];plan.append((off,l,w,length));all_spans.append((off,off+length))
                if sha(work)!=first[scene]['template_sha256']:raise ValueError('extra component output mismatch')
                stages.append(plan);plan=[]
                for record in first[scene]['records']:
                    kind=record['kind'];item=body_plans[kind][scene][record['target_id']]
                    dst=targets[record['target_id']];src=[x[item['source_id']] for x in source_objects]
                    prefix_size=28 if kind=='mono' else 12;expected_type='MonoBehaviour' if kind=='mono' else 'ParticleSystem'
                    if dst.type.name!=expected_type or any(o.type.name!=expected_type for o in src):raise ValueError('body type mismatch')
                    length=record['size'];start=record['offset'];data=[bytearray(x.get_raw_data()[prefix_size:]) for x in src]
                    if start!=dst.byte_start+prefix_size or any(len(x)!=length for x in data) or length!=dst.byte_size-prefix_size:raise ValueError('body size mismatch')
                    refs=sorted((r['offset']-prefix_size,r['size'],r['pointer_sha256']) for r in item.get('references',[]))
                    end=0
                    for pos,size,pointer_hash in refs:
                        if size!=12 or pos<end or pos+size>length or sha(work[start+pos:start+pos+size])!=pointer_hash:raise ValueError('body pointer mismatch')
                        pointer=work[start+pos:start+pos+size]
                        for d in data:d[pos:pos+size]=pointer
                        end=pos+size
                    if data[0]!=data[1] or sha(data[0])!=item['body_sha256']:raise ValueError('body content mismatch')
                    cursor=0
                    for end in [pos for pos,_,_ in refs]+[length]:
                        if end>cursor:
                            dest=start+cursor;l=src[0].byte_start+prefix_size+cursor;w=src[1].byte_start+prefix_size+cursor;size=end-cursor
                            if any(work[dest:dest+size]) or sources[0][l:l+size]!=sources[1][w:w+size]:raise ValueError('body range mismatch')
                            work[dest:dest+size]=sources[0][l:l+size];plan.append((dest,l,w,size));all_spans.append((dest,dest+size))
                        if end!=length:
                            ref=next(r for r in refs if r[0]==end);cursor=end+ref[1]
                if sha(work)!=body.TARGET_PINS[scene]:raise ValueError('complete scene output mismatch')
                stages.append(plan)
            spans=sorted(all_spans)
            if any(spans[i][1]>spans[i+1][0] for i in range(len(spans)-1)):raise ValueError('overlapping spans')
            result.extend(scene.encode().ljust(8,b'\0'))
            for h in (pins[scene]['sha256'],WINDOWS_HASHES[scene],game.PARTIAL_BLANK[scene],game.PARTIAL_RESTORED[scene],trans.PARTIAL_RESTORED[scene]):result.extend(bytes.fromhex(h))
            if a.complete_scenes:
                result.extend(bytes.fromhex(first[scene]['template_sha256']));result.extend(bytes.fromhex(body.TARGET_PINS[scene]))
            sizes=[len(sources[0]),len(sources[1]),len(target),*(len(x) for x in stages)]
            result.extend(struct.pack('>'+'I'*len(sizes),*sizes))
            for plan in stages:
                for row in plan:result.extend(struct.pack('>IIII',*row))
            print(scene,'stage ranges',*[len(x) for x in stages],flush=True)
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('xb') as f:f.write(result)
    print('plan bytes',len(result),'sha256',sha(result),'offsets only; research prototype')
if __name__=='__main__':main()
