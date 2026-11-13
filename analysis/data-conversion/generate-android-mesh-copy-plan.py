#!/usr/bin/env python3
"""Generate offset-only plans for 50 pinned Steam mesh assets."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'build/android/localization-shared-scrubbed-base/base.obb'
TARGET=ROOT/'build/android/launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
MANIFEST=ROOT/'build/android/local-research-manifests/hashed-mesh-template/manifest.json'
LINUX=ROOT/'build/android/linux-steam-regression.zip';WINDOWS=ROOT/'build/android/windows-steam-full-input.zip'
OUTPUT=ROOT/'build/android/mesh-android-copy-plan/mesh-plan.bin'
BASE_SHA='ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
TARGET_SHA='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691'
MANIFEST_SHA='44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24'
SOURCE_HASHES=('d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea')
def sha(b):return hashlib.sha256(b).hexdigest()
def file_sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--base',type=Path,default=BASE);p.add_argument('--target',type=Path,default=TARGET);p.add_argument('--manifest',type=Path,default=MANIFEST)
    p.add_argument('--linux-zip',type=Path,default=LINUX);p.add_argument('--windows-zip',type=Path,default=WINDOWS);p.add_argument('--output',type=Path,default=OUTPUT)
    a=p.parse_args();out=a.output.absolute()
    if out.exists() or out.is_symlink():raise ValueError('output exists')
    for path,expected in ((a.base,BASE_SHA),(a.target,TARGET_SHA),(a.manifest,MANIFEST_SHA)):
        if file_sha(path)!=expected:raise ValueError('input pin mismatch: '+str(path))
    records=json.loads(a.manifest.read_text())['meshes']
    if len(records)!=50 or len({x['file'] for x in records})!=50:raise ValueError('mesh inventory mismatch')
    plan=bytearray(b'OBSME001'+struct.pack('>I',len(records)))
    for h in SOURCE_HASHES:plan.extend(bytes.fromhex(h))
    with zipfile.ZipFile(a.base) as base,zipfile.ZipFile(a.target) as target,zipfile.ZipFile(a.linux_zip) as linux,zipfile.ZipFile(a.windows_zip) as windows:
        assets=[];streams=[];objects=[]
        for z,pin in zip((linux,windows),SOURCE_HASHES[:2]):
            asset=z.read('Megabonk_Data/sharedassets1.assets');stream=z.read('Megabonk_Data/sharedassets1.assets.resS')
            if sha(asset)!=pin or sha(stream)!=SOURCE_HASHES[2]:raise ValueError('Steam source mismatch')
            assets.append(asset);streams.append(stream);objects.append({o.path_id:o for o in UnityPy.load(asset).objects})
        for rec in records:
            name=rec['file'];entry='assets/bin/Data/'+name
            if len(name)!=32 or any(c not in '0123456789abcdef' for c in name):raise ValueError('invalid mesh name')
            template=base.read(entry);expected=target.read(entry)
            if sha(template)!=rec['template_sha256'] or sha(expected)!=rec['original_sha256']:raise ValueError('target mismatch')
            source_plans=[]
            for asset,stream,index in zip(assets,streams,objects):
                obj=index[rec['source_id']]
                if obj.type.name!='Mesh':raise ValueError('mesh source type mismatch')
                mesh=obj.read();raw=obj.get_raw_data();v=bytes(mesh.m_VertexData.m_DataSize or b'');i=bytes(mesh.m_IndexBuffer or [])
                if not v and mesh.m_StreamData and mesh.m_StreamData.path:
                    r=mesh.m_StreamData
                    if r.path!='sharedassets1.assets.resS' or r.offset<0 or r.size<=0 or r.offset+r.size>len(stream):raise ValueError('bad mesh stream')
                    v=stream[r.offset:r.offset+r.size];vfile=1;voff=r.offset
                else:
                    vo=raw.find(v)
                    if not v or vo<0:raise ValueError('inline vertex data missing')
                    vfile=0;voff=obj.byte_start+vo
                io=raw.find(i)
                if not i or io<0:raise ValueError('index data missing')
                ioff=obj.byte_start+io
                if (len(v),sha(v),len(i),sha(i))!=(rec['vertex_size'],rec['vertex_sha256'],rec['index_size'],rec['index_sha256']):raise ValueError('mesh data differs')
                if asset[ioff:ioff+len(i)]!=i or (asset if vfile==0 else stream)[voff:voff+len(v)]!=v:raise ValueError('mesh source range differs')
                source_plans.append(((vfile,voff),(0,ioff),v,i))
            if source_plans[0][2:]!=source_plans[1][2:]:raise ValueError('platform mesh bytes differ')
            rebuilt=bytearray(template)
            for offset,value in ((rec['vertex_offset'],source_plans[0][2]),(rec['index_offset'],source_plans[0][3])):
                if offset<0 or offset+len(value)>len(rebuilt) or any(rebuilt[offset:offset+len(value)]):raise ValueError('mesh target range not blank')
                rebuilt[offset:offset+len(value)]=value
            if bytes(rebuilt)!=expected:raise ValueError('mesh reconstruction differs')
            plan.extend(name.encode('ascii'));plan.extend(bytes.fromhex(rec['template_sha256']));plan.extend(bytes.fromhex(rec['original_sha256']))
            for pos,key,hashkey,idx in ((rec['vertex_offset'],'vertex_size','vertex_sha256',0),(rec['index_offset'],'index_size','index_sha256',1)):
                (lf,lo),(wf,wo)=source_plans[0][idx],source_plans[1][idx]
                plan.extend(struct.pack('>IIIQIQ',pos,rec[key],lf,lo,wf,wo));plan.extend(bytes.fromhex(rec[hashkey]))
    out.parent.mkdir(parents=True,exist_ok=True)
    with out.open('xb') as f:f.write(plan)
    print('50 meshes;',sum(r['vertex_size']+r['index_size'] for r in records),'buffer bytes; plan',len(plan),'sha256',sha(plan),'offsets and hashes only')
if __name__=='__main__':main()
