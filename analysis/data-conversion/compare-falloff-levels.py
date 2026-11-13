#!/usr/bin/env python3
"""Compare each Falloff alpha mip in two Android OBBs and pinned Steam data.

Pixel statistics do not establish which mip is sampled or in-game appearance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile
import UnityPy
from UnityPy.export import Texture2DConverter as Converter

OLD='ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'
NEW='d47ca939431bfe4555c195155f29ea66303f24b848b2c90a8822d91d305c84ca'
PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
STREAM='43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea'


def digest(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def android(path):
    with zipfile.ZipFile(path) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entry')
        pre='assets/bin/Data/sharedassets1.assets.split';parts=sorted((n for n in z.namelist() if n.startswith(pre)),key=lambda n:int(n[len(pre):]))
        if not parts or [int(n[len(pre):]) for n in parts]!=list(range(len(parts))):raise ValueError('missing asset fragment')
        objects=[o for o in UnityPy.load(b''.join(z.read(n) for n in parts)).objects if o.path_id==827 and o.type.name=='Texture2D']
        if len(objects)!=1:raise ValueError('missing Android texture')
        return objects[0].read()


def levels(texture):
    if texture.m_Name!='Falloff' or (texture.m_Width,texture.m_Height,texture.m_MipCount)!=(256,8,9) or texture.m_TextureFormat not in (1,47):raise ValueError('unexpected texture metadata')
    data=texture.get_image_data();result=[];pos=0
    for level in range(texture.m_MipCount):
        w,h=max(1,texture.m_Width>>level),max(1,texture.m_Height>>level)
        size=w*h if texture.m_TextureFormat==1 else ((w+3)//4)*((h+3)//4)*16
        image=Converter.parse_image_data(data[pos:pos+size],w,h,texture.m_TextureFormat,texture.object_reader.version,texture.object_reader.platform).convert('RGBA')
        result.append(list(image.getchannel('A').getdata()));pos+=size
    if pos!=len(data):raise ValueError('unexpected mip payload length')
    return result


def compare(a,b):
    if len(a)!=len(b) or not a:raise ValueError('mip size mismatch')
    diff=[abs(x-y) for x,y in zip(a,b)]
    return {'mean_absolute':sum(diff)/len(diff),'maximum':max(diff),'first_mean':sum(a)/len(a),'second_mean':sum(b)/len(b)}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('old_obb',type=Path);p.add_argument('new_obb',type=Path);p.add_argument('pc_data',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    for path,h in ((a.old_obb,OLD),(a.new_obb,NEW),(a.pc_data/'sharedassets1.assets',PC),(a.pc_data/'sharedassets1.assets.resS',STREAM)):
        if not path.is_file() or digest(path)!=h:p.error('incompatible input: '+str(path))
    objs=[o for o in UnityPy.load(str(a.pc_data/'sharedassets1.assets')).objects if o.path_id==597 and o.type.name=='Texture2D']
    if len(objs)!=1:raise ValueError('missing Steam texture')
    original,rebuilt,steam=levels(android(a.old_obb)),levels(android(a.new_obb)),levels(objs[0].read())
    report={'old_obb_sha256':OLD,'new_obb_sha256':NEW,'steam_asset_sha256':PC,'levels':[],
            'limitations':'Texture alpha statistics only; no camera, projector, filtering, Unity shader, visual perception or device behavior is measured.'}
    for i,(old,new,pc) in enumerate(zip(original,rebuilt,steam)):
        row={'level':i,'width':max(1,256>>i),'height':max(1,8>>i),'old_vs_new':compare(old,new),'steam_vs_old':compare(pc,old),'steam_vs_new':compare(pc,new)}
        report['levels'].append(row);print(i,f"{row['width']}x{row['height']}",'old-new mean',round(row['old_vs_new']['mean_absolute'],3),'pc-new mean',round(row['steam_vs_new']['mean_absolute'],3))
    with a.output.open('x') as f:json.dump(report,f,indent=2);f.write('\n')


if __name__=='__main__':main()
