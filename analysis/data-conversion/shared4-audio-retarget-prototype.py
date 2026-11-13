#!/usr/bin/env python3
"""Retarget six sharedassets4 audio clips to local pinned Steam streams.

Local research only: output retains game data and is not a release package.
Matching metadata and FSB headers do not establish audible equivalence.
"""
import argparse
import collections
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile
import UnityPy

PC_ASSET='cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3'
WINDOWS_ASSET='896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9'
PC_AUDIO='aa7cba281dd84a509bc1c0f200fa69cb21640615d92f16a65e9959db69a3b78b'
ANDROID_OBB='ca2280993b055ff7ad2172169ba100c895e69781e67ad4af53b71b027a9159af'
SCRUBBED_OBB='ca28198dfbb8d67eaa6e066fada95f1c4cc2bb8658dbf9dd69b3de597c868de8'
MESH_SCRUBBED_OBB='ca2fd174f7a0eceebef4d5df0c4bc8031f6addf2be9895b3164525dcbf251041'
MESH_TEMPLATE='d9461f6047e92558dfdc552da980f1c1086cea9feacb22d8cee2e7b9debc333e'
MESH_RESTORED='521dd2cbe9a32ef45227ff38e3d303c437146e6ee1d6426707210de8aed230f6'
TEXTURE_SCRUBBED_OBB='ca1257a62b412bddb02cb60a7c4b03397d2616b3a68650a6aa3b56e9ccebefad'
TEXTURE_TEMPLATE='868c4bc827c0f21293c2252660ef9b51931e931018e1af44240c5b4594b4db09'
TEXTURE_RESTORED='153369b4099a487db5f7a294323c9b58b0a8c113064acffb897ae2ef89f5a3c7'
PARTICLE_SCRUBBED_OBB='ca2cecc18fe83e50eae46c56ef4dc4e712b3ce89313e7bd91409075f36b913f6'
PARTICLE_TEMPLATE='fb3896524cca9e8b137eb5d4c7c6f17eaca57d02185b8f1a31fcf83ab0fd99a0'
PARTICLE_REFERENCE_OBB='cab3394d867cd28e30e5a4528ea8d64b9eb3835bcd2ff82dcb3f77267d06b0dd'
PARTICLE_REFERENCE_TEMPLATE='2b663dc3d4030228841601cc53ccc2202d6d91be599641e5ba1fa690ab291074'
GAMEPAD_SCRUBBED_OBB='ca5a34fe3c0d84af23072913478413546034ced84a728f653c570e252fe590c6'
ANIMATION_SCRUBBED_OBB='ca99de9c1fb7a0d917a99cd2fc4fe0f421a1e44b4b7e2eef395551e8cbe3110d'
PRELOAD_PARTICLE_OBB='ca1b8fc1992bc18a641cf6ce899cd9caefc2108d328731cf49b2d98491a42567'
PRELOAD_OBJECTS_OBB='caa5e5b2809e0f5e0db659ef7589d7fffad764f9b6d64022097067b5a647ab05'
PRELOAD_EXTRA_OBB='ca41168e07591229b061a7dfa8c46d70e56a455e4de1e57d5dd00425d09ae43d'
TRANSFORM_BASE_OBB='ca12334ed5b939c61c01914e0caf3180087db6926b4feab82648caaf4f01e9bd'
GAMEOBJECT_BASE_OBB='ca049117478a740c11cb4f6c105e1f3aa7ebc82dcbc4281b84b9c9b317d6e318'
MATERIAL_BASE_OBB='caba193e78eef5c4b2b4d4e5dd066306584ef151832e5f4c9f9d1ecfb4ca64fb'
ANDROID_ASSET='fd49162dc6d36ec83cf52f45338a2c871c061664987ae7d207512c2ab3c57aa0'
NAMES={'wosh_big_airy2','PylonHover','MeteorFalling','MeteorImpact','PylonsSpawn','Ambience'}
ATTRS=('m_Length','m_Channels','m_Frequency','m_BitsPerSample','m_LoadType','m_CompressionFormat','m_SubsoundIndex')
spec=importlib.util.spec_from_file_location('compare',Path(__file__).with_name('compare-android-asset-objects.py'));compare=importlib.util.module_from_spec(spec);spec.loader.exec_module(compare)


