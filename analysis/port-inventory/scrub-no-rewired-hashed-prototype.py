#!/usr/bin/env python3
"""Blank selected hashed payloads in a local diagnostic base.

Other original game data, metadata and code remain. The APK is unsigned and
the output is not a release, rights review, or distributable package.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import zipfile
import zlib

ROOT=Path(__file__).resolve().parents[2]
APK_SHA='e131cce897989fe2b05cb304aa749e25e4c3c2e90a60d2d7fc8f5870ad106ce3'
OBB_SHA='ca8694de6c7148e1c93eb2d9fa45525edfaf87e39deffad67641c80f2e3bab23'
ASSET_MANIFEST_SHA='c4cec041aec2aac138b0ddae6b62ca354701340dd8b0eae02f6938aff5e1a082'
EXPANDED_APK_SHA='78e96bb74b09523d17036dec1036dae98c53768bb8b6ab574cf0d25217a341b8'
EXPANDED_OBB_SHA='ca049f82bb20220dd34ef636938f95e0f09ef0bdf4a0bf59795e0b03b87b9ce3'
EXPANDED_ASSET_MANIFEST_SHA='294055b5e8c9edec427883c2d1ea73fddcd39b25e9f2ad37b8804a08b8924325'
spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)
spec2=importlib.util.spec_from_file_location('builder',ROOT/'tools/build-no-rewired-data-diagnostic.py');builder=importlib.util.module_from_spec(spec2);spec2.loader.exec_module(builder)


def digest(b):return hashlib.sha256(b).hexdigest()
def require(p,h):
    if not p.is_file() or repair.sha(p)!=h:raise ValueError('incompatible input: '+str(p))


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('output',type=Path);p.add_argument('--expanded-shared-base',action='store_true');a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out,src=a.output.resolve(),a.base.resolve()
    if out.is_relative_to(src) or src.is_relative_to(out):p.error('output must be separate from base')
    apk=a.base/'base.apk';obb=a.base/'base.obb';asset_manifest=a.base/'asset-manifest.json'
    input_apk_sha=EXPANDED_APK_SHA if a.expanded_shared_base else APK_SHA;input_obb_sha=EXPANDED_OBB_SHA if a.expanded_shared_base else OBB_SHA;asset_manifest_sha=EXPANDED_ASSET_MANIFEST_SHA if a.expanded_shared_base else ASSET_MANIFEST_SHA
    require(apk,input_apk_sha);require(obb,input_obb_sha);require(asset_manifest,asset_manifest_sha)
    with tempfile.TemporaryDirectory(prefix='.openbonk-hashed-base-',dir=a.output.parent) as tmp:
        tmp=Path(tmp);new_apk=tmp/'base.apk';new_obb=tmp/'base.obb';replacements={};entries={};summary={}
        with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
            for z in (az,oz):
                if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('APK/OBB GUID mismatch')
            dex=az.read('classes.dex');old=input_obb_sha.encode('ascii');previous,following=repair.dex_bounds(dex,old)
            for i,(folder,key,count) in enumerate(builder.HASHED_CATEGORIES):
                directory=builder.BUILD/'share-source-experiment'/folder;manifest=directory/'manifest.json';require(manifest,builder.HASHED_MANIFEST_HASHES[i]);rows=json.loads(manifest.read_text())[key]
                if len(rows)!=count or len({x['file'] for x in rows})!=count:raise ValueError('invalid category inventory')
                selected=removed=0
                for row in rows:
                    name=row['file'];entry='assets/bin/Data/'+name
                    if entry in entries or oz.namelist().count(entry)!=1:raise ValueError('missing or duplicate resource: '+entry)
                    raw=oz.read(entry)
                    if key=='clips':
                        if digest(raw)!=row['asset_template_sha256']:raise ValueError('audio object mismatch: '+entry)
                        entries[entry]={'original_sha256':digest(raw),'scrubbed_sha256':digest(raw),'kind':folder}
                        resource=entry+'.resource'
                        if resource in entries or oz.namelist().count(resource)!=1:raise ValueError('missing audio stream')
                        audio=oz.read(resource)
                        if len(audio)!=row['original_size'] or digest(audio)!=row['original_resource_sha256']:raise ValueError('audio stream mismatch')
                        blank=bytes(len(audio));replacements[resource]=blank;selected+=len(audio);removed+=sum(x!=0 for x in audio)
                        entries[resource]={'original_sha256':digest(audio),'scrubbed_sha256':digest(blank),'kind':folder}
                    else:
                        if digest(raw)!=row['original_sha256']:raise ValueError('original resource mismatch: '+entry)
                        template=directory/name;require(template,row['template_sha256']);blank=template.read_bytes()
                        if len(blank)!=len(raw):raise ValueError('template size mismatch')
                        spans=[(row['vertex_offset'],row['vertex_size']),(row['index_offset'],row['index_size'])] if key=='meshes' else [(row['offset'],row['size'])]
                        spans.sort();candidate=bytearray(raw)
                        if any(o<0 or n<0 or o+n>len(raw) for o,n in spans) or any(x[0]+x[1]>y[0] for x,y in zip(spans,spans[1:])):raise ValueError('invalid payload bounds')
                        for offset,size in spans:
                            candidate[offset:offset+size]=bytes(size);selected+=size;removed+=sum(x!=0 for x in raw[offset:offset+size])
                        if candidate!=blank:raise ValueError('template changes other bytes')
                        replacements[entry]=blank;entries[entry]={'original_sha256':digest(raw),'scrubbed_sha256':digest(blank),'kind':folder}
                summary[folder]={'items':count,'selected_bytes':selected,'nonzero_bytes_removed':removed}
            with zipfile.ZipFile(new_obb,'x',allowZip64=True) as dest:
                for info in oz.infolist():dest.writestr(copy.copy(info),replacements[info.filename] if info.filename in replacements else oz.read(info))
                dest.comment=b'00000000'
            h=hashlib.sha256()
            with new_obb.open('rb') as f:
                remaining=new_obb.stat().st_size-8
                while remaining:
                    b=f.read(min(1048576,remaining))
                    if not b:raise ValueError('short OBB')
                    h.update(b);remaining-=len(b)
            for nonce in range(1_000_000):
                comment=f'{nonce:08x}'.encode();v=h.copy();v.update(comment);new=v.hexdigest().encode('ascii')
                if previous<new<following and new!=old:break
            else:raise ValueError('could not preserve DEX string order')
            with new_obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
            if repair.sha(new_obb).encode()!=new:raise ValueError('OBB digest mismatch')
            patched=bytearray(dex.replace(old,new));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
            with zipfile.ZipFile(new_apk,'x',allowZip64=True) as dest:
                for info in az.infolist():
                    if repair.jar_signature(info.filename):continue
                    dest.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
        with zipfile.ZipFile(new_apk) as az,zipfile.ZipFile(new_obb) as oz:
            if az.testzip() is not None or oz.testzip() is not None or az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output pair validation failed')
            for name,rec in entries.items():
                if digest(oz.read(name))!=rec['scrubbed_sha256']:raise ValueError('scrubbed resource differs')
        (tmp/'hashed-manifest.json').write_text(json.dumps({'version':1,'kind':'local research only, not for distribution','entries':entries},indent=2)+'\n')
        report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(new_apk),'obb_sha256':repair.sha(new_obb),'hashed_manifest_sha256':repair.sha(tmp/'hashed-manifest.json'),'asset_manifest_sha256':asset_manifest_sha,'removed':summary,'limits':['APK is unsigned.','Only selected payloads are blanked; other game content and code remain.','Source matches, metadata and third-party rights need review.']}
        (tmp/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        a.output.mkdir()
        try:
            for f in (new_apk,new_obb,tmp/'hashed-manifest.json',tmp/'report.json',asset_manifest):shutil.copyfile(f,a.output/f.name)
        except BaseException:shutil.rmtree(a.output);raise
        print(json.dumps(report,indent=2),flush=True)


if __name__=='__main__':main()
