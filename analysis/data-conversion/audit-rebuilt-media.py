#!/usr/bin/env python3
"""Audit serialized audio/video resource bounds in a local Android APK/OBB.

FSB/WebM headers and sizes are structural evidence, not proof that every sound
is correct or audible. This diagnostic does not authorize redistribution.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

import UnityPy

ROOT = 'assets/bin/Data/'


def sha_stream(f):
    h = hashlib.sha256()
    for b in iter(lambda: f.read(1048576), b''): h.update(b)
    return h.hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('apk', type=Path);p.add_argument('obb', type=Path);p.add_argument('output', type=Path)
    a = p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    rows=[];inventory=[]
    with zipfile.ZipFile(a.apk) as az,zipfile.ZipFile(a.obb) as oz:
        for z in (az,oz):
            if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entries')
        groups=defaultdict(list);single=[]
        for label,z in (('apk',az),('obb',oz)):
            for n in z.namelist():
                if not n.startswith(ROOT):continue
                m=re.fullmatch(r'(.+)\.split(\d+)',n)
                if m:groups[(label,m.group(1))].append((int(m.group(2)),n))
                elif re.fullmatch(r'assets/bin/Data/(?:[0-9a-f]{32}|level\d+|sharedassets\d+\.assets|globalgamemanagers(?:\.assets)?)',n):single.append((label,n))
        units=[]
        for (label,name),parts in sorted(groups.items()):
            parts.sort()
            if [i for i,_ in parts]!=list(range(len(parts))):raise ValueError('missing asset fragment: '+name)
            units.append((label,name,[n for _,n in parts]))
        units += [(label,name,[name]) for label,name in sorted(single)]
        for label,name,parts in units:
            z=az if label=='apk' else oz
            data=b''.join(z.read(n) for n in parts)
            try:objects=UnityPy.load(data).objects
            except Exception as e:raise ValueError('cannot inspect serialized file: '+name) from e
            inventory.append({'archive':label,'name':name,'objects':len(objects)})
            for obj in objects:
                if obj.type.name not in ('AudioClip','VideoClip'):continue
                tree=obj.read_typetree();ref=tree['m_Resource' if obj.type.name=='AudioClip' else 'm_ExternalResources']
                source=ref['m_Source'].replace('\\','/').rsplit('/',1)[-1];offset=ref['m_Offset'];size=ref['m_Size']
                entry=ROOT+source;row={'asset':name,'path_id':obj.path_id,'type':obj.type.name,'name':tree.get('m_Name',''),
                                      'source':source,'offset':offset,'size':size,'status':'unknown'}
                if not source or offset<0 or size<=0:
                    row['status']='unsupported_reference'
                else:
                    sources=[z for z in (az,oz) if entry in z.namelist()]
                    if len(sources)!=1:row['status']='missing_or_ambiguous_source'
                    else:
                        source_zip=sources[0];info=source_zip.getinfo(entry);row['zip_storage']='stored' if info.compress_type==zipfile.ZIP_STORED else 'compressed'
                        if offset+size>info.file_size:row['status']='out_of_bounds'
                        else:
                            with source_zip.open(info) as f:f.seek(offset);payload=f.read(size)
                            row['payload_sha256']=hashlib.sha256(payload).hexdigest()
                            if len(payload)!=size:row['status']='short_read'
                            elif obj.type.name=='VideoClip':row['status']='webm_header' if payload.startswith(b'\x1a\x45\xdf\xa3') else 'invalid_video_header'
                            elif len(payload)<60 or payload[:4]!=b'FSB5':row['status']='invalid_fsb_header'
                            else:
                                version,count,header,names_size,data_size,mode=struct.unpack_from('<6I',payload,4)
                                row['fsb_version']=version;row['fsb_samples']=count;row['fsb_mode']=mode
                                row['status']='fsb5_structural' if version==1 and count>0 and 60+header+names_size+data_size==size else 'invalid_fsb_lengths'
                rows.append(row)
    with a.apk.open('rb') as f:apk_sha=sha_stream(f)
    with a.obb.open('rb') as f:obb_sha=sha_stream(f)
    counts=Counter((r['type'],r['status']) for r in rows)
    report={'apk_sha256':apk_sha,'obb_sha256':obb_sha,'serialized_files_inspected':len(inventory),
            'counts':{f'{k[0]}:{k[1]}':v for k,v in sorted(counts.items())},'media':rows,
            'limitations':'Inspects named/split serialized assets and hexadecimal assets in the APK/OBB only. Header, bounds and FSB length checks do not establish correct audio, decoding, playback, visual output, or rights.'}
    with a.output.open('x',encoding='utf-8') as f:json.dump(report,f,indent=2);f.write('\n')
    print('inspected',len(inventory),'serialized assets;',dict(counts),'report',a.output)


if __name__=='__main__':main()
