#!/usr/bin/env python3
"""Compact zero-filled audio entries in an unsigned local research base.

Rebuilt output must restore the audio entries to ZIP_STORED for Unity. Other
game content remains; this is not a release or a distributable package.
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
APK_SHA='1bab76352b72bddec06216ffbfc8d83b2ce00cf28387c7b1d817906641dc7406'
OBB_SHA='ca79d294f0082d4b3ceb41c7cfb42329a72f760bce1599fdc0c3cc6daf2a005b'
ASSET_SHA='294055b5e8c9edec427883c2d1ea73fddcd39b25e9f2ad37b8804a08b8924325'
HASHED_SHA='876d086b963679f1513532737859c5a79133d706f5e7f64643b0007ef53515df'
spec=importlib.util.spec_from_file_location('repair',Path(__file__).with_name('repair-split-guid-probe.py'));repair=importlib.util.module_from_spec(spec);spec.loader.exec_module(repair)


def require(p,h):
    if not p.is_file() or repair.sha(p)!=h:raise ValueError('incompatible input: '+str(p))


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('output',type=Path);p.add_argument('--blank-shared4-audio',action='store_true',help='also blank and compact the six sharedassets4 audio streams');p.add_argument('--shared4-mesh-template',type=Path,help='use verified blanked sharedassets4 mesh template');p.add_argument('--shared4-texture-template',type=Path,help='use verified blanked sharedassets4 mesh and texture template');p.add_argument('--shared4-particle-template',type=Path,help='use verified blanked sharedassets4 particle template');p.add_argument('--shared4-particle-reference-template',type=Path,help='use verified blanked particle bodies with preserved pointers');p.add_argument('--shared2-gamepad-template',type=Path,help='use verified blanked sharedassets2 controller texture template');p.add_argument('--shared2-animation-template',type=Path,help='use verified blanked sharedassets2 animation streams');p.add_argument('--shared0-font-metadata-template',type=Path,help='use verified blanked multilingual font metadata');a=p.parse_args()
    if a.shared4_mesh_template and not a.blank_shared4_audio:p.error('--shared4-mesh-template requires --blank-shared4-audio')
    if a.shared4_texture_template and not a.shared4_mesh_template:p.error('--shared4-texture-template requires --shared4-mesh-template')
    if a.shared4_particle_template and not a.shared4_texture_template:p.error('--shared4-particle-template requires --shared4-texture-template')
    if a.shared4_particle_reference_template and not a.shared4_particle_template:p.error('--shared4-particle-reference-template requires --shared4-particle-template')
    if a.shared2_gamepad_template and not a.shared4_particle_reference_template:p.error('--shared2-gamepad-template requires --shared4-particle-reference-template')
    if a.shared2_animation_template and not a.shared2_gamepad_template:p.error('--shared2-animation-template requires --shared2-gamepad-template')
    if a.shared0_font_metadata_template and not a.shared2_animation_template:p.error('--shared0-font-metadata-template requires --shared2-animation-template')
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out,src=a.output.resolve(),a.base.resolve()
    if out.is_relative_to(src) or src.is_relative_to(out):p.error('output must be separate from input')
    apk,obb,asset,hashed=(a.base/n for n in ('base.apk','base.obb','asset-manifest.json','hashed-manifest.json'))
    for path,h in ((apk,APK_SHA),(obb,OBB_SHA),(asset,ASSET_SHA),(hashed,HASHED_SHA)):require(path,h)
    inventory=json.loads(hashed.read_text())
    if inventory.get('version')!=1:raise ValueError('invalid hashed manifest')
    selected={name for name,item in inventory['entries'].items() if item['kind']=='hashed-audio-template' and name.endswith('.resource')}
    if len(selected)!=63:raise ValueError('unexpected hashed audio count')
    selected|={'assets/bin/Data/sharedassets1.resource','assets/bin/Data/sharedassets2.resource'}
    shared4='assets/bin/Data/sharedassets4.resource'
    if a.blank_shared4_audio:selected.add(shared4)
    mesh_asset=mesh_manifest=None
    if a.shared4_mesh_template:
        mesh_asset=a.shared4_mesh_template/'sharedassets4.assets';mesh_manifest=a.shared4_mesh_template/'manifest.json'
        require(mesh_asset,'d9461f6047e92558dfdc552da980f1c1086cea9feacb22d8cee2e7b9debc333e')
        require(mesh_manifest,'5599b039f9891bb624c077a8cfbad52b8dd383d47a87e1d1813b375f6a3b44ff')
    texture_manifest=None
    if a.shared4_texture_template:
        mesh_asset=a.shared4_texture_template/'sharedassets4.assets';texture_manifest=a.shared4_texture_template/'manifest.json'
        require(mesh_asset,'868c4bc827c0f21293c2252660ef9b51931e931018e1af44240c5b4594b4db09')
        require(texture_manifest,'08001488876c5f3c1faca6737507b5d72776ab5c4f743325e253dbf055428354')
    particle_manifest=None
    if a.shared4_particle_template:
        mesh_asset=a.shared4_particle_template/'sharedassets4.assets';particle_manifest=a.shared4_particle_template/'manifest.json'
        require(mesh_asset,'fb3896524cca9e8b137eb5d4c7c6f17eaca57d02185b8f1a31fcf83ab0fd99a0')
        require(particle_manifest,'16119d8d7dd6ba8f7bdce87ab4033875fa9106386c60a87e22eba042e9e1820f')
    particle_reference_manifest=None
    if a.shared4_particle_reference_template:
        mesh_asset=a.shared4_particle_reference_template/'sharedassets4.assets';particle_reference_manifest=a.shared4_particle_reference_template/'manifest.json'
        require(mesh_asset,'2b663dc3d4030228841601cc53ccc2202d6d91be599641e5ba1fa690ab291074')
        require(particle_reference_manifest,'97fe46c853bbbfdb1fd5f54cf6b281f3eeae1fb5c43db230e5c4455d6171983f')
    gamepad_asset=gamepad_manifest=None
    if a.shared2_gamepad_template:
        gamepad_asset=a.shared2_gamepad_template/'sharedassets2.assets';gamepad_manifest=a.shared2_gamepad_template/'manifest.json'
        require(gamepad_asset,'e4d95de778e37ec5422ea5394aaf8e028ad5a11e145ec40dc1d1fb7a83f78d62')
        require(gamepad_manifest,'6b6e7b3ffd146286760664d87a583c1271e9ad4058a00ded438eab151992a616')
    animation_manifest=None
    if a.shared2_animation_template:
        gamepad_asset=a.shared2_animation_template/'sharedassets2.assets';animation_manifest=a.shared2_animation_template/'manifest.json'
        require(gamepad_asset,'77d16b6174cf96eaf0565a5d555235de571de38dde7755096b09220e39f8d464')
        require(animation_manifest,'c260aa264809a9afe70937c6c10097dde70c13ec92a2d127cdfe9aa57d5bb20f')
    fonts_asset=fonts_manifest=None
    if a.shared0_font_metadata_template:
        fonts_asset=a.shared0_font_metadata_template/'sharedassets0.assets';fonts_manifest=a.shared0_font_metadata_template/'manifest.json'
        require(fonts_asset,'69ae7f8ccd1048553ac88cb0cfd86d599f1e772bc2b443c0263af5e1890ee844')
        require(fonts_manifest,'f3f899fbbad2b29534dfb6854b1f33552616112b2afc1569db2ec7280bf69c5c')
    with tempfile.TemporaryDirectory(prefix='.openbonk-compact-',dir=a.output.parent) as t:
        t=Path(t);new_apk=t/'base.apk';new_obb=t/'base.obb';stats=[]
        with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
            for z in (az,oz):
                if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate entries')
            if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('APK/OBB GUID mismatch')
            font_parts=[]
            if fonts_asset:
                prefix='assets/bin/Data/sharedassets0.assets.split';font_parts=sorted((n for n in az.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not font_parts or [int(n[len(prefix):]) for n in font_parts]!=list(range(len(font_parts))):raise ValueError('invalid sharedassets0 parts')
                if hashlib.sha256(b''.join(az.read(n) for n in font_parts)).hexdigest()!='009ad342794b4b5289efe4ab9e814de2374caf70248f05e17719d59d576cc80a':raise ValueError('unexpected sharedassets0 asset')
                font_bytes=fonts_asset.read_bytes()
                if len(font_bytes)!=sum(az.getinfo(n).file_size for n in font_parts):raise ValueError('invalid font template size')
            mesh_parts=['assets/bin/Data/sharedassets4.assets.split0','assets/bin/Data/sharedassets4.assets.split1']
            if mesh_asset:
                original=b''.join(oz.read(n) for n in mesh_parts)
                if hashlib.sha256(original).hexdigest()!='fd49162dc6d36ec83cf52f45338a2c871c061664987ae7d207512c2ab3c57aa0':raise ValueError('unexpected original sharedassets4 asset')
                mesh_bytes=mesh_asset.read_bytes()
            gamepad_parts=[]
            if gamepad_asset:
                prefix='assets/bin/Data/sharedassets2.assets.split'
                gamepad_parts=sorted((n for n in oz.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
                if not gamepad_parts or [int(n[len(prefix):]) for n in gamepad_parts]!=list(range(len(gamepad_parts))):raise ValueError('invalid sharedassets2 parts')
                if hashlib.sha256(b''.join(oz.read(n) for n in gamepad_parts)).hexdigest()!='dc0fa2e17c7607009ad45489f5995aac47cc81a021ae0ce46a0ecd4981bf3206':raise ValueError('unexpected sharedassets2 asset')
                gamepad_bytes=gamepad_asset.read_bytes()
                if not (len(gamepad_parts)-1)*1048576<len(gamepad_bytes)<=len(gamepad_parts)*1048576:raise ValueError('invalid sharedassets2 template size')
            dex=az.read('classes.dex');old=OBB_SHA.encode('ascii');previous,following=repair.dex_bounds(dex,old)
            with zipfile.ZipFile(new_obb,'x',allowZip64=True) as dest:
                for item in oz.infolist():
                    info=copy.copy(item);data=oz.read(item)
                    if mesh_asset and item.filename in mesh_parts:
                        n=mesh_parts.index(item.filename);data=mesh_bytes[n*1048576:(n+1)*1048576]
                    if gamepad_asset and item.filename in gamepad_parts:
                        n=gamepad_parts.index(item.filename);data=gamepad_bytes[n*1048576:(n+1)*1048576]
                    if item.filename in selected:
                        if item.filename==shared4:
                            if len(data)!=559680 or hashlib.sha256(data).hexdigest()!='517ed56f3ef48be5e4edc9726d8edd3e1dcb4dd77fcb11d9315db897f3f69daa':raise ValueError('unexpected sharedassets4 audio')
                            data=bytes(len(data))
                        if item.compress_type!=zipfile.ZIP_STORED or not data or any(data):raise ValueError('entry is not stored zero audio: '+item.filename)
                        info.compress_type=zipfile.ZIP_DEFLATED;info._compresslevel=9
                        stats.append({'entry':item.filename,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()})
                    dest.writestr(info,data)
                dest.comment=b'00000000'
            if len(stats)!=(66 if a.blank_shared4_audio else 65):raise ValueError('missing selected zero audio')
            h=hashlib.sha256()
            with new_obb.open('rb') as f:
                left=new_obb.stat().st_size-8
                while left:
                    block=f.read(min(1048576,left))
                    if not block:raise ValueError('short OBB')
                    h.update(block);left-=len(block)
            for n in range(1_000_000):
                comment=f'{n:08x}'.encode();v=h.copy();v.update(comment);new=v.hexdigest().encode('ascii')
                if previous<new<following and new!=old:break
            else:raise ValueError('could not preserve DEX string order')
            with new_obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
            if repair.sha(new_obb).encode()!=new:raise ValueError('OBB digest mismatch')
            patched=bytearray(dex.replace(old,new));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
            with zipfile.ZipFile(new_apk,'x',allowZip64=True) as dest:
                for info in az.infolist():
                    if repair.jar_signature(info.filename):continue
                    if info.filename=='classes.dex':data=bytes(patched)
                    elif fonts_asset and info.filename in font_parts:
                        n=font_parts.index(info.filename);data=font_bytes[n*1048576:(n+1)*1048576]
                    else:data=az.read(info)
                    dest.writestr(copy.copy(info),data)
        with zipfile.ZipFile(new_apk) as az,zipfile.ZipFile(new_obb) as oz:
            if az.testzip() is not None or oz.testzip() is not None or az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output validation failed')
            for item in stats:
                info=oz.getinfo(item['entry']);data=oz.read(info)
                if info.compress_type!=zipfile.ZIP_DEFLATED or len(data)!=item['bytes'] or hashlib.sha256(data).hexdigest()!=item['sha256']:raise ValueError('compacted audio mismatch')
            if mesh_asset and hashlib.sha256(b''.join(oz.read(n) for n in mesh_parts)).hexdigest()!=repair.sha(mesh_asset):raise ValueError('mesh template mismatch')
            if gamepad_asset and hashlib.sha256(b''.join(oz.read(n) for n in gamepad_parts)).hexdigest()!=repair.sha(gamepad_asset):raise ValueError('controller texture template mismatch')
            if fonts_asset and hashlib.sha256(b''.join(az.read(n) for n in font_parts)).hexdigest()!=repair.sha(fonts_asset):raise ValueError('font metadata template mismatch')
        report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(new_apk),'obb_sha256':repair.sha(new_obb),'asset_manifest_sha256':ASSET_SHA,'hashed_manifest_sha256':HASHED_SHA,'shared4_audio_blanked':a.blank_shared4_audio,'shared4_meshes_blanked':bool(mesh_asset),'shared4_textures_blanked':bool(texture_manifest),'shared4_particles_blanked':bool(particle_manifest),'shared4_particle_references_blanked':bool(particle_reference_manifest),'shared2_gamepad_blanked':bool(gamepad_manifest),'shared2_animation_streams_blanked':bool(animation_manifest),'shared0_font_metadata_blanked':bool(fonts_manifest),'original_obb_bytes':obb.stat().st_size,'compacted_obb_bytes':new_obb.stat().st_size,'compacted_audio':stats,'limits':['APK is unsigned.','Zero audio is compressed only in the base and must be restored as ZIP_STORED.','Other game data and code remain.']}
        (t/'report.json').write_text(json.dumps(report,indent=2)+'\n');a.output.mkdir()
        try:
            for f in (new_apk,new_obb,asset,hashed,t/'report.json'):shutil.copyfile(f,a.output/f.name)
            if mesh_manifest:shutil.copyfile(mesh_manifest,a.output/'shared4-mesh-manifest.json')
            if texture_manifest:shutil.copyfile(texture_manifest,a.output/'shared4-texture-manifest.json')
            if particle_manifest:shutil.copyfile(particle_manifest,a.output/'shared4-particle-manifest.json')
            if particle_reference_manifest:shutil.copyfile(particle_reference_manifest,a.output/'shared4-particle-reference-manifest.json')
            if gamepad_manifest:shutil.copyfile(gamepad_manifest,a.output/'shared2-gamepad-manifest.json')
            if animation_manifest:shutil.copyfile(animation_manifest,a.output/'shared2-animation-manifest.json')
            if fonts_manifest:shutil.copyfile(fonts_manifest,a.output/'shared0-font-metadata-manifest.json')
        except BaseException:shutil.rmtree(a.output);raise
        print('compacted',len(stats),'zero audio streams;',report['original_obb_bytes'],'->',report['compacted_obb_bytes'],'bytes')


if __name__=='__main__':main()
