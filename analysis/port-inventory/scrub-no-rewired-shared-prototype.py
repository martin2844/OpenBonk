#!/usr/bin/env python3
"""Blank selected reconstructable objects and audio in a local research base.

Not for distribution: many game assets, headers, code and dependencies remain.
The output APK is unsigned. This tool does not modify its inputs.
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
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
INPUT_APK='76386a92c658dda1d262f2611c6bed5182196c5a36d372eb2b6ca8725215857f'
INPUT_OBB='cac0c58a6852be36afc9e284e88bdad443a74ac8ce52db4b905b2442f79914a3'
CONVERTED_OBB='bad3e899a7d75e7be6828261e04604f6d82c128296b0da125f4b48d0be0cdeb9'
EXTRA_PLANS={'mesh1':('full-cubemaps-stripped/mesh-manifest.json','bf97a55467b07f123143ad1c9e787f935441f077d5ec3a291e9f42c9b1677bf3','meshes',420),'terrain':('shared1-terrain-animation-template/objects-manifest.json','9641811a775b44f61ab6b2148ceb384ea6fa2155acd99a5d8fc44449402a1259','objects',13),'mesh2':('shared2-mesh-template/mesh-manifest.json','831fee2261236f0ec9db20798e5b8f4c05bfb2baa786e26d9ed65b5acfb26b61','meshes',251),'texture2':('shared2-matched72-template/textures-manifest.json','9df05620261b30b27f3b64ed050ba3dfdd2fe087087b2c1993974e005048386e','textures',72),'atlas2':('shared2-atlases-template/atlases-manifest.json','cad14caa26c75e5dce4efc66f05be00517ac0281f25314f8709ed878dc7f3080','textures',2)}
spec=importlib.util.spec_from_file_location('pinned',ROOT/'analysis/data-conversion/apply-pinned-converted-objects.py');pinned=importlib.util.module_from_spec(spec);spec.loader.exec_module(pinned)
spec2=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec2);spec2.loader.exec_module(repair)


def digest(b):return hashlib.sha256(b).hexdigest()
def require(p,h):
    if not p.is_file() or repair.sha(p)!=h:raise ValueError('incompatible input: '+str(p))


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('converted_obb',type=Path);p.add_argument('output',type=Path);p.add_argument('--include-identical-rebuilt',action='store_true',help='also blank known reconstructed objects that already match');a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve();src=a.base.resolve()
    if out.is_relative_to(src) or src.is_relative_to(out):p.error('output must be separate from base')
    apk=a.base/'base.apk';obb=a.base/'base.obb';require(apk,INPUT_APK);require(obb,INPUT_OBB);require(a.converted_obb,CONVERTED_OBB)
    plans={}
    if a.include_identical_rebuilt:
        for key,(path,h,field,count) in EXTRA_PLANS.items():
            f=ROOT/'build/android/share-source-experiment'/path;require(f,h);rows=json.loads(f.read_text())[field]
            if len(rows)!=count or len({r['target_id'] for r in rows})!=count:raise ValueError('invalid extra manifest: '+key)
            plans[key]=rows
    replacements_apk={};replacements_obb={};summary={};manifest={'version':1,'kind':'local research only, not for distribution','include_identical_rebuilt':a.include_identical_rebuilt,'assets':{}}
    with tempfile.TemporaryDirectory(prefix='.openbonk-shared-base-',dir=a.output.parent) as temp:
        tmp=Path(temp);new_apk=tmp/'base.apk';new_obb=tmp/'base.obb'
        with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz,zipfile.ZipFile(a.converted_obb) as cz:
            for z in (az,oz,cz):
                if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('APK/OBB GUID mismatch')
            old=INPUT_OBB.encode('ascii');dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,old)
            for i in range(3):
                name=f'sharedassets{i}.assets';z=az if i==0 else oz;dest=replacements_apk if i==0 else replacements_obb
                original=pinned.base.archive_compare.load_archive(z,name);converted=pinned.base.archive_compare.load_archive(cz,name)
                file,rep_sha,count=pinned.REPORTS[name];rep=ROOT/'build/android'/file;require(rep,rep_sha);m=json.loads(rep.read_text())
                if digest(original)!=m['new_sha256'] or digest(converted)!=m['converted_sha256']:raise ValueError('asset source mismatch: '+name)
                old_objs=pinned.base.objects(UnityPy.load(original));converted_objs=pinned.base.objects(UnityPy.load(converted));rows=list(m['changed_objects'])
                if len(rows)!=count or len({r['path_id'] for r in rows})!=count or old_objs.keys()!=converted_objs.keys():raise ValueError('invalid object inventory')
                if i==0:
                    ids={r['path_id'] for r in rows};fonts=[o for o in old_objs.values() if o.type.name=='Font' and o.path_id not in ids]
                    if len(fonts)!=7:raise ValueError('unexpected fonts')
                    for obj in fonts:
                        raw=obj.get_raw_data();c=converted_objs[obj.path_id]
                        if c.type.name!='Font' or raw!=c.get_raw_data():raise ValueError('font source mismatch')
                        rows.append({'path_id':obj.path_id,'type':'Font','original_sha256':digest(raw),'converted_sha256':digest(raw)})
                if a.include_identical_rebuilt and i in (1,2):
                    already={r['path_id'] for r in rows}
                    if i==1:
                        textures=[o.path_id for o in old_objs.values() if o.type.name=='Texture2D']
                        if len(textures)!=847:raise ValueError('unexpected sharedassets1 texture inventory')
                        candidate_ids=set(textures)|{r['target_id'] for r in plans['mesh1']}|{r['target_id'] for r in plans['terrain']}
                        extra_count=609
                    else:
                        candidate_ids={r['target_id'] for key in ('mesh2','texture2','atlas2') for r in plans[key]}
                        extra_count=260
                    candidates=candidate_ids-already
                    if len(candidates)!=extra_count:raise ValueError('unexpected identical rebuilt object count')
                    for obj_id in sorted(candidates):
                        obj=old_objs[obj_id];c=converted_objs[obj_id];raw=obj.get_raw_data();cb=c.get_raw_data()
                        if obj.type.name!=c.type.name or raw!=cb:raise ValueError('identical rebuilt object mismatch: '+str(obj_id))
                        rows.append({'path_id':obj_id,'type':obj.type.name,'original_sha256':digest(raw),'converted_sha256':digest(cb)})
                work=bytearray(original);records=[];changed=0
                for row in rows:
                    obj=old_objs[row['path_id']];c=converted_objs[row['path_id']];raw=obj.get_raw_data();cb=c.get_raw_data();start=obj.byte_start
                    original_sha=pinned.WATER['new_sha256'] if i==1 and obj.path_id==pinned.WATER['path_id'] else row['original_sha256']
                    if obj.type.name!=row['type'] or c.type.name!=row['type'] or digest(raw)!=original_sha or digest(cb)!=row['converted_sha256'] or original[start:start+len(raw)]!=raw:raise ValueError('object mismatch: '+str(obj.path_id))
                    changed+=sum(x!=0 for x in raw);work[start:start+len(raw)]=bytes(len(raw))
                    records.append({'path_id':obj.path_id,'type':obj.type.name,'offset':start,'size':len(raw),'converted_sha256':digest(cb)})
                records.sort(key=lambda r:r['offset'])
                if any(x['offset']+x['size']>y['offset'] for x,y in zip(records,records[1:])):raise ValueError('overlapping object ranges')
                prefix='assets/bin/Data/'+name+'.split';parts=sorted((x for x in z.namelist() if x.startswith(prefix)),key=lambda x:int(x[len(prefix):]))
                if not parts or [int(x[len(prefix):]) for x in parts]!=list(range(len(parts))):raise ValueError('missing asset fragments')
                pos=0
                for part in parts:
                    size=z.getinfo(part).file_size;dest[part]=bytes(work[pos:pos+size]);pos+=size
                if pos!=len(work):raise ValueError('fragment size mismatch')
                manifest['assets'][name]={'input_sha256':digest(original),'scrubbed_sha256':digest(work),'output_sha256':m['output_sha256'],'objects':records}
                summary[name]={'objects':len(records),'selected_bytes':sum(r['size'] for r in records),'nonzero_bytes_removed':changed}
            for name in ('sharedassets1.resource','sharedassets2.resource'):
                entry='assets/bin/Data/'+name
                if oz.namelist().count(entry)!=1:raise ValueError('missing audio resource')
                raw=oz.read(entry);replacements_obb[entry]=bytes(len(raw));summary[name]={'selected_bytes':len(raw),'nonzero_bytes_removed':sum(x!=0 for x in raw)}
            with zipfile.ZipFile(new_obb,'x',allowZip64=True) as dest:
                for info in oz.infolist():dest.writestr(copy.copy(info),replacements_obb[info.filename] if info.filename in replacements_obb else oz.read(info))
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
                    data=bytes(patched) if info.filename=='classes.dex' else replacements_apk[info.filename] if info.filename in replacements_apk else az.read(info)
                    dest.writestr(copy.copy(info),data)
        with zipfile.ZipFile(new_apk) as az,zipfile.ZipFile(new_obb) as oz:
            if az.testzip() is not None or oz.testzip() is not None or az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output pair validation failed')
        (tmp/'asset-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
        report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(new_apk),'obb_sha256':repair.sha(new_obb),'asset_manifest_sha256':repair.sha(tmp/'asset-manifest.json'),'removed':summary,'limits':['APK is unsigned.','Only listed objects and audio streams were blanked; other game content and code remain.','Serialized metadata, references and third-party dependencies need review.']}
        (tmp/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        a.output.mkdir()
        try:
            for f in (new_apk,new_obb,tmp/'asset-manifest.json',tmp/'report.json'):shutil.copyfile(f,a.output/f.name)
        except BaseException:shutil.rmtree(a.output);raise
        print(json.dumps(report,indent=2),flush=True)


if __name__=='__main__':main()
