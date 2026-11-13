#!/usr/bin/env python3
"""Inventory Android/Steam differences in 108 pinned animation clips.

This is research evidence, not an animation equivalence or licensing test.
"""
import argparse
import collections
import copy
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import re
import struct
import zipfile

import UnityPy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('stream_compare',HERE/'analyze-shared2-animation-streams.py')
stream_compare=importlib.util.module_from_spec(spec);spec.loader.exec_module(stream_compare)
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
OBB='ca3fa6b0e1d994339655331b76c23c2d4f94c2038bd6f87c825575ab28cdb919'
MANIFEST='d906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8'


def sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def differing_paths(a,b,path='',result=None):
    if result is None:result=collections.Counter()
    if type(a)!=type(b):result[path+' [type]']+=1
    elif isinstance(a,dict):
        for key in a.keys()|b.keys():
            if key not in a or key not in b:result[path+'/'+str(key)+' [missing]']+=1
            else:differing_paths(a[key],b[key],path+'/'+str(key),result)
    elif isinstance(a,(list,tuple)):
        if len(a)!=len(b):result[path+' [length]']+=1
        for x,y in zip(a,b):differing_paths(x,y,path+'[]',result)
    elif a!=b and not (isinstance(a,float) and math.isnan(a) and math.isnan(b)):result[path]+=1
    return result


def sample(xwords,ywords):
    xf,yf=stream_compare.frames(xwords),stream_compare.frames(ywords)
    if len(xf)!=len(yf):raise ValueError('frame count differs')
    worst=0.;where=None;count=0;intervals=0
    for i,((xt,xkeys),(yt,ykeys)) in enumerate(zip(xf,yf)):
        if xt!=yt or len(xkeys)!=len(ykeys) or [k for k,_ in xkeys]!=[k for k,_ in ykeys]:raise ValueError('frame or curve mapping differs')
        if i+1>=len(xf):continue
        dt=xf[i+1][0]-xt
        if xt<0 or not math.isfinite(dt) or dt<=0 or dt>60:continue
        intervals+=1
        for (curve,xc),(_,yc) in zip(xkeys,ykeys):
            for fraction in (0.,0.25,0.5,0.75,1.):
                diff=abs(stream_compare.evaluate(xc,dt*fraction)-stream_compare.evaluate(yc,dt*fraction));count+=1
                if diff>worst:worst=diff;where={'frame':i,'time':xt,'curve':curve,'fraction':fraction}
    return {'frames':len(xf),'intervals':intervals,'samples':count,'max_sampled_curve_difference':worst,'worst_sample':where}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('android_obb',type=Path);p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    pc=a.pc_data/'sharedassets1.assets'
    if sha(pc)!=PC or sha(a.android_obb)!=OBB or sha(a.manifest)!=MANIFEST:raise ValueError('unexpected input hash')
    manifest=json.loads(a.manifest.read_text());records=manifest['animations']
    if len(records)!=108 or len({x['file'] for x in records})!=108:raise ValueError('unexpected animation inventory')
    source={o.path_id:o for o in UnityPy.load(str(pc)).objects};results=[]
    with zipfile.ZipFile(a.android_obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        for rec in records:
            filename=rec['file']
            if not re.fullmatch('[0-9a-f]{32}',filename):raise ValueError('invalid asset name')
            asset=z.read('assets/bin/Data/'+filename)
            if hashlib.sha256(asset).hexdigest()!=rec['original_sha256']:raise ValueError('Android asset mismatch: '+filename)
            objs=list(UnityPy.load(asset).objects);src=source[rec['source_id']]
            if len(objs)!=1 or objs[0].type.name!='AnimationClip' or src.type.name!='AnimationClip':raise ValueError('unexpected clip')
            x,y=objs[0].read_typetree(),src.read_typetree()
            if x['m_Name']!=rec['name'] or y['m_Name']!=rec['name']:raise ValueError('clip name mismatch')
            xdata=x['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data'];ydata=y['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']
            xm,ym=copy.deepcopy(x),copy.deepcopy(y)
            xm['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[];ym['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
            paths=differing_paths(xm,ym)
            row={'file':filename,'name':rec['name'],'source_id':rec['source_id'],'patch_bytes':rec['patch_count'],
                 'non_stream_differences':dict(paths),'stream_word_counts':[len(xdata),len(ydata)],
                 'different_stream_words':sum(a!=b for a,b in zip(xdata,ydata)) if len(xdata)==len(ydata) else None}
            try:row['sampled_curves']=sample(xdata,ydata)
            except (ValueError,OverflowError,struct.error) as exc:row['sample_error']=str(exc)
            results.append(row)
    summary={'clips':len(results),'non_stream_equal':sum(not r['non_stream_differences'] for r in results),
             'stream_structure_sampled':sum('sampled_curves' in r for r in results),'patch_bytes':sum(r['patch_bytes'] for r in results)}
    report={'summary':summary,'clips':results,'limitations':['Sampling five points per serialized segment does not prove Unity pose equivalence.','Metadata equality and name similarity do not establish ownership or redistribution rights.']}
    a.output.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(summary),flush=True)


if __name__=='__main__':
    main()
