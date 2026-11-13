#!/usr/bin/env python3
"""Build a local mixed animation manifest from pinned comparison evidence.

25 streams use Steam curves; remaining clips retain Android-derived patches.
The output is local research material and is not cleared for distribution.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

import UnityPy

PC='d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35'
OBB='ca3fa6b0e1d994339655331b76c23c2d4f94c2038bd6f87c825575ab28cdb919'
MANIFEST='d906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8'
INVENTORY='605aadce9d921ae7c95594e4f00c3f7536b9dd60970793521973b6f19b869a9e'
THRESHOLD=1e-6


def digest(b):return hashlib.sha256(b).hexdigest()


def sha(path):
    with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()


def stream(tree):return tree['m_MuscleClip']['m_Clip']['data']['m_StreamedClip']['data']


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('pc_data',type=Path);p.add_argument('obb',type=Path);p.add_argument('template_dir',type=Path);p.add_argument('inventory',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    pc=a.pc_data/'sharedassets1.assets';manifest=a.template_dir/'manifest.json'
    if sha(pc)!=PC or sha(a.obb)!=OBB or sha(manifest)!=MANIFEST or sha(a.inventory)!=INVENTORY:raise ValueError('incompatible input')
    old=json.loads(manifest.read_text());inventory=json.loads(a.inventory.read_text());by_file={r['file']:r for r in inventory['clips']}
    if len(old['animations'])!=108 or len(by_file)!=108:raise ValueError('unexpected inventory size')
    sources={o.path_id:o for o in UnityPy.load(str(pc)).objects};records=[];selected=0;removed=0
    with zipfile.ZipFile(a.obb) as z:
        if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate OBB entries')
        for original in old['animations']:
            rec=dict(original);filename=rec['file']
            if not re.fullmatch('[0-9a-f]{32}',filename) or filename not in by_file:raise ValueError('invalid animation ID')
            stats=by_file[filename];sample=stats.get('sampled_curves')
            eligible=not stats['non_stream_differences'] and sample is not None and sample['samples']>0 and sample['max_sampled_curve_difference']<=THRESHOLD
            if eligible:
                target=z.read('assets/bin/Data/'+filename);template=(a.template_dir/filename).read_bytes();source=sources[rec['source_id']]
                if digest(target)!=rec['original_sha256'] or digest(template)!=rec['template_sha256'] or source.type.name!='AnimationClip':raise ValueError('animation input mismatch')
                clip=source.get_raw_data();target_obj=UnityPy.load(target).objects[0];target_raw=target_obj.get_raw_data();words=stream(target_obj.read_typetree());block=struct.pack('<%dI'%len(words),*words);pos=target_raw.find(block)
                if (digest(clip)!=rec['source_sha256'] or len(clip)!=rec['size'] or len(target_raw)!=len(clip)
                        or target_raw.count(block)!=1 or pos<0 or target_raw[:pos]!=clip[:pos] or target_raw[pos+len(block):]!=clip[pos+len(block):]):raise ValueError('difference is not restricted to stream')
                start=rec['offset'];size=rec['size']
                if start<0 or start+size>len(template) or any(template[start:start+size]):raise ValueError('invalid template body')
                rebuilt=bytearray(template);rebuilt[start:start+size]=clip
                removed+=rec['patch_count'];rec.pop('patch_base64');rec['mode']='steam-stream';rec['converted_sha256']=digest(rebuilt)
                rec['max_sampled_curve_difference']=sample['max_sampled_curve_difference'];selected+=1
            else:rec['mode']='legacy-exact'
            records.append(rec)
    if selected!=25:raise ValueError('unexpected selected clip count')
    report={'version':1,'base_obb_sha256':old['base_obb_sha256'],'inventory_sha256':INVENTORY,'threshold':THRESHOLD,
            'selected_count':selected,'removed_patch_bytes':removed,'animations':records,
            'limitations':'25 clips have numerically different curves; 83 retain Android-derived patches. Not distributable.'}
    with a.output.open('x') as f:json.dump(report,f,separators=(',',':'))
    print('selected',selected,'clips; removed',removed,'patch bytes; hybrid manifest SHA256',sha(a.output),flush=True)


if __name__=='__main__':main()
