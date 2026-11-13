#!/usr/bin/env python3
"""Locally remove/restore identical PC mesh vertex and index payloads.

Pinned research prototype. Other mesh fields and game content remain in the
template; it is not a release package or a rights determination.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import shutil
import UnityPy
from UnityPy.helpers.ResourceReader import get_resource_data

PC_ASSETS='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
PC_STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TEXTURE_TEMPLATE='6a67b08a6e37daa4689c2e032a0bc025b307797be2b4524f4f98399cd70c9bdd'
def sha_file(path):
    h=hashlib.sha256()
    with path.open('rb') as source:
        for block in iter(lambda:source.read(1024*1024),b''):h.update(block)
    return h.hexdigest()
def require(path, expected):
    if not path.is_file() or sha_file(path)!=expected: raise ValueError(f'unsupported input: {path}')
def payload(mesh):
    v=mesh.m_VertexData.m_DataSize or b''
    if not v and mesh.m_StreamData and mesh.m_StreamData.path:
        if mesh.m_StreamData.path!='sharedassets1.assets.resS':raise ValueError('unexpected mesh stream')
        v=get_resource_data(mesh.m_StreamData.path,mesh.object_reader.assets_file,mesh.m_StreamData.offset,mesh.m_StreamData.size)
    return v,bytes(mesh.m_IndexBuffer or [])
def geometry(mesh):
    channels=tuple((x.stream,x.offset,x.format,x.dimension) for x in mesh.m_VertexData.m_Channels)
    return mesh.m_Name,mesh.m_VertexData.m_VertexCount,channels,mesh.m_IndexFormat
def digest(data):return hashlib.sha256(data).hexdigest()
def key(mesh,v,i):return geometry(mesh),digest(v),digest(i)

parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('action',choices=('template','restore'))
parser.add_argument('pc_data',type=Path);parser.add_argument('input_dir',type=Path);parser.add_argument('output_dir',type=Path);a=parser.parse_args()
if a.output_dir.exists():raise SystemExit('output already exists')
require(a.pc_data/'sharedassets1.assets',PC_ASSETS);require(a.pc_data/'sharedassets1.assets.resS',PC_STREAM)
asset=a.input_dir/'sharedassets1.assets'; combined=a.input_dir/'combined.json'
if not combined.is_file():raise ValueError('missing combined texture metadata')
if a.action=='template':require(asset,TEXTURE_TEMPLATE)
else:
    manifest=json.loads((a.input_dir/'mesh-manifest.json').read_text())
    if manifest.get('version')!=1 or manifest.get('reconstructed_sha256')!=TEXTURE_TEMPLATE or len(manifest.get('meshes',[]))!=410:raise ValueError('invalid mesh manifest')
    require(asset,manifest['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets'));source_by_id={o.path_id:o for o in pc.objects}; index=collections.defaultdict(list)
for obj in pc.objects:
    if obj.type.name!='Mesh':continue
    mesh=obj.read();v,i=payload(mesh)
    if v and i:index[key(mesh,v,i)].append(obj.path_id)
env=UnityPy.load(str(asset));targets={o.path_id:o for o in env.objects};records=[]
if a.action=='template':
    for obj in env.objects:
        if obj.type.name!='Mesh':continue
        mesh=obj.read();v,i=payload(mesh)
        if not v or not i or not mesh.m_VertexData.m_DataSize:continue
        matches=index[key(mesh,v,i)]
        if len(matches)!=1:continue
        records.append({'target_id':obj.path_id,'source_id':matches[0],'name':mesh.m_Name,'vertices_size':len(v),
                        'indices_size':len(i),'vertices_sha256':digest(v),'indices_sha256':digest(i)})
        mesh.m_VertexData.m_DataSize=bytes(len(v));mesh.m_IndexBuffer=[0]*len(i);mesh.save()
    if len(records)!=410:raise ValueError(f'unexpected mesh count: {len(records)}')
else:
    seen=set()
    for rec in manifest['meshes']:
        tid,sid=rec['target_id'],rec['source_id']
        if tid in seen or tid not in targets or sid not in source_by_id or targets[tid].type.name!='Mesh' or source_by_id[sid].type.name!='Mesh':raise ValueError('invalid mesh identity')
        seen.add(tid);dst=targets[tid].read();src=source_by_id[sid].read();v,i=payload(src)
        if geometry(src)!=geometry(dst) or dst.m_Name!=rec['name'] or len(v)!=rec['vertices_size'] or len(i)!=rec['indices_size'] or digest(v)!=rec['vertices_sha256'] or digest(i)!=rec['indices_sha256']:
            raise ValueError('mesh source mismatch')
        if dst.m_VertexData.m_DataSize!=bytes(len(v)) or bytes(dst.m_IndexBuffer)!=bytes(len(i)):
            raise ValueError('mesh payload was not cleared')
        dst.m_VertexData.m_DataSize=v;dst.m_IndexBuffer=list(i);dst.save()
    records=manifest['meshes']
a.output_dir.mkdir(parents=True)
try:
    out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save());h=sha_file(out)
    if a.action=='restore' and h!=TEXTURE_TEMPLATE:raise ValueError('restored texture template hash mismatch')
    shutil.copyfile(combined,a.output_dir/'combined.json')
    if a.action=='template':
        manifest={'version':1,'reconstructed_sha256':TEXTURE_TEMPLATE,'template_sha256':h,'meshes':records}
        (a.output_dir/'mesh-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(a.action,len(records),'meshes,',sum(x['vertices_size']+x['indices_size'] for x in records),'payload bytes; SHA256',h)
    print('Other game content remains; local research only, not a release artifact.')
except Exception:
    for p in a.output_dir.iterdir():p.unlink()
    a.output_dir.rmdir();raise
