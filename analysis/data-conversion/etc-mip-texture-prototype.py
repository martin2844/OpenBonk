#!/usr/bin/env python3
"""Experimental local PC RGBA mip to Android ETC texture reconstruction.

The template still contains game content; neither template nor output is a
release artifact. Encoded ETC bytes can vary across encoder versions.
"""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import UnityPy
from UnityPy.export import Texture2DConverter

PC_ASSETS='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
PC_STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'
TARGET='5d07d1f4dd17770462be0533825f9499c63f21188b8fd2da7b0fc554d05b0ef3'
DIRECT='3bcf4a4ff496fcdac1520384bd30c49d671e9944b4ec5e7e23cc9e23ae5fe100'
RGBA='abbad059169083017f36b6c560faed2ad88c92a17d6b4bc8f76d9816d5327750'
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''): h.update(b)
    return h.hexdigest()
def require(path,expected):
    if not path.is_file() or sha(path)!=expected: raise ValueError(f'unsupported file: {path}')
def identity(src,dst):
    return (src.m_Name,src.m_Width,src.m_Height,src.m_MipCount,src.m_ColorSpace,src.m_ImageCount)==(dst.m_Name,dst.m_Width,dst.m_Height,dst.m_MipCount,dst.m_ColorSpace,dst.m_ImageCount)
def candidate(src,dst):
    return (identity(src,dst) and src.m_TextureFormat==4 and dst.m_TextureFormat in (45,47) and dst.m_MipCount>=2
            and 0<dst.m_Width<=256 and 0<dst.m_Height<=256 and dst.m_ImageCount==1 and 'normal' not in dst.m_Name.lower())

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('action',choices=('template','restore'));parser.add_argument('pc_data',type=Path)
parser.add_argument('input_dir',type=Path);parser.add_argument('direct_manifest',type=Path)
parser.add_argument('rgba_manifest',type=Path);parser.add_argument('output_dir',type=Path);a=parser.parse_args()
if a.output_dir.exists(): raise SystemExit('output directory already exists')
for p,h in ((a.pc_data/'sharedassets1.assets',PC_ASSETS),(a.pc_data/'sharedassets1.assets.resS',PC_STREAM),
            (a.direct_manifest,DIRECT),(a.rgba_manifest,RGBA)): require(p,h)
excluded=set()
for p in (a.direct_manifest,a.rgba_manifest):
    excluded.update(r['target_id'] for r in json.loads(p.read_text())['textures'])
asset=a.input_dir/'sharedassets1.assets'
if a.action=='template':require(asset,TARGET)
else:
    manifest=json.loads((a.input_dir/'manifest.json').read_text())
    if manifest.get('version')!=1 or manifest.get('source_target_sha256')!=TARGET or len(manifest.get('textures',[]))!=138: raise ValueError('invalid manifest')
    require(asset,manifest['template_sha256'])
pc=UnityPy.load(str(a.pc_data/'sharedassets1.assets')); sources=collections.defaultdict(list)
for obj in pc.objects:
    if obj.type.name=='Texture2D':
        t=obj.read();sources[(t.m_Name,t.m_Width,t.m_Height)].append((obj.path_id,t))
env=UnityPy.load(str(asset));objects={o.path_id:o for o in env.objects};records=[]
if a.action=='template':
    for obj in env.objects:
        if obj.type.name!='Texture2D' or obj.path_id in excluded:continue
        dst=obj.read();matches=sources[(dst.m_Name,dst.m_Width,dst.m_Height)]
        if len(matches)!=1:continue
        sid,src=matches[0]
        if not candidate(src,dst):continue
        raw=src.get_image_data();old=dst.image_data
        expected=sum(max(1,dst.m_Width>>i)*max(1,dst.m_Height>>i)*4 for i in range(dst.m_MipCount))
        if len(raw)!=expected or not old:raise ValueError(f'unexpected payload: {dst.m_Name}')
        records.append({'target_id':obj.path_id,'source_id':sid,'name':dst.m_Name,'size':len(old),
                        'pc_raw_sha256':hashlib.sha256(raw).hexdigest()})
        dst.image_data=bytes(len(old));dst.save()
    if len(records)!=138:raise ValueError(f'unexpected candidate count: {len(records)}')
else:
    seen=set()
    for rec in manifest['textures']:
        tid=rec['target_id']
        if tid in seen or tid in excluded or tid not in objects or objects[tid].type.name!='Texture2D':raise ValueError('bad target id')
        seen.add(tid);dst=objects[tid].read();matches=sources[(dst.m_Name,dst.m_Width,dst.m_Height)]
        if len(matches)!=1 or matches[0][0]!=rec['source_id'] or dst.m_Name!=rec['name']:raise ValueError('source identity mismatch')
        src=matches[0][1];raw=src.get_image_data()
        if not candidate(src,dst) or hashlib.sha256(raw).hexdigest()!=rec['pc_raw_sha256'] or dst.image_data!=bytes(rec['size']):raise ValueError('texture data mismatch')
        encoded=[];offset=0
        for level in range(dst.m_MipCount):
            width=max(1,dst.m_Width>>level);height=max(1,dst.m_Height>>level);size=width*height*4
            image=Texture2DConverter.parse_image_data(raw[offset:offset+size],width,height,4,src.object_reader.version,src.object_reader.platform)
            data,fmt=Texture2DConverter.image_to_texture2d(image,dst.m_TextureFormat,dst.object_reader.platform,dst.m_PlatformBlob)
            if int(fmt)!=dst.m_TextureFormat:raise ValueError('unexpected ETC output format')
            encoded.append(data);offset+=size
        payload=b''.join(encoded)
        if offset!=len(raw) or len(payload)!=rec['size']:raise ValueError(f'encoded mip size mismatch: {dst.m_Name}')
        dst.image_data=payload;dst.m_CompleteImageSize=len(payload);dst.save()
    records=manifest['textures']
a.output_dir.mkdir(parents=True)
try:
    out=a.output_dir/'sharedassets1.assets';out.write_bytes(env.file.save());h=sha(out)
    if a.action=='template':
        manifest={'version':1,'source_target_sha256':TARGET,'template_sha256':h,'textures':records}
        (a.output_dir/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(a.action,len(records),'textures; asset SHA256',h)
    print('Experimental output contains other game data; not for release.')
except Exception:
    for p in a.output_dir.iterdir():p.unlink()
    a.output_dir.rmdir();raise
