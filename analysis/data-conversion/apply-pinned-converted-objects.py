#!/usr/bin/env python3
"""Apply pinned reconstructed objects without opening the old full base.

The converted archive and new base still retain original game data. Matching
hashes and successful reconstruction do not establish distribution rights.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import UnityPy

ROOT=Path(__file__).resolve().parents[2]
REPORTS={'sharedassets0.assets':('no-rewired-shared0-transplant.json','65daa0be53d0058d178b05d458e9af5d5283ee4f4c7bfd7c7ee0651233fe2cfe',96), 'sharedassets1.assets':('no-rewired-shared1-safe-transplant.json','418ef5f586a86229c2d1ea6ab754532b187d32efe38975ec16998016a2d3bb5a',798), 'sharedassets2.assets':('no-rewired-shared2-safe-transplant.json','686dc6c8f588337c565b3c37eb05a9cb95dc5f91ef40eb12b9a1f6e28c07bdbd',189)}
WATER={'path_id':1072,'new_sha256':'b424b33fc84c27c2e01ab04901a7da6f8ea92910263f3fd419022571c00d3347','converted_sha256':'ffbc65a2e87cce915efcaf331020e80996567d45aefde3decfe785644b0dc83c'}
SCRUBBED_MANIFEST_SHA='c4cec041aec2aac138b0ddae6b62ca354701340dd8b0eae02f6938aff5e1a082'
EXPANDED_MANIFEST_SHA='294055b5e8c9edec427883c2d1ea73fddcd39b25e9f2ad37b8804a08b8924325'
EXPANDED_COUNTS={'sharedassets0.assets':103,'sharedassets1.assets':1407,'sharedassets2.assets':449}
GAMEPAD_REBUILT='9a6f385d633c6d2c2699a4edcc53b54699233951a6b2cc5588f90428bf876307'
GAMEPAD_OUTPUT='aa581f3af73a1b1e0cc20b0ba8a73705db19a474bc8153687c0ed36ef7941616'
ANIMATIONS_REBUILT='c6185443281f4489d85f3a545067a32b1a404bf1549aa1d150c469589762dc47'
ANIMATIONS_OUTPUT='a3bb71785a61a2acddb230e28b1b1f8466c076a944112e18273433a635faea37'
FONT_BLANK_INPUT='69ae7f8ccd1048553ac88cb0cfd86d599f1e772bc2b443c0263af5e1890ee844'
FONT_BLANK_OUTPUT='240dbd13bc37e6a67cb790942e42cd728c5aa502d48eb50cd0fdb0b4f426075e'
CONVERTED_FONT_BLANK='e92b4561faf7be547bef42a874067ba3d688947db0bfa0fbd966532792e2e7b1'
spec=importlib.util.spec_from_file_location('transplant',Path(__file__).with_name('transplant-matching-assets-prototype.py'));base=importlib.util.module_from_spec(spec);spec.loader.exec_module(base)


def digest(b):return hashlib.sha256(b).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('converted_archive',type=Path);p.add_argument('new_archive',type=Path);p.add_argument('asset',choices=REPORTS);p.add_argument('output',type=Path);p.add_argument('--report',type=Path,required=True);p.add_argument('--converted-is-file',action='store_true',help='read a raw rebuilt asset instead of an OBB');p.add_argument('--scrubbed-object-manifest',type=Path,help='use pinned blank-object inventory');p.add_argument('--experimental-gamepad-rebuilt',action='store_true',help='use the pinned raw sharedassets2 with rebuilt gamepad texture');p.add_argument('--experimental-animation-streams',action='store_true',help='use the pinned rebuilt animation-stream variant');p.add_argument('--experimental-fonts-restored-file',action='store_true',help='read the pinned sharedassets0 after restoring font metadata');p.add_argument('--experimental-fonts-blank-file',action='store_true',help='leave pinned font metadata blank for semantic restoration');p.add_argument('--experimental-converted-fonts-blank',action='store_true',help='accept converted sharedassets0 with font metadata blank')
    p.add_argument('--experimental-materials-restored-file',action='store_true',help='use raw sharedassets1 with material values restored')
    a=p.parse_args()
    if a.experimental_materials_restored_file and (a.asset!='sharedassets1.assets' or not a.scrubbed_object_manifest):p.error('--experimental-materials-restored-file requires scrubbed sharedassets1')
    if a.experimental_gamepad_rebuilt and (a.asset!='sharedassets2.assets' or not a.scrubbed_object_manifest):p.error('--experimental-gamepad-rebuilt requires scrubbed sharedassets2')
    if a.experimental_animation_streams and not a.experimental_gamepad_rebuilt:p.error('--experimental-animation-streams requires --experimental-gamepad-rebuilt')
    if a.experimental_fonts_restored_file and (a.asset!='sharedassets0.assets' or not a.scrubbed_object_manifest or a.experimental_gamepad_rebuilt):p.error('--experimental-fonts-restored-file requires scrubbed sharedassets0')
    if a.experimental_fonts_blank_file and (a.asset!='sharedassets0.assets' or not a.scrubbed_object_manifest or a.experimental_gamepad_rebuilt or a.experimental_fonts_restored_file):p.error('--experimental-fonts-blank-file requires scrubbed sharedassets0')
    if a.experimental_converted_fonts_blank and (not a.experimental_fonts_blank_file or not a.converted_is_file):p.error('--experimental-converted-fonts-blank requires blank fonts and converted file')
    if any(x.exists() or x.is_symlink() for x in (a.output,a.report)) or not a.output.parent.is_dir() or not a.report.parent.is_dir():p.error('output and report must be new')
    name,expected,count=REPORTS[a.asset];manifest=ROOT/'build/android'/name
    if not manifest.is_file() or digest(manifest.read_bytes())!=expected:raise ValueError('pinned object manifest mismatch')
    m=json.loads(manifest.read_text());rows=m['changed_objects']
    if m['asset']!=a.asset or len(rows)!=count or len({r['path_id'] for r in rows})!=count:raise ValueError('invalid object inventory')
    converted_data=a.converted_archive.read_bytes() if a.converted_is_file else base.read_asset(a.converted_archive,a.asset);new_data=a.new_archive.read_bytes() if a.experimental_materials_restored_file or a.experimental_gamepad_rebuilt or a.experimental_fonts_restored_file or a.experimental_fonts_blank_file else base.read_asset(a.new_archive,a.asset)
    scrubbed=None
    if a.scrubbed_object_manifest:
        if not a.scrubbed_object_manifest.is_file():raise ValueError('scrubbed object manifest mismatch')
        scrubbed_hash=digest(a.scrubbed_object_manifest.read_bytes())
        if scrubbed_hash not in (SCRUBBED_MANIFEST_SHA,EXPANDED_MANIFEST_SHA):raise ValueError('scrubbed object manifest mismatch')
        sm=json.loads(a.scrubbed_object_manifest.read_text())
        if sm.get('version')!=1:raise ValueError('invalid scrubbed object manifest')
        scrubbed=sm['assets'][a.asset]
    if digest(converted_data)!=(CONVERTED_FONT_BLANK if a.experimental_converted_fonts_blank else m['converted_sha256']) or digest(new_data)!=(FONT_BLANK_INPUT if a.experimental_fonts_blank_file else ANIMATIONS_REBUILT if a.experimental_animation_streams else GAMEPAD_REBUILT if a.experimental_gamepad_rebuilt else scrubbed['scrubbed_sha256'] if scrubbed else m['new_sha256']):raise ValueError('asset input hash mismatch')
    converted_env,new_env=UnityPy.load(converted_data),UnityPy.load(new_data);converted,new=base.objects(converted_env),base.objects(new_env)
    if converted.keys()!=new.keys():raise ValueError('asset object IDs differ')
    expected_objects={k:digest(v.get_raw_data()) for k,v in new.items()}
    if scrubbed:
        selected=scrubbed['objects'];expected_ids={r['path_id'] for r in rows}|(set(range(124,131)) if a.asset=='sharedassets0.assets' else set())
        selected_ids={r['path_id'] for r in selected};expected_count=EXPANDED_COUNTS[a.asset] if scrubbed_hash==EXPANDED_MANIFEST_SHA else len(expected_ids)
        if len(selected)!=expected_count or not expected_ids.issubset(selected_ids) or len(selected_ids)!=len(selected):raise ValueError('invalid scrubbed object set')
        if any(r['type'] not in ('Font','Texture2D','Mesh','AudioClip','Cubemap','TerrainData','AnimationClip') for r in selected):raise ValueError('unexpected scrubbed object type')
        for item in selected:
            i=item['path_id'];c=converted[i];n=new[i];cb,nb=c.get_raw_data(),n.get_raw_data()
            if c.type.name!=item['type'] or n.type.name!=item['type'] or n.byte_start!=item['offset'] or len(nb)!=item['size'] or any(nb) or digest(cb)!=item['converted_sha256']:raise ValueError('scrubbed object mismatch: '+str(i))
            n.set_raw_data(cb);expected_objects[i]=digest(cb)
        rows=[]
    for row in rows:
        i=row['path_id']; c=converted[i];n=new[i];cb,nb=c.get_raw_data(),n.get_raw_data()
        if c.type.name!=row['type'] or n.type.name!=row['type'] or digest(cb)!=row['converted_sha256']:raise ValueError('converted object mismatch: '+str(i))
        if a.asset=='sharedassets1.assets' and i==WATER['path_id']:
            if digest(nb)!=WATER['new_sha256'] or digest(cb)!=WATER['converted_sha256'] or c.type.name!='Texture2D':raise ValueError('water object mismatch')
            ct,nt=c.read(),n.read();fields=('m_Name','m_Width','m_Height','m_MipCount','m_ImageCount','m_ColorSpace','m_TextureFormat')
            if any(getattr(ct,k)!=getattr(nt,k) for k in fields):raise ValueError('water texture fields differ')
            cp,np=bytes(ct.image_data),bytes(nt.image_data)
            if not cp or len(cp)!=len(np) or cb.count(cp)!=1 or nb.count(np)!=1:raise ValueError('ambiguous water payload')
            x,y=cb.index(cp),nb.index(np)
            if cb[:x]!=nb[:y] or cb[x+len(cp):]!=nb[y+len(np):]:raise ValueError('water metadata differs')
            cb=nb[:y]+cp+nb[y+len(np):]
        elif digest(nb)!=row['original_sha256']:raise ValueError('new object mismatch: '+str(i))
        n.set_raw_data(cb);expected_objects[i]=digest(cb)
    data=new_env.file.save();verified=base.objects(UnityPy.load(data))
    if verified.keys()!=new.keys() or any(verified[k].type.name!=new[k].type.name or digest(verified[k].get_raw_data())!=expected_objects[k] for k in expected_objects):raise ValueError('post-save objects mismatch')
    if digest(data)!=(FONT_BLANK_OUTPUT if a.experimental_fonts_blank_file else ANIMATIONS_OUTPUT if a.experimental_animation_streams else GAMEPAD_OUTPUT if a.experimental_gamepad_rebuilt else m['output_sha256']):raise ValueError('reconstructed asset hash mismatch')
    result={'kind':'local diagnostic only, not for distribution','asset':a.asset,'manifest_sha256':expected,'output_sha256':digest(data),'changed_object_count':len(scrubbed['objects']) if scrubbed else count,'limitations':['Original game data and dependencies remain.','Hash equivalence does not establish provenance or rights.']}
    created=False;report_created=False
    try:
        with a.output.open('xb') as f:created=True;f.write(data)
        with a.report.open('x') as f:report_created=True;json.dump(result,f,indent=2);f.write('\n')
    except BaseException:
        if created:a.output.unlink(missing_ok=True)
        if report_created:a.report.unlink(missing_ok=True)
        raise
    print('applied',result['changed_object_count'],'pinned objects;',a.asset,'SHA-256',digest(data),flush=True)


if __name__=='__main__':main()
