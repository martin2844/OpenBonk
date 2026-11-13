#!/usr/bin/env python3
"""Compare sampled streamed animation polynomials in pinned PC/Android assets.

This estimates differences in serialized curve values. It does not evaluate
Unity's animation pipeline, retargeting, blending, or final rendered poses.
"""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path
import struct

import UnityPy

PC_SHA='e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da'
ANDROID_SHA='dc0fa2e17c7607009ad45489f5995aac47cc81a021ae0ce46a0ecd4981bf3206'
PAIRS=((455,247,'MonkeBreakdance'),(456,249,'MovingBushQuest'))


def sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def floating(word):
    return struct.unpack('<f',struct.pack('<I',word))[0]


def frames(words):
    result=[];offset=0
    while offset<len(words):
        if offset+2>len(words):raise ValueError('truncated frame')
        time=floating(words[offset]);count=words[offset+1];offset+=2
        if count>100000 or offset+count*5>len(words):raise ValueError('invalid key count')
        keys=[]
        for _ in range(count):
            curve=words[offset];coeff=tuple(floating(x) for x in words[offset+1:offset+5]);offset+=5
            if not all(math.isfinite(v) for v in coeff):raise ValueError('non-finite coefficient')
            keys.append((curve,coeff))
        if len({k for k,_ in keys})!=len(keys):raise ValueError('duplicate curve index')
        result.append((time,keys))
    return result


def evaluate(c,dt):
    return ((c[0]*dt+c[1])*dt+c[2])*dt+c[3]


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('android_asset',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    source=a.pc_data/'sharedassets2.assets'
    if sha(source)!=PC_SHA or sha(a.android_asset)!=ANDROID_SHA:raise ValueError('unexpected input hash')
    android={o.path_id:o for o in UnityPy.load(str(a.android_asset)).objects};pc={o.path_id:o for o in UnityPy.load(str(source)).objects};results=[]
    for tid,sid,name in PAIRS:
        if android[tid].type.name!='AnimationClip' or pc[sid].type.name!='AnimationClip':raise ValueError('unexpected object type')
        x,y=android[tid].read_typetree(),pc[sid].read_typetree()
        if x['m_Name']!=name or y['m_Name']!=name:raise ValueError('unexpected clip name')
        x_data=x['m_MuscleClip']['m_Clip']['data']['m_StreamedClip'];y_data=y['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']
        x_words,y_words=x_data['data'],y_data['data']
        # Compare the whole decoded type tree after excluding only stream words.
        x_meta,y_meta=copy.deepcopy(x),copy.deepcopy(y)
        x_meta['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
        y_meta['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
        if x_meta!=y_meta or len(x_words)!=len(y_words):raise ValueError('clip metadata or stream length differs')
        xf,yf=frames(x_words),frames(y_words)
        if len(xf)!=len(yf):raise ValueError('frame count differs')
        changed_words=sum(a!=b for a,b in zip(x_words,y_words));worst=0.0;worst_at=None;samples=0;max_coef=0.0;intervals=0
        for i,((xt,xkeys),(yt,ykeys)) in enumerate(zip(xf,yf)):
            if xt!=yt or len(xkeys)!=len(ykeys) or [k for k,_ in xkeys]!=[k for k,_ in ykeys]:raise ValueError('frame timing or curve mapping differs')
            if i+1>=len(xf):continue
            dt=xf[i+1][0]-xt
            # Unity uses sentinel frames far outside the clip; ignore their intervals.
            if xt<0 or not math.isfinite(dt) or dt<=0 or dt>60:continue
            intervals+=1
            for (curve,xc),(_,yc) in zip(xkeys,ykeys):
                max_coef=max(max_coef,*(abs(a-b) for a,b in zip(xc,yc)))
                for fraction in (0.0,0.25,0.5,0.75,1.0):
                    diff=abs(evaluate(xc,dt*fraction)-evaluate(yc,dt*fraction));samples+=1
                    if diff>worst:worst=diff;worst_at={'frame':i,'time':xt,'curve':curve,'fraction':fraction}
        results.append({'name':name,'android_id':tid,'steam_id':sid,'frames':len(xf),'stream_words':len(x_words),'different_words':changed_words,
                        'sampled_intervals':intervals,'samples':samples,'max_coefficient_difference':max_coef,'max_sampled_curve_difference':worst,'worst_sample':worst_at})
    report={'pc_sha256':PC_SHA,'android_sha256':ANDROID_SHA,'comparison':results,
            'limitations':['Only five sample points per serialized curve interval.','Does not evaluate Unity runtime, skeleton poses, blending, or rendering.','Name and numerical similarity do not establish ownership or rights.']}
    a.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(results),flush=True)


if __name__=='__main__':main()
