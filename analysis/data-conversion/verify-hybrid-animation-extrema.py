#!/usr/bin/env python3
"""Find extrema of cubic coefficient differences for selected hybrid clips.

This evaluates serialized curve polynomials in double precision, not Unity
pose evaluation or rendering. It is local diagnostic evidence only.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import math
from pathlib import Path
import zipfile

import UnityPy

HERE=Path(__file__).resolve().parent
spec=importlib.util.spec_from_file_location('curve',HERE/'analyze-shared2-animation-streams.py');curve=importlib.util.module_from_spec(spec);spec.loader.exec_module(curve)
spec=importlib.util.spec_from_file_location('inventory',HERE/'inventory-hashed-animation-differences.py');inventory=importlib.util.module_from_spec(spec);spec.loader.exec_module(inventory)
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
OBB='ca3fa6b0e1d994339655331b76c23c2d4f94c2038bd6f87c825575ab28cdb919'
MANIFEST='716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1'


def sha(p):
    with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def extrema(x,y,dt):
    a,b,c,d=(u-v for u,v in zip(x,y));points=[0.,dt];A,B,C=3*a,2*b,c
    if A==0.:
        if B!=0.:points.append(-C/B)
    else:
        disc=B*B-4*A*C
        if disc>=0.:
            root=math.sqrt(disc)
            # This form avoids cancellation for one root; their product gives the other.
            q=-0.5*(B+math.copysign(root,B))
            if q!=0.:points.extend((q/A,C/q))
            else:points.append(-B/(2*A))
    valid=[t for t in points if math.isfinite(t) and 0<=t<=dt]
    return max((abs(((a*t+b)*t+c)*t+d),t) for t in valid)


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('obb',type=Path);p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);p.add_argument('--all-matching',action='store_true',help='also investigate clips with matching non-stream metadata; does not authorize their use');a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    pc=a.pc_data/'sharedassets1.assets'
    if sha(pc)!=PC or sha(a.obb)!=OBB or sha(a.manifest)!=MANIFEST:raise ValueError('unexpected input hash')
    records=json.loads(a.manifest.read_text())['animations']
    if len(records)!=108 or len({r['file'] for r in records})!=108:raise ValueError('unexpected inventory')
    if not a.all_matching: records=[r for r in records if r['mode']=='steam-stream']
    if not a.all_matching and len(records)!=25:raise ValueError('unexpected selection')
    sources={o.path_id:o for o in UnityPy.load(str(pc)).objects};results=[];excluded=[]
    with zipfile.ZipFile(a.obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate OBB entries')
        for rec in records:
            raw=z.read('assets/bin/Data/'+rec['file'])
            if hashlib.sha256(raw).hexdigest()!=rec['original_sha256']:raise ValueError('unexpected Android animation')
            target=UnityPy.load(raw).objects[0].read_typetree();source=sources[rec['source_id']].read_typetree()
            if target['m_Name']!=rec['name'] or source['m_Name']!=rec['name']:raise ValueError('clip name mismatch')
            tx,sx=copy.deepcopy(target),copy.deepcopy(source)
            tx['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[];sx['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']=[]
            differences=inventory.differing_paths(tx,sx)
            if differences:
                if not a.all_matching:raise ValueError('selected clip metadata differs')
                excluded.append({'file':rec['file'],'name':rec['name'],'reason':'non-stream metadata differs','paths':dict(differences)})
                continue
            get=lambda x:x['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']
            x,y=curve.frames(get(target)),curve.frames(get(source))
            if len(x)!=len(y):raise ValueError('frame count mismatch')
            worst=0.;worst_at=None;segments=0
            for i,((xt,xkeys),(yt,ykeys)) in enumerate(zip(x,y)):
                if xt!=yt or [k for k,_ in xkeys]!=[k for k,_ in ykeys]:raise ValueError('frame or mapping mismatch')
                if i+1==len(x):continue
                dt=x[i+1][0]-xt
                if xt<0 or not math.isfinite(dt) or dt<=0 or dt>60:continue
                for (key,xc),(_,yc) in zip(xkeys,ykeys):
                    value,at=extrema(xc,yc,dt);segments+=1
                    if value>worst:worst=value;worst_at={'frame':i,'time':xt,'curve':key,'segment_time':at}
            if not segments:raise ValueError('no comparable curve segments')
            results.append({'file':rec['file'],'name':rec['name'],'mode':rec['mode'],'patch_bytes':rec['patch_count'],'segments':segments,'max_curve_difference':worst,'worst':worst_at})
    if a.all_matching and (len(results)!=107 or len(excluded)!=1):raise ValueError('unexpected matching inventory')
    report={'clips':results,'max_curve_difference':max(x['max_curve_difference'] for x in results),
            'excluded':excluded,
            'limitations':['Double-precision polynomial extrema do not model Unity float arithmetic, skeleton posing, blending or rendering.','Intervals outside 0 < dt <= 60 seconds and sentinel frames are excluded.']}
    a.output.write_text(json.dumps(report,indent=2)+'\n');print('verified',len(results),'clips, maximum curve difference',report['max_curve_difference'],flush=True)


if __name__=='__main__':main()
