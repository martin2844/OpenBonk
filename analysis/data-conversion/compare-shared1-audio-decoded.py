#!/usr/bin/env python3
"""Compare 118 rebuilt clips with pinned Steam and original Android audio.

FMOD decodes locally; no audio samples are exported. Correlation is sampled
and does not prove perceptual fidelity, Android playback or redistribution rights.
"""
import argparse
from collections import defaultdict
import hashlib
import importlib.util
import json
from pathlib import Path
import zipfile

import UnityPy

spec=importlib.util.spec_from_file_location('audio_compare',Path(__file__).with_name('compare-hashed-audio-decoded.py'))
audio=importlib.util.module_from_spec(spec);spec.loader.exec_module(audio)
OLD='ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
NEW='d47ca939431bfe4555c195155f29ea66303f24b848b2c90a8822d91d305c84ca'
STEAM_ASSET='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STEAM_RESOURCE='28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'


def clips(data):
    result=defaultdict(list)
    for obj in UnityPy.load(data).objects:
        if obj.type.name=='AudioClip':
            clip=obj.read();result[clip.m_Name].append(clip)
    return result


def packaged(path):
    with zipfile.ZipFile(path) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        prefix='assets/bin/Data/sharedassets1.assets.split'
        parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
        if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid asset fragments')
        return clips(b''.join(z.read(n) for n in parts)),z.read('assets/bin/Data/sharedassets1.resource')


def payload(resource,clip):
    ref=clip.m_Resource
    if ref.m_Source!='sharedassets1.resource' or ref.m_Offset<0 or ref.m_Size<=0 or ref.m_Offset+ref.m_Size>len(resource):raise ValueError('invalid resource range')
    result=resource[ref.m_Offset:ref.m_Offset+ref.m_Size]
    if result[:4]!=b'FSB5':raise ValueError('missing FSB5 header')
    return result


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('old_obb',type=Path);p.add_argument('rebuilt_obb',type=Path);p.add_argument('pc_data',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    for path,expected in ((a.old_obb,OLD),(a.rebuilt_obb,NEW),(a.pc_data/'sharedassets1.assets',STEAM_ASSET),(a.pc_data/'sharedassets1.resource',STEAM_RESOURCE)):
        if not path.is_file() or audio.sha(path)!=expected:p.error('incompatible input: '+str(path))
    old,old_res=packaged(a.old_obb);new,new_res=packaged(a.rebuilt_obb);pc=clips((a.pc_data/'sharedassets1.assets').read_bytes());pc_res=(a.pc_data/'sharedassets1.resource').read_bytes()
    if len(old)!=118 or set(old)!=set(new) or any(len(old[n])!=1 or len(new[n])!=1 or len(pc.get(n,[]))!=1 for n in old):raise ValueError('unexpected or ambiguous clip inventory')
    attrs=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex')
    rows=[]
    for i,name in enumerate(sorted(old),1):
        before,after,source=old[name][0],new[name][0],pc[name][0]
        try:
            if any(getattr(before,k,None)!=getattr(after,k,None) or getattr(before,k,None)!=getattr(source,k,None) for k in attrs):raise ValueError('clip metadata differs')
            previous,reconstructed,steam=payload(old_res,before),payload(new_res,after),payload(pc_res,source)
            if reconstructed!=steam:raise ValueError('rebuilt bytes differ from Steam clip')
            row={'name':name,'steam_payload_sha256':hashlib.sha256(steam).hexdigest(),'rebuilt_matches_steam':True}
            row.update(audio.compare(previous,reconstructed,before))
            if row['android']!=row['steam']:raise ValueError('decoded channel, rate or frame count differs')
        except Exception as e:row={'name':name,'error':str(e)}
        rows.append(row);print(i,name,row.get('error','decoded'),flush=True)
    report={'old_obb_sha256':OLD,'rebuilt_obb_sha256':NEW,'steam_asset_sha256':STEAM_ASSET,'steam_resource_sha256':STEAM_RESOURCE,
            'clips':rows,'limitations':'Steam slice equality and desktop FMOD decoding are not listening, Android playback or ownership tests; the sampled correlation does not cover every frame.'}
    with a.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    print('decoded',sum('error' not in x for x in rows),'of',len(rows),flush=True)
    if any('error' in x for x in rows):raise SystemExit(1)


if __name__=='__main__':main()
