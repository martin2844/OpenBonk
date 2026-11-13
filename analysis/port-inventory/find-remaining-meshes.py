#!/usr/bin/env python3
"""Find exact remaining vertex/index payload matches across PC Unity files.

Matching bytes and layouts do not establish ownership, intent or all mesh fields.
"""
import collections
import hashlib
import json
from pathlib import Path
import sys
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data

if len(sys.argv)!=4:raise SystemExit('usage: find-remaining-meshes.py PC_DATA_DIR ANDROID_ASSET MESH_MANIFEST')
pc,target,manifest=map(Path,sys.argv[1:])
used={r['target_id'] for r in json.loads(manifest.read_text())['meshes']}
def payload(m):
    v=m.m_VertexData.m_DataSize or b''
    if not v and m.m_StreamData and m.m_StreamData.path:
        v=get_resource_data(m.m_StreamData.path,m.object_reader.assets_file,m.m_StreamData.offset,m.m_StreamData.size)
    return v,bytes(m.m_IndexBuffer or [])
def signature(m,v,i):
    ch=tuple((x.stream,x.offset,x.format,x.dimension) for x in m.m_VertexData.m_Channels)
    return (m.m_VertexData.m_VertexCount,m.m_IndexFormat,ch,hashlib.sha256(v).hexdigest(),hashlib.sha256(i).hexdigest())
targets=collections.defaultdict(list);empty=[];errors=[]
for obj in UnityPy.load(str(target)).objects:
    if obj.type.name!='Mesh' or obj.path_id in used:continue
    try:
        m=obj.read();v,i=payload(m)
        if not v or not i:empty.append((obj.path_id,m.m_Name,len(v),len(i)));continue
        targets[signature(m,v,i)].append((obj.path_id,m.m_Name,len(v),len(i)))
    except Exception as e:errors.append(('target',obj.path_id,type(e).__name__))
matches=collections.defaultdict(list); scanned=0
paths=[pc/'resources.assets',*sorted(pc.glob('sharedassets*.assets')),*sorted(pc.glob('level[0-9]*'))]
for path in paths:
    if not path.is_file():continue
    try:
        for obj in UnityPy.load(str(path)).objects:
            if obj.type.name!='Mesh':continue
            scanned+=1
            try:
                m=obj.read();v,i=payload(m)
                if v and i:
                    s=signature(m,v,i)
                    if s in targets:matches[s].append((path.name,obj.path_id,m.m_Name))
            except Exception as e:errors.append((path.name,obj.path_id,type(e).__name__))
    except Exception as e:errors.append((path.name,None,type(e).__name__))
for sig,ts in targets.items():
    for t in ts:print('TARGET',json.dumps({'target':t,'matches':matches[sig]},ensure_ascii=False))
print('SUMMARY',json.dumps({'scanned_pc_meshes':scanned,'target_with_payload':sum(map(len,targets.values())),
                            'target_empty':len(empty),'target_exact_matches':sum(len(ts) for s,ts in targets.items() if matches[s]),
                            'errors':len(errors),'error_examples':errors[:10],'empty':empty},ensure_ascii=False))
print('SCOPE: unique payload signatures only, names ignored; other mesh fields and containers not compared.')