def digest(b):return hashlib.sha256(b).hexdigest()
def sha(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1048576),b''):h.update(b)
    return h.hexdigest()
def require(p,h):
    if not p.is_file() or sha(p)!=h:raise ValueError('incompatible input: '+str(p))
def objects(env):
    result={}
    for o in env.objects:
        if o.path_id in result:raise ValueError('duplicate object ID')
        result[o.path_id]=o
    return result


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('android_obb',type=Path);p.add_argument('output',type=Path);p.add_argument('--restored-asset',type=Path);p.add_argument('--windows-source',action='store_true');a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    out=a.output.resolve();pc=a.pc_data.resolve();obb=a.android_obb.resolve()
    if out.is_relative_to(pc) or pc.is_relative_to(out) or out.is_relative_to(obb) or obb.is_relative_to(out):p.error('output must be separate from inputs')
    require(pc/'sharedassets4.assets',WINDOWS_ASSET if a.windows_source else PC_ASSET);require(pc/'sharedassets4.resource',PC_AUDIO)
    if not obb.is_file() or sha(obb) not in (ANDROID_OBB,SCRUBBED_OBB,MESH_SCRUBBED_OBB,TEXTURE_SCRUBBED_OBB,PARTICLE_SCRUBBED_OBB,PARTICLE_REFERENCE_OBB,GAMEPAD_SCRUBBED_OBB,ANIMATION_SCRUBBED_OBB,PRELOAD_PARTICLE_OBB,PRELOAD_OBJECTS_OBB,PRELOAD_EXTRA_OBB,TRANSFORM_BASE_OBB,GAMEOBJECT_BASE_OBB,MATERIAL_BASE_OBB):raise ValueError('incompatible input: '+str(obb))
    with zipfile.ZipFile(obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        original=compare.load_archive(z,'sharedassets4.assets')
        if sha(obb) in (MESH_SCRUBBED_OBB,TEXTURE_SCRUBBED_OBB,PARTICLE_SCRUBBED_OBB,PARTICLE_REFERENCE_OBB,GAMEPAD_SCRUBBED_OBB,ANIMATION_SCRUBBED_OBB,PRELOAD_PARTICLE_OBB,PRELOAD_OBJECTS_OBB,PRELOAD_EXTRA_OBB,TRANSFORM_BASE_OBB,GAMEOBJECT_BASE_OBB,MATERIAL_BASE_OBB):
            texture_mode=sha(obb) in (TEXTURE_SCRUBBED_OBB,PARTICLE_SCRUBBED_OBB,PARTICLE_REFERENCE_OBB,GAMEPAD_SCRUBBED_OBB,ANIMATION_SCRUBBED_OBB,PRELOAD_PARTICLE_OBB,PRELOAD_OBJECTS_OBB,PRELOAD_EXTRA_OBB,TRANSFORM_BASE_OBB,GAMEOBJECT_BASE_OBB,MATERIAL_BASE_OBB)
            expected_template=PARTICLE_REFERENCE_TEMPLATE if sha(obb) in (PARTICLE_REFERENCE_OBB,GAMEPAD_SCRUBBED_OBB,ANIMATION_SCRUBBED_OBB,PRELOAD_PARTICLE_OBB,PRELOAD_OBJECTS_OBB,PRELOAD_EXTRA_OBB,TRANSFORM_BASE_OBB,GAMEOBJECT_BASE_OBB,MATERIAL_BASE_OBB) else PARTICLE_TEMPLATE if sha(obb)==PARTICLE_SCRUBBED_OBB else TEXTURE_TEMPLATE if texture_mode else MESH_TEMPLATE
            if digest(original)!=expected_template or not a.restored_asset:raise ValueError('missing or incompatible restored mesh asset')
            require(a.restored_asset,TEXTURE_RESTORED if texture_mode else MESH_RESTORED);original=a.restored_asset.read_bytes()
        elif a.restored_asset or digest(original)!=ANDROID_ASSET:raise ValueError('sharedassets4 asset mismatch')
    source_env=UnityPy.load(str(pc/'sharedassets4.assets'));target_env=UnityPy.load(original);target=objects(target_env);sources=collections.defaultdict(list)
    for obj in source_env.objects:
        if obj.type.name=='AudioClip':
            clip=obj.read();sources[clip.m_Name].append((obj.path_id,clip))
    targets=[]
    for obj in target.values():
        if obj.type.name!='AudioClip':continue
        dst=obj.read();matches=sources[dst.m_Name]
        if len(matches)!=1 or dst.m_Name not in NAMES:raise ValueError('missing or ambiguous clip: '+dst.m_Name)
        sid,src=matches[0]
        if any(getattr(src,k,None)!=getattr(dst,k,None) for k in ATTRS):raise ValueError('audio metadata mismatch: '+dst.m_Name)
        r=src.m_Resource
        if r.m_Source!='sharedassets4.resource' or r.m_Size<=0 or r.m_Offset<0 or r.m_Offset+r.m_Size>(pc/'sharedassets4.resource').stat().st_size:raise ValueError('invalid source stream: '+dst.m_Name)
        targets.append((obj.path_id,dst,sid,r.m_Offset,r.m_Size))
    if len(targets)!=6 or {d.m_Name for _,d,_,_,_ in targets}!=NAMES:raise ValueError('unexpected clip inventory')
    untouched={i:(o.type.name,digest(o.get_raw_data())) for i,o in target.items() if o.type.name!='AudioClip'}
    records=[];a.output.mkdir()
    try:
        with (pc/'sharedassets4.resource').open('rb') as src,(a.output/'sharedassets4.resource').open('xb') as dest:
            for tid,dst,sid,offset,size in targets:
                dest.write(bytes(-dest.tell()%32));new_offset=dest.tell();src.seek(offset);data=src.read(size)
                if len(data)!=size or not data.startswith(b'FSB5'):raise ValueError('invalid FSB stream: '+dst.m_Name)
                dest.write(data);dst.m_Resource.m_Offset=new_offset;dst.m_Resource.m_Size=size;dst.m_Resource.m_Source='sharedassets4.resource';dst.save()
                records.append({'name':dst.m_Name,'target_id':tid,'source_id':sid,'source_offset':offset,'size':size,'output_offset':new_offset,'sha256':digest(data)})
        asset=a.output/'sharedassets4.assets';asset.write_bytes(target_env.file.save());check=objects(UnityPy.load(str(asset)))
        if set(check)!=set(target) or any(check[i].type.name!=typ or digest(check[i].get_raw_data())!=h for i,(typ,h) in untouched.items()):raise ValueError('unrelated object changed')
        stream=a.output/'sharedassets4.resource'
        with stream.open('rb') as f:
            for rec in records:
                c=check[rec['target_id']].read();r=c.m_Resource
                if c.m_Name!=rec['name'] or r.m_Source!='sharedassets4.resource' or r.m_Offset!=rec['output_offset'] or r.m_Size!=rec['size']:raise ValueError('saved audio reference mismatch')
                f.seek(r.m_Offset)
                if digest(f.read(r.m_Size))!=rec['sha256']:raise ValueError('saved stream mismatch')
        report={'kind':'local diagnostic only, not for distribution','input_asset_sha256':digest(original),'output_asset_sha256':sha(asset),'output_resource_sha256':sha(stream),'clips':records,'unchanged_other_objects':len(untouched),'limitations':['Other original assets and code remain.','FSB metadata and hashes do not prove audible equivalence or rights.','No full-game or Retroid playback test.']}
        (a.output/'report.json').write_text(json.dumps(report,indent=2)+'\n')
        print('retargeted',len(records),'clips; unchanged other objects',len(untouched),'asset',report['output_asset_sha256'],'resource',report['output_resource_sha256'],flush=True)
    except BaseException:
        shutil.rmtree(a.output);raise


if __name__=='__main__':main()
