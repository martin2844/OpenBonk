#!/usr/bin/env python3
"""Rebuild two pinned Android meshes and their smoothed outline normals.

Experimental local research. Templates/output retain game data and are not a
release artifact. Floating-point normals can differ slightly from Unity's.
"""
import argparse
import collections
import hashlib
import json
import math
from pathlib import Path
import shutil
import struct
import UnityPy

PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
TARGET='dd2e28ef52867cfd1b8ef894332d22c70e7f1f06bfa9337ab2483bc50ffa533e'
MESHES=((1559,1482,'Cube',2600),(1641,1827,'lp.obj',493))
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest()
def f32(x):return struct.unpack('<f',struct.pack('<f',x))[0]
def channels(mesh):return [(i,c.stream,c.offset,c.format,c.dimension) for i,c in enumerate(mesh.m_VertexData.m_Channels) if c.dimension&15]
def generate(raw,count):
    groups=collections.defaultdict(list)
    for i in range(count):groups[struct.unpack_from('<3f',raw,32*i)].append(i)
    smooth=[None]*count
    for indices in groups.values():
        result=struct.unpack_from('<3f',raw,32*indices[0]+12)
        if len(indices)>1:
            sums=[0.,0.,0.]
            for i in indices:
                n=struct.unpack_from('<3f',raw,32*i+12)
                for j in range(3):sums[j]=f32(sums[j]+n[j])
            length=f32(math.sqrt(f32(f32(f32(sums[0]*sums[0])+f32(sums[1]*sums[1]))+f32(sums[2]*sums[2]))))
            result=tuple(f32(x/length) for x in sums) if length>1e-5 else (0.,0.,0.)
        for i in indices:smooth[i]=result
    out=bytearray(count*44)
    for i in range(count):
        out[i*44:i*44+32]=raw[i*32:i*32+32];struct.pack_into('<3f',out,i*44+32,*smooth[i])
    return bytes(out)

p=argparse.ArgumentParser(description=__doc__);p.add_argument('action',choices=('template','restore'))
p.add_argument('pc_data',type=Path);p.add_argument('input_dir',type=Path);p.add_argument('output_dir',type=Path);a=p.parse_args()
if a.output_dir.exists():raise SystemExit('output already exists')
pc_path=a.pc_data/'sharedassets1.assets';target_path=a.input_dir/'sharedassets1.assets'
if not pc_path.is_file() or sha(pc_path)!=PC:raise ValueError('unsupported PC assets')
if a.action=='template':expected=TARGET
else:
    manifest=json.loads((a.input_dir/'outline.json').read_text())
    if manifest.get('version')!=1 or manifest.get('target_sha256')!=TARGET:raise ValueError('invalid template manifest')
    expected=manifest['template_sha256']
if not target_path.is_file() or sha(target_path)!=expected:raise ValueError('unsupported target asset')
pc=UnityPy.load(str(pc_path));env=UnityPy.load(str(target_path));sources={o.path_id:o for o in pc.objects};targets={o.path_id:o for o in env.objects}
for tid,sid,name,count in MESHES:
    if tid not in targets or sid not in sources or targets[tid].type.name!='Mesh' or sources[sid].type.name!='Mesh':raise ValueError('mesh missing')
    src=sources[sid].read();dst=targets[tid].read();raw=src.m_VertexData.m_DataSize;idx=bytes(src.m_IndexBuffer)
    if src.m_Name!=name or dst.m_Name!=name or src.m_VertexData.m_VertexCount!=count or dst.m_VertexData.m_VertexCount!=count or len(raw)!=count*32:
        raise ValueError('mesh identity mismatch')
    if channels(src)!=[(0,0,0,0,3),(1,0,12,0,3),(4,0,24,0,2)] or channels(dst)!=[(0,0,0,0,3),(1,0,12,0,3),(4,0,24,0,2),(7,0,32,0,3)]:
        raise ValueError('unexpected vertex layout')
    if len(dst.m_VertexData.m_DataSize)!=count*44 or len(dst.m_IndexBuffer)!=len(idx):raise ValueError('unexpected payload size')
    if a.action=='template':
        old=dst.m_VertexData.m_DataSize
        if bytes(dst.m_IndexBuffer)!=idx or any(old[i*44:i*44+32]!=raw[i*32:i*32+32] for i in range(count)):
            raise ValueError('source vertex/index data mismatch')
        dst.m_VertexData.m_DataSize=bytes(count*44);dst.m_IndexBuffer=[0]*len(idx)
    else:
        if dst.m_VertexData.m_DataSize!=bytes(count*44) or bytes(dst.m_IndexBuffer)!=bytes(len(idx)):
            raise ValueError('template was not cleared')
        dst.m_VertexData.m_DataSize=generate(raw,count);dst.m_IndexBuffer=list(idx)
    dst.save()
a.output_dir.mkdir(parents=True)
try:
    out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save());h=sha(out)
    for name in ('combined.json','mesh-manifest.json'):
        shutil.copyfile(a.input_dir/name,a.output_dir/name)
    if a.action=='template':
        (a.output_dir/'outline.json').write_text(json.dumps({'version':1,'target_sha256':TARGET,'template_sha256':h},indent=2)+'\n')
    print(a.action,'2 meshes; SHA256',h)
    print('Research output still contains game data; not for distribution.')
except Exception:
    for path in a.output_dir.iterdir():path.unlink()
    a.output_dir.rmdir();raise
