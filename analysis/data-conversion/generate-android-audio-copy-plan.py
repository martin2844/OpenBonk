#!/usr/bin/env python3
"""Generate offset-only metadata for 63 pinned Steam audio resources."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
BASE=ROOT/'build/android/localization-shared-scrubbed-base/base.obb'
LINUX=ROOT/'build/android/linux-steam-regression.zip'
WINDOWS=ROOT/'build/android/windows-steam-full-input.zip'
TARGET=ROOT/'build/android/launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
MANIFEST=ROOT/'build/android/local-research-manifests/hashed-audio-template/manifest.json'
OUTPUT=ROOT/'build/android/audio-android-copy-plan/audio-plan.bin'
BASE_SHA='ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
TARGET_SHA='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691'
MANIFEST_SHA='6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f'
SOURCE_HASHES=(('d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35','28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'),('64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a','29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d'))
def sha(b):return hashlib.sha256(b).hexdigest()
def file_sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--base',type=Path,default=BASE);p.add_argument('--linux-zip',type=Path,default=LINUX)
    p.add_argument('--windows-zip',type=Path,default=WINDOWS);p.add_argument('--target',type=Path,default=TARGET)
    p.add_argument('--manifest',type=Path,default=MANIFEST);p.add_argument('--output',type=Path,default=OUTPUT)
    a=p.parse_args();output=a.output.absolute()
    if output.exists() or output.is_symlink():raise ValueError('output exists')
    for path,expected in ((a.base,BASE_SHA),(a.target,TARGET_SHA),(a.manifest,MANIFEST_SHA)):
        if file_sha(path)!=expected:raise ValueError('input pin mismatch: '+str(path))
    records=json.loads(a.manifest.read_text())['clips']
    if len(records)!=63 or len({r['file'] for r in records})!=63:raise ValueError('audio inventory mismatch')
    plan=bytearray(b'OBSAU001'+struct.pack('>I',len(records)))
    for pair in SOURCE_HASHES:
        for h in pair:plan.extend(bytes.fromhex(h))
    with zipfile.ZipFile(a.base) as base,zipfile.ZipFile(a.target) as target,zipfile.ZipFile(a.linux_zip) as linux,zipfile.ZipFile(a.windows_zip) as windows:
        source_data=[];source_objects=[]
        for z,pins in zip((linux,windows),SOURCE_HASHES):
            asset=z.read('Megabonk_Data/sharedassets1.assets');resource=z.read('Megabonk_Data/sharedassets1.resource')
            if (sha(asset),sha(resource))!=pins:raise ValueError('Steam source mismatch')
            source_data.append(resource);source_objects.append({o.path_id:o for o in UnityPy.load(asset).objects})
        for row in records:
            name=row['file'];entry='assets/bin/Data/'+name
            if len(name)!=32 or any(c not in '0123456789abcdef' for c in name):raise ValueError('invalid output name')
            template=base.read(entry);expected=target.read(entry);payload=target.read(entry+'.resource')
            if (sha(template),sha(expected),sha(payload))!=(row['asset_template_sha256'],row['asset_sha256'],row['resource_sha256']):raise ValueError('target mismatch')
            offsets=[]
            for objects,resource in zip(source_objects,source_data):
                obj=objects[row['source_id']]
                if obj.type.name!='AudioClip':raise ValueError('audio source type mismatch')
                clip=obj.read();ref=clip.m_Resource
                if clip.m_Name!=row['source_name'] or ref.m_Source!='sharedassets1.resource' or ref.m_Size!=row['resource_size'] or ref.m_Offset<0 or ref.m_Offset+ref.m_Size>len(resource):raise ValueError('audio source metadata mismatch')
                if resource[ref.m_Offset:ref.m_Offset+ref.m_Size]!=payload:raise ValueError('audio bytes differ')
                offsets.append(ref.m_Offset)
            add=len(expected)-len(template)
            if add not in (0,4) or struct.unpack_from('>Q',template,24)[0]!=len(template) or struct.unpack_from('<Q',template,len(template)-12)[0]!=row['original_size']:raise ValueError('unsupported metadata layout')
            rebuilt=bytearray(template)+bytes(add);struct.pack_into('>Q',rebuilt,24,len(rebuilt));struct.pack_into('<Q',rebuilt,len(template)-12,len(payload))
            if rebuilt!=expected:raise ValueError('metadata reconstruction differs')
            plan.extend(name.encode('ascii'))
            plan.extend(struct.pack('>QQIIII',*offsets,len(payload),row['original_size'],len(template),add))
            for key in ('asset_template_sha256','asset_sha256','resource_sha256'):plan.extend(bytes.fromhex(row[key]))
    output.parent.mkdir(parents=True,exist_ok=True)
    with output.open('xb') as f:f.write(plan)
    print('63 audio clips;',sum(r['resource_size'] for r in records),'payload bytes; plan',len(plan),'sha256',sha(plan),'offsets and metadata only')
if __name__=='__main__':main()
