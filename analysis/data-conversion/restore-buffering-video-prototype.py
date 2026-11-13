#!/usr/bin/env python3
"""Restore the buffering clip from one pinned Steam install to a local build.

Research only. The resulting APK/OBB contain game data and other material;
they are not cleared for distribution and are not a general user installer.
"""
import argparse
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import tempfile
import zipfile
import zlib

import UnityPy

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('repair', ROOT/'analysis/port-inventory/repair-split-guid-probe.py')
repair = importlib.util.module_from_spec(spec); spec.loader.exec_module(repair)
APK_SHA = 'cbad1fa9f908703c6d9096e6853fb24d688cef0e81094c1f42b671b532dd3b8c'
APK_CONTENT_SHA = None  # Optional pinned payload digest, independent of APK signature.
OBB_SHA = 'd53ff09b9662d8b6315bad35fe78e682c20f11ba96e0a7899c5cb708a4a61917'
PC_ASSET_SHA = 'd9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
PC_RESOURCE_SHA = '28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c'
WINDOWS_ASSET_SHA = '64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a'
WINDOWS_RESOURCE_SHA = '29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d'
CLIP_SHA = '020a724dccd35bfd6d2cbab07b40e6a8b4be19dc3433dbdf37f185f49f276be1'
OBB_NAME = 'main.1.com.megabonk.nativeport.splittest.obb'


def sha(data): return hashlib.sha256(data).hexdigest()


