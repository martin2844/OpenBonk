#!/usr/bin/env python3
"""Find identical transform values while excluding platform-specific pointers.

Byte equality is only reconstruction evidence, not object identity, rights,
or proof that other fields can safely be shared between platforms.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
import math
from pathlib import Path
import struct
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
SCENES=('level1','level2','level4')
OBB_SHA='ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2'


def sha(data): return hashlib.sha256(data).hexdigest()


def vectors(obj):
    raw=obj.get_raw_data();tree=obj.read_typetree();children=tree['m_Children']
    rect=obj.type.name=='RectTransform'
    expected=(108 if rect else 68)+12*len(children)
    if len(raw)!=expected:raise ValueError('unexpected transform size')
    values=[]
    for key, axes in (('m_LocalRotation','xyzw'),('m_LocalPosition','xyz'),('m_LocalScale','xyz')):
        values.extend(tree[key][axis] for axis in axes)
    if not all(math.isfinite(v) for v in values) or struct.pack('<10f',*values)!=raw[12:52]:
        raise ValueError('unexpected transform value layout')
    spans=[raw[12:52]]
    if rect:
        values=[]
        for key in ('m_AnchorMin','m_AnchorMax','m_AnchoredPosition','m_SizeDelta','m_Pivot'):
            values.extend(tree[key][axis] for axis in 'xy')
        if not all(math.isfinite(v) for v in values) or struct.pack('<10f',*values)!=raw[-40:]:
            raise ValueError('unexpected rect value layout')
        spans.append(raw[-40:])
    return tuple(spans)


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('steam_data',type=Path);p.add_argument('obb',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    with a.obb.open('rb') as f:
        if hashlib.file_digest(f,'sha256').hexdigest()!=OBB_SHA:raise ValueError('incompatible OBB')
    pins=json.loads((ROOT/'tools/steam-linux-diagnostic-inputs.json').read_text())
    results=[]
    with zipfile.ZipFile(a.obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        for scene in SCENES:
            pc=a.steam_data/scene
            with pc.open('rb') as f:steam=f.read()
            if len(steam)!=pins[scene]['size'] or sha(steam)!=pins[scene]['sha256']:raise ValueError('incompatible Steam scene: '+scene)
            index=defaultdict(list)
            for obj in UnityPy.load(steam).objects:
                if obj.type.name in ('Transform','RectTransform'):
                    index[(obj.type.name,vectors(obj))].append(obj.path_id)
            prefix='assets/bin/Data/'+scene+'.split'
            parts=sorted((n for n in z.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
            if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid scene fragments')
            scene_bytes=b''.join(z.read(n) for n in parts);stats=defaultdict(Counter);rows=[]
            for obj in UnityPy.load(scene_bytes).objects:
                kind=obj.type.name
                if kind not in ('Transform','RectTransform'):continue
                spans=vectors(obj);matches=index.get((kind,spans),[]);r=stats[kind];r['total']+=1
                if matches:
                    r['matched']+=1;r['unique']+=len(matches)==1;r['bytes']+=sum(map(len,spans));r['nonzero_bytes']+=sum(x!=0 for span in spans for x in span)
                    rows.append({'type':kind,'target_id':obj.path_id,'source_ids':matches,'value_sha256':sha(b''.join(spans))})
            results.append({'scene':scene,'steam_sha256':sha(steam),'android_sha256':sha(scene_bytes),'stats':{k:dict(v) for k,v in stats.items()},'matches':rows})
    report={'obb_sha256':OBB_SHA,'scenes':results,'limitations':'Exact value bytes only. References, remaining data, object identity and rights are not established.'}
    with a.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    for result in results:print(result['scene'],result['stats'])


if __name__=='__main__':main()
