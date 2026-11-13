#!/usr/bin/env python3
"""Blank Steam-reconstructible GameObject value tails in a pinned local base.

The output still contains game and third-party data and is not distributable.
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

spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'))
repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)
BASE_APK='5e251631fe41877dcd53f101346a3c19e75714971b4e0f72d765083012a4f27a'
BASE_OBB='ca12334ed5b939c61c01914e0caf3180087db6926b4feab82648caaf4f01e9bd'
MANIFEST='991d529e746effdc75c075f610ec88f3126f15520d61efdd6f7b0eb3b1f9bd04'
BASE_SCENES={'level1': '36836545a5b43f63a3fd9da78122523ffb24520c63dc47e730da171e148d47b9', 'level2': 'b892998b260b74b1d772904db1c7c05d34d1d235f17393cad992e8eaf53d3c1b', 'level4': 'db89e220410dc4eaee34b6b3e8bd4d1a197ccf6866c2e75bdb4b49b3e1c89cb7'}


def sha(b):return hashlib.sha256(b).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('template',type=Path);p.add_argument('original_obb',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
    for src in (a.base,a.template,a.original_obb):
        src=src.resolve()
        if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
    for path,expected in ((a.base/'base.apk',BASE_APK),(a.base/'base.obb',BASE_OBB),(a.template/'manifest.json',MANIFEST),
                          (a.original_obb,'ca916fd870e4546c70a66428591272ef155987140495c1a0dd446a332b578cc2')):
        if not path.is_file() or repair.sha(path)!=expected:raise ValueError('incompatible input: '+str(path))
    manifest=json.loads((a.template/'manifest.json').read_text()); metrics={};replacements={}
    with tempfile.TemporaryDirectory(prefix='.openbonk-transform-base-',dir=out.parent) as tmp:
        tmp=Path(tmp);new_apk=tmp/'base.apk';new_obb=tmp/'base.obb'
        with zipfile.ZipFile(a.base/'base.apk') as az,zipfile.ZipFile(a.base/'base.obb') as oz,zipfile.ZipFile(a.original_obb) as original:
            for z in (az,oz,original):repair.unique(z)
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('base GUID mismatch')
            old=BASE_OBB.encode();dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,old)
            for scene,expected_base in BASE_SCENES.items():
                prefix='assets/bin/Data/'+scene+'.split'
                parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                oparts=sorted((n for n in original.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))) or parts!=oparts:raise ValueError('invalid scene fragments')
                source=b''.join(original.read(n) for n in parts);base=b''.join(oz.read(n) for n in parts);blank=(a.template/scene).read_bytes();record=manifest['scenes'][scene]
                if sha(source)!=record['original_sha256'] or sha(base)!=expected_base or sha(blank)!=record['template_sha256'] or len(source)!=len(base) or len(base)!=len(blank):raise ValueError('incompatible scene: '+scene)
                objects={o.path_id:o for o in UnityPy.load(source).objects};ranges=[]
                for row in record['objects']:
                    obj=objects[row['target_id']]
                    if obj.type.name!='GameObject':raise ValueError('unexpected object type')
                    offset=4+12*len(obj.read_typetree()['m_Component'])
                    ranges.append((obj.byte_start+offset,obj.byte_start+obj.byte_size))
                work=bytearray(base);cursor=0;nonzero=0
                for start,end in sorted(ranges):
                    if start<cursor or end>len(base) or source[cursor:start]!=blank[cursor:start] or any(blank[start:end]) or source[start:end]!=base[start:end]:raise ValueError('unexpected GameObject value range')
                    nonzero+=sum(x!=0 for x in base[start:end]);work[start:end]=bytes(end-start);cursor=end
                if source[cursor:]!=blank[cursor:]:raise ValueError('unexpected template trailing change')
                transformed=bytes(work);pos=0
                for name in parts:
                    size=oz.getinfo(name).file_size;replacements[name]=transformed[pos:pos+size];pos+=size
                metrics[scene]={'objects':len(record['objects']),'selected_bytes':sum(b-a for a,b in ranges),
                                'nonzero_bytes_removed':nonzero,'base_sha256':expected_base,'blank_sha256':sha(transformed)}
            with zipfile.ZipFile(new_obb,'x',allowZip64=True) as dst:
                for info in oz.infolist():dst.writestr(copy.copy(info),replacements.get(info.filename,oz.read(info)))
                dst.comment=b'00000000'
            h=hashlib.sha256()
            with new_obb.open('rb') as f:
                remaining=new_obb.stat().st_size-8
                while remaining:
                    block=f.read(min(1048576,remaining))
                    if not block:raise ValueError('short OBB')
                    h.update(block);remaining-=len(block)
            for nonce in range(1_000_000):
                comment=f'{nonce:08x}'.encode();candidate=h.copy();candidate.update(comment);new=candidate.hexdigest().encode()
                if previous<new<following and new!=old:break
            else:raise ValueError('cannot preserve DEX string ordering')
            with new_obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
            if repair.sha(new_obb).encode()!=new:raise ValueError('OBB hash mismatch')
            patched=bytearray(dex.replace(old,new));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
            with zipfile.ZipFile(new_apk,'x',allowZip64=True) as dst:
                for info in az.infolist():
                    if not repair.jar_signature(info.filename):dst.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
        with zipfile.ZipFile(new_apk) as az,zipfile.ZipFile(new_obb) as oz:
            repair.unique(az);repair.unique(oz)
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output pair mismatch')
            if any(oz.read(n)!=v for n,v in replacements.items()):raise ValueError('output scene fragments differ')
        report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(new_apk),'obb_sha256':repair.sha(new_obb),'scenes':metrics,
                'limitations':['Other game and third-party data remain.','Pointers and metadata are retained.','Not a distributable package.']}
        out.mkdir(mode=0o700)
        try:
            for f in a.base.iterdir():
                if f.is_file() and f.name not in ('base.apk','base.obb','report.json'):shutil.copyfile(f,out/f.name)
            shutil.copyfile(new_apk,out/'base.apk');shutil.copyfile(new_obb,out/'base.obb')
            shutil.copyfile(a.template/'manifest.json',out/'gameobject-manifest.json')
            (out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        except BaseException:shutil.rmtree(out);raise
    print(json.dumps(report,indent=2))


if __name__=='__main__':main()
