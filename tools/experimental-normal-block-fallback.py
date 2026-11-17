#!/usr/bin/env python3
"""Rebuild individually bounded normal-map blocks from local Steam textures.

Only three known ETC2 RGB normal maps are considered. Each substituted 4x4
block must keep every decoded normal direction within the existing 10-degree
bound; blocks that fail remain unchanged. The result is experimental, is not
pixel-identical, and must stay private until the whole pipeline is validated.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
spec = importlib.util.spec_from_file_location('partial', ROOT / 'tools/experimental-partial-rgba-plan.py')
partial = importlib.util.module_from_spec(spec); spec.loader.exec_module(partial)
TARGETS = {'t_chest_normal': (256, 256, 9), 'FBX_Log0_Color_Normal': (128, 128, 8), 'BossPortalNormal': (256, 256, 9)}


def blend_mip(old, fast, quality, original_image, fast_image, quality_image, acceptable, block_bytes=8):
    """Keep unsafe blocks; select a safe encoding with the most original bytes."""
    width, height = original_image.size
    expected = ((width+3)//4)*((height+3)//4)*block_bytes
    if block_bytes not in (8,16) or width < 1 or height < 1 or any(len(x) != expected for x in (old,fast,quality)) or fast_image.size != (width,height) or quality_image.size != (width,height):
        raise ValueError('invalid normal block mip')
    result=bytearray(old); changed=safe=unchanged=0; pos=0
    for y in range(0,height,4):
        for x in range(0,width,4):
            box=(x,y,min(x+4,width),min(y+4,height)); reference=original_image.crop(box)
            candidates=[]
            for payload,image in ((quality,quality_image),(fast,fast_image)):
                block=payload[pos:pos+block_bytes]
                if block == old[pos:pos+block_bytes] or acceptable(reference,image.crop(box)):
                    candidates.append((sum(a==b for a,b in zip(old[pos:pos+block_bytes],block)),block))
            if candidates:
                chosen=max(candidates,key=lambda item:item[0])[1];safe+=1
                changed += sum(a!=b for a,b in zip(old[pos:pos+block_bytes],chosen))
                result[pos:pos+block_bytes]=chosen
            else: unchanged+=1
            pos+=block_bytes
    return bytes(result),safe,unchanged,changed


def generate(a):
    import UnityPy
    from UnityPy.export import Texture2DConverter as converter
    steam=a.steam_data.resolve(strict=True);obb=a.obb.resolve(strict=True)
    output=partial.masker.regular_output(a.output,(steam,obb));encoder=partial.study.load_quality_encoder(a.quality_library)
    index,_=partial.sources(steam);changes={};records=[];seen=set()
    with zipfile.ZipFile(obb) as archive:
        infos=partial.masker.zip_entries(archive);groups=partial.objects.asset_groups(archive)
        for group in ('sharedassets1.assets','sharedassets2.assets'):
            parts=groups[group];raw=b''.join(archive.read(part) for part in parts);modified=bytearray(raw)
            for obj in UnityPy.load(raw).objects:
                if obj.type.name!='Texture2D':continue
                target=obj.read();name=target.m_Name
                if name not in TARGETS:continue
                if name in seen or (target.m_Width,target.m_Height,target.m_MipCount,target.m_ImageCount,int(target.m_TextureFormat)) != (*TARGETS[name],1,45):
                    raise ValueError('normal target duplicated or metadata differs')
                seen.add(name)
                item=partial.study.select_source(target,index[(name,target.m_Width,target.m_Height)],texture=lambda x:x[2])
                if item is None or int(item[2].m_TextureFormat)!=10:raise ValueError('normal source missing or ambiguous')
                source=item[2];old=bytes(target.image_data);body=obj.get_raw_data()
                if not old or body.count(old)!=1:raise ValueError('normal payload ambiguous')
                fast=partial.converted(source,target.m_Width,target.m_Height,target.m_MipCount,45,encoder,True)
                quality=partial.converted(source,target.m_Width,target.m_Height,target.m_MipCount,45,encoder)
                levels=[partial.study.mip_slices(data,45,target.m_Width,target.m_Height,target.m_MipCount) for data in (old,fast,quality)]
                chunks=[];safe=unsafe=changed=0
                for base,fast_mip,quality_mip in zip(*levels):
                    w,h,old_data=base; images=[converter.parse_image_data(item[2],w,h,45,target.object_reader.version,target.object_reader.platform).convert('RGB') for item in (base,fast_mip,quality_mip)]
                    block,s,u,c=blend_mip(old_data,fast_mip[2],quality_mip[2],*images,partial.study.acceptable_normal_image)
                    chunks.append(block);safe+=s;unsafe+=u;changed+=c
                replacement=b''.join(chunks);offset=obj.byte_start+body.index(old)
                if len(replacement)!=len(old) or raw[offset:offset+len(old)]!=old or modified[offset:offset+len(old)]!=old:raise ValueError('normal range differs or overlaps')
                modified[offset:offset+len(old)]=replacement
                records.append({'name':name,'path_id':obj.path_id,'safe_blocks':safe,'unchanged_unsafe_blocks':unsafe,'changed_bytes':changed})
            offset=0
            for part in parts:
                changes[part.filename]=bytes(modified[offset:offset+part.file_size]);offset+=part.file_size
        if seen!=set(TARGETS):raise ValueError('expected normal textures missing')
        output.mkdir(mode=0o700)
        try:
            with zipfile.ZipFile(output/'converted.obb','x') as dest:
                for info in infos:
                    if info.filename in changes:dest.writestr(info,changes[info.filename])
                    else:
                        with archive.open(info) as inp,dest.open(info,'w') as out:shutil.copyfileobj(inp,out,1024*1024)
            (output/'report.json').write_text(json.dumps({'schema':1,'max_normal_angle_degrees':10,'textures':records},indent=2)+'\n')
        except BaseException:
            shutil.rmtree(output);raise
    print('Bounded normal blocks:',sum(x['safe_blocks'] for x in records),'unsafe blocks preserved:',sum(x['unchanged_unsafe_blocks'] for x in records),'output remains private.')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--steam-data',required=True,type=Path);p.add_argument('--obb',required=True,type=Path)
    p.add_argument('--quality-library',required=True,type=Path);p.add_argument('--output',required=True,type=Path)
    generate(p.parse_args())

if __name__=='__main__':main()