def require(path, expected):
    if not path.is_file() or repair.sha(path) != expected: raise ValueError('incompatible input: '+str(path))


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('input',type=Path);p.add_argument('output',type=Path)
    p.add_argument('--windows-source',action='store_true',help='use the pinned Windows resource for this clip')
    a=p.parse_args()
    pc=a.pc_data.resolve();inp=a.input.resolve();out=a.output.absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
    for source in (pc,inp):
        if out.resolve().is_relative_to(source) or source.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
    apk=inp/'OpenBonk-diagnostic.apk';obb=inp/OBB_NAME;pc_asset=pc/'sharedassets1.assets';pc_resource=pc/'sharedassets1.resource'
    if APK_CONTENT_SHA:
        spec=importlib.util.spec_from_file_location('apk_content_digest',ROOT/'tools/apk-content-digest.py')
        content=importlib.util.module_from_spec(spec);spec.loader.exec_module(content)
        if not apk.is_file() or content.digest(apk)!=APK_CONTENT_SHA:raise ValueError('incompatible APK payload: '+str(apk))
    else:require(apk,APK_SHA)
    for path,h in ((obb,OBB_SHA),(pc_asset,WINDOWS_ASSET_SHA if a.windows_source else PC_ASSET_SHA),(pc_resource,WINDOWS_RESOURCE_SHA if a.windows_source else PC_RESOURCE_SHA)):require(path,h)
    pc_clips=[o for o in UnityPy.load(str(pc_asset)).objects if o.type.name=='VideoClip']
    if len(pc_clips)!=1:raise ValueError('unexpected Steam clip inventory')
    pc_clip=pc_clips[0].read_typetree();ext=pc_clip['m_ExternalResources']
    if (pc_clips[0].path_id!=3037 or pc_clip['m_Name']!='Buffering3' or ext['m_Source']!='sharedassets1.resource' or ext['m_Offset']!=60836960 or ext['m_Size']!=8740):raise ValueError('unexpected Steam clip metadata')
    with pc_resource.open('rb') as f:f.seek(ext['m_Offset']);clip=f.read(ext['m_Size'])
    if a.windows_source:
        # The two depot clips differ only in seven bytes of WebM metadata.
        clip=bytearray(clip);offset=60837212-ext['m_Offset']
        if clip[offset:offset+7]!=b'\xb4'*7:raise ValueError('unexpected Windows video metadata')
        clip[offset:offset+7]=b'p'*7;clip=bytes(clip)
    if sha(clip)!=CLIP_SHA:raise ValueError('Steam clip bytes differ')
    unity=Path(os.environ.get('OPENBONK_UNITY_EDITOR',str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Data/PlaybackEngines/AndroidPlayer';tools=Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT',str(unity/'SDK'))).expanduser()/'build-tools/34.0.0';java=Path(os.environ.get('OPENBONK_JAVA_HOME',str(unity/'OpenJDK'))).expanduser();key=Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE',str(Path.home()/'.android/debug.keystore'))).expanduser()
    for path in (tools/'zipalign',tools/'apksigner',java/'bin/java',key):
        if not path.is_file():p.error('missing prerequisite: '+str(path))
    runner=os.environ.get('OPENBONK_TOOL_RUNNER','steam-run' if shutil.which('steam-run') else 'none')
    if runner!='none' and not shutil.which(runner):p.error('configured runner missing: '+runner)
    runner_prefix=[] if runner=='none' else [runner]
    env=os.environ.copy();env['JAVA_HOME']=str(java);env['PATH']=str(java/'bin')+os.pathsep+env.get('PATH','')
    if APK_CONTENT_SHA:
        subprocess.run(runner_prefix+[str(tools/'zipalign'),'-c','-p','4',str(apk)],env=env,check=True)
        subprocess.run(runner_prefix+[str(tools/'apksigner'),'verify',str(apk)],env=env,check=True)
    with tempfile.TemporaryDirectory(prefix='.openbonk-video-',dir=out.parent) as t:
        t=Path(t);new_obb=t/OBB_NAME;unsigned=t/'unsigned.apk';aligned=t/'aligned.apk';signed=t/'OpenBonk-diagnostic.apk'
        with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
            for z in (az,oz):repair.unique(z)
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('input GUID mismatch')
            prefix='assets/bin/Data/sharedassets1.assets.split';parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
            if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('invalid asset fragments')
            asset=b''.join(oz.read(n) for n in parts);resource=oz.read('assets/bin/Data/sharedassets1.resource')
            objects=UnityPy.load(asset).objects;clips=[o for o in objects if o.type.name=='VideoClip']
            if len(clips)!=1:raise ValueError('unexpected Android clip inventory')
            obj=clips[0];tree=obj.read_typetree();ref=tree['m_ExternalResources'];raw=obj.get_raw_data()
            if (obj.path_id!=1937 or tree['m_Name']!='Buffering3' or ref['m_Source']!='sharedassets1.resource' or ref['m_Offset']!=28321376 or ref['m_Size']!=len(clip)):
                raise ValueError('unexpected Android clip metadata')
            if resource[ref['m_Offset']:ref['m_Offset']+len(clip)]==clip:raise ValueError('clip is already correct')
            needle=struct.pack('<Q',ref['m_Offset']);pos=raw.find(needle)
            if raw.count(needle)!=1:raise ValueError('ambiguous serialized clip offset')
            new_offset=len(resource);patched=bytearray(asset);start=obj.byte_start+pos
            patched[start:start+8]=struct.pack('<Q',new_offset)
            check=[o for o in UnityPy.load(bytes(patched)).objects if o.path_id==obj.path_id]
            if len(check)!=1 or check[0].read_typetree()['m_ExternalResources']!={'m_Source':'sharedassets1.resource','m_Offset':new_offset,'m_Size':len(clip)}:
                raise ValueError('patched clip metadata differs')
            repl={'assets/bin/Data/sharedassets1.resource':resource+clip}
            for i,name in enumerate(parts):repl[name]=bytes(patched[i*1048576:(i+1)*1048576])
            with zipfile.ZipFile(new_obb,'x',allowZip64=True) as z:
                for info in oz.infolist():z.writestr(copy.copy(info),repl[info.filename] if info.filename in repl else oz.read(info))
                z.comment=b'00000000'
            dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,OBB_SHA.encode())
            h=hashlib.sha256()
            with new_obb.open('rb') as f:
                remaining=new_obb.stat().st_size-8
                while remaining:
                    b=f.read(min(1048576,remaining))
                    if not b:raise ValueError('short OBB')
                    h.update(b);remaining-=len(b)
            for nonce in range(1000000):
                comment=f'{nonce:08x}'.encode();candidate=h.copy();candidate.update(comment);new_hash=candidate.hexdigest().encode()
                if previous<new_hash<following and new_hash!=OBB_SHA.encode():break
            else:raise ValueError('cannot preserve DEX string order')
            with new_obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
            if repair.sha(new_obb).encode()!=new_hash:raise ValueError('OBB hash mismatch')
            dex=bytearray(dex.replace(OBB_SHA.encode(),new_hash));dex[12:32]=hashlib.sha1(dex[32:]).digest();dex[8:12]=(zlib.adler32(dex[12:])&0xffffffff).to_bytes(4,'little')
            with zipfile.ZipFile(unsigned,'x',allowZip64=True) as z:
                for info in az.infolist():
                    if not repair.jar_signature(info.filename):z.writestr(copy.copy(info),bytes(dex) if info.filename=='classes.dex' else az.read(info))
        def run(args):subprocess.run(runner_prefix+list(map(str,args)),env=env,check=True)
        run([tools/'zipalign','-p','4',unsigned,aligned]);run([tools/'apksigner','sign','--ks',key,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned]);run([tools/'zipalign','-c','4',signed]);run([tools/'apksigner','verify','--verbose',signed])
        with zipfile.ZipFile(signed) as az,zipfile.ZipFile(new_obb) as oz:
            for z in (az,oz):repair.unique(z)
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new_hash)!=1:raise ValueError('output pair mismatch')
            data=oz.read('assets/bin/Data/sharedassets1.resource')
            if data[:new_offset]!=resource or data[new_offset:]!=clip:raise ValueError('resource reconstruction differs')
        report={'kind':'local diagnostic only, not for distribution','apk_sha256':repair.sha(signed),'obb_sha256':repair.sha(new_obb),'clip_sha256':CLIP_SHA,'old_offset':ref['m_Offset'],'new_offset':new_offset,'clip_bytes':len(clip),'limitations':['Other game and third-party data remain.','Experimental DEX patch and debug signature.','Video playback not established by archive checks.']}
        (t/'report.json').write_text(json.dumps(report,indent=2)+'\n');out.mkdir()
        try:
            for f in (signed,new_obb,t/'report.json'):shutil.copyfile(f,out/f.name)
        except BaseException:shutil.rmtree(out);raise
        print(json.dumps(report,indent=2))


if __name__=='__main__':main()
