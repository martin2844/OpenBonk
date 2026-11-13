#!/usr/bin/env python3
"""Produce a local APK/OBB base with selected scene bodies zeroed.

Other game content and third-party code remain. The output APK is unsigned;
this is not a public installer, a rights review, or a distributable package.
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
FIRST=ROOT/'build/android/no-rewired-scene-template'
EXTRA=ROOT/'build/android/no-rewired-scene-extra-template'
APK_SHA='b3255411ab3e3f00001daa22c3ecd48e7f1db3c31f290bdd545237c65c45bb38'
OBB_SHA='ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
FIRST_SHA='a77039c682ed1651c2b9a2d195dffff33b2bfad49cd1da42278bc2152bfa854f'
EXTRA_SHA='3c2455aaf75c2fe9a9a1a8f649d8c770dd6ae4db16560dce01ee0703034a7775'
spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)


def digest(b):return hashlib.sha256(b).hexdigest()
def require(p,h):
    if not p.is_file() or repair.sha(p)!=h:raise ValueError('incompatible input: '+str(p))


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('apk',type=Path);p.add_argument('obb',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    require(a.apk,APK_SHA);require(a.obb,OBB_SHA);require(FIRST/'manifest.json',FIRST_SHA);require(EXTRA/'extra-manifest.json',EXTRA_SHA)
    first=json.loads((FIRST/'manifest.json').read_text())['scenes'];extra=json.loads((EXTRA/'extra-manifest.json').read_text())['scenes']
    with tempfile.TemporaryDirectory(prefix='.openbonk-scene-base-',dir=a.output.parent) as tmp:
        tmp=Path(tmp);obb=tmp/'base.obb';apk=tmp/'base.apk'; replacements={};metrics={}
        with zipfile.ZipFile(a.apk) as az,zipfile.ZipFile(a.obb) as oz:
            for z in (az,oz):
                if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate entries')
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('APK/OBB GUID mismatch')
            dex=az.read('classes.dex'); old=OBB_SHA.encode('ascii'); previous,following=repair.dex_bounds(dex,old)
            for scene in ('level1','level2','level4'):
                prefix='assets/bin/Data/'+scene+'.split';parts=sorted((x for x in oz.namelist() if x.startswith(prefix)),key=lambda x:int(x[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('scene fragments missing')
                original=b''.join(oz.read(x) for x in parts);blank=(EXTRA/scene).read_bytes()
                if digest(original)!=first[scene]['original_sha256'] or digest(blank)!=extra[scene]['template_sha256'] or len(original)!=len(blank):raise ValueError('scene template mismatch')
                ranges=sorted([(r['offset'],r['size']) for r in first[scene]['records']]+[(r['offset'],r['size']) for r in extra[scene]['records']])
                if any(size<=0 or off<0 or off+size>len(original) for off,size in ranges) or any(x[0]+x[1]>y[0] for x,y in zip(ranges,ranges[1:])):raise ValueError('invalid or overlapping body ranges')
                cursor=0;changed=0
                for off,size in ranges:
                    if original[cursor:off]!=blank[cursor:off]:raise ValueError('unexpected change outside body')
                    oldbody=original[off:off+size];newbody=blank[off:off+size]
                    if any(n not in (0,o) for o,n in zip(oldbody,newbody)):raise ValueError('unexpected byte in body')
                    changed+=sum(o!=n for o,n in zip(oldbody,newbody));cursor=off+size
                if original[cursor:]!=blank[cursor:]:raise ValueError('unexpected trailing change')
                start=0
                for part in parts:
                    size=oz.getinfo(part).file_size;replacements[part]=blank[start:start+size];start+=size
                metrics[scene]={'bodies':len(ranges),'selected_bytes':sum(n for _,n in ranges),'nonzero_bytes_removed':changed}
            with zipfile.ZipFile(obb,'x',allowZip64=True) as dest:
                for info in oz.infolist():dest.writestr(copy.copy(info),replacements.get(info.filename,oz.read(info)))
                dest.comment=b'00000000'
            h=hashlib.sha256()
            with obb.open('rb') as f:
                left=obb.stat().st_size-8
                while left:
                    b=f.read(min(1048576,left))
                    if not b:raise ValueError('short OBB')
                    h.update(b);left-=len(b)
            for n in range(1_000_000):
                comment=f'{n:08x}'.encode();v=h.copy();v.update(comment);new=v.hexdigest().encode('ascii')
                if previous<new<following and new!=old:break
            else:raise ValueError('could not retain DEX string ordering')
            with obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
            if repair.sha(obb).encode()!=new:raise ValueError('scrubbed OBB hash mismatch')
            patched=bytearray(dex.replace(old,new));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
            with zipfile.ZipFile(apk,'x',allowZip64=True) as dest:
                for info in az.infolist():
                    if repair.jar_signature(info.filename):continue
                    dest.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
        with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
            if az.testzip() is not None or oz.testzip() is not None or az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output pair validation failed')
        report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(apk),'obb_sha256':repair.sha(obb),'scenes':metrics,'limits':['APK is unsigned and must be rebuilt before use.','Only selected scene bodies are zeroed; other game data and code remain.','Pointers and metadata remain and rights have not been reviewed.']}
        (tmp/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        a.output.mkdir()
        try:
            for f in (apk,obb,tmp/'report.json'):shutil.copyfile(f,a.output/f.name)
        except BaseException:shutil.rmtree(a.output);raise
        print(json.dumps(report,indent=2),flush=True)


if __name__=='__main__':main()
