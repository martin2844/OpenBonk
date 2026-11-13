#!/usr/bin/env python3
"""Private host prototype for restoring matching material bytes from Steam.

The binary plan contains positions and bit masks, not material byte values. It is
not an Android integration or a determination of provenance or rights.
"""
import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import re
import struct
import zipfile
import zlib

spec=importlib.util.spec_from_file_location('scene_audit',Path(__file__).with_name('audit-material-template-matches.py'))
audit=importlib.util.module_from_spec(spec);spec.loader.exec_module(audit)
SHARED=('sharedassets1.assets','sharedassets2.assets')


def sha(value): return hashlib.sha256(value).digest()


def source(scene, platform, linux_data, windows_zip):
    if platform == 'linux': return (linux_data/scene).read_bytes()
    with zipfile.ZipFile(windows_zip) as z:
        matches=[i for i in z.infolist() if i.filename == 'Megabonk_Data/'+scene]
        if len(matches)!=1: raise ValueError('missing source scene')
        return z.read(matches[0])


def split_data(path, raw, scene):
    entries=[]
    with zipfile.ZipFile(path) as z:
        for i in z.infolist():
            m=re.fullmatch(r'assets/bin/Data/'+re.escape(scene)+r'\.split(\d+)',i.filename)
            if not m:continue
            if i.compress_type!=8 or i.flag_bits!=0:raise ValueError('unsupported split')
            off=i.header_offset
            if raw[off:off+4]!=b'PK\x03\x04':raise ValueError('bad header')
            n,e=struct.unpack_from('<HH',raw,off+26);off+=30+n+e
            value=zlib.decompress(raw[off:off+i.compress_size],-15)
            if len(value)!=i.file_size:raise ValueError('bad split length')
            entries.append((int(m.group(1)),off,i.compress_size,value))
    entries.sort()
    if not entries or [x[0] for x in entries]!=list(range(len(entries))):raise ValueError('bad split inventory')
    return entries


def write_splits(base, entries, scene):
    cursor=0
    for _,off,reserved,old in entries:
        value=scene[cursor:cursor+len(old)];cursor+=len(old)
        if value==old:continue
        z=zlib.compressobj(6,zlib.DEFLATED,-15);compressed=z.compress(value)+z.flush()
        if len(compressed)>reserved:raise ValueError('compressed scene exceeds reservation')
        base[off:off+reserved]=compressed+bytes(reserved-len(compressed))
    if cursor!=len(scene):raise ValueError('invalid scene length')


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('mode',choices=('scrub','restore'));p.add_argument('base',type=Path);p.add_argument('linux_data',type=Path)
    p.add_argument('windows_zip',type=Path);p.add_argument('report_dir',type=Path);p.add_argument('output',type=Path);p.add_argument('plan',type=Path)
    p.add_argument('--platform',choices=('linux','windows'));a=p.parse_args()
    if a.output.exists() or a.output.is_symlink() or not a.output.parent.is_dir():p.error('output must be new')
    if a.mode=='scrub' and (a.plan.exists() or a.plan.is_symlink() or a.platform):p.error('invalid scrub args')
    if a.mode=='restore' and not a.platform:p.error('restore requires --platform')
    original='d1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431'
    names=SHARED
    base=bytearray(a.base.read_bytes());total=0;rows_total=0
    if a.mode=='scrub':
        if sha(base).hex()!=original:raise ValueError('unexpected template')
        plan=io.BytesIO();plan.write(b'OBMAT001'+sha(base)+bytes(32))
    else:
        plan=io.BytesIO(a.plan.read_bytes())
        if plan.read(8)!=b'OBMAT001' or plan.read(32).hex()!=original or plan.read(32)!=sha(base):raise ValueError('plan or template differs')
    for scene in names:
        entries=split_data(a.base,base,scene);content=bytearray(b''.join(e[3] for e in entries))
        if a.mode=='scrub':
            report_name = 'material-shared1-audit.json' if scene=='sharedassets1.assets' else 'material-shared2-audit.json'
            report=json.loads((a.report_dir/report_name).read_text())
            left=source(scene,'linux',a.linux_data,a.windows_zip);right=source(scene,'windows',a.linux_data,a.windows_zip)
            if report['base_sha256']!=original or sha(left).hex()!=report['linux_sha256'] or sha(right).hex()!=report['windows_sha256']:
                raise ValueError('report inputs differ')
            lo={x[0]:x for v in audit.material_objects(left)[0].values() for x in v}
            wo={x[0]:x for v in audit.material_objects(right)[0].values() for x in v}
            selected=[r for r in report['objects'] if r['equal_nonzero']>0]
            plan.write(struct.pack('>I',len(selected))+sha(left)+sha(right)); used=[]
            for r in selected:
                pos,size=r['target_offset'],r['size'];l=lo[r['linux_id']];w=wo[r['windows_id']]
                if pos<0 or size<=0 or pos+size>len(content) or len(l[2])!=size or len(w[2])!=size:raise ValueError('invalid object range')
                mask=bytearray((size+7)//8);count=0
                for i,(x,y,z) in enumerate(zip(content[pos:pos+size],l[2],w[2])):
                    if x and x==y==z:mask[i//8]|=1<<(i%8);content[pos+i]=0;count+=1
                if count!=r['equal_nonzero']:raise ValueError('audit row differs')
                used.append((pos,pos+size));plan.write(struct.pack('>IIII',pos,l[1],w[1],size)+mask)
                total+=count;rows_total+=1
            used.sort()
            if any(x[1]>y[0] for x,y in zip(used,used[1:])):raise ValueError('overlapping objects')
        else:
            header=plan.read(68)
            if len(header)!=68:raise ValueError('truncated plan')
            count=struct.unpack_from('>I',header)[0];value=source(scene,a.platform,a.linux_data,a.windows_zip)
            if count>20000 or sha(value)!=(header[4:36] if a.platform=='linux' else header[36:68]):raise ValueError('source or record count differs')
            for _ in range(count):
                rec=plan.read(16)
                if len(rec)!=16:raise ValueError('truncated record')
                pos,l,w,size=struct.unpack('>IIII',rec);off=l if a.platform=='linux' else w
                if not size or size>1048576 or pos+size>len(content) or off+size>len(value):raise ValueError('bad record bounds')
                mask=plan.read((size+7)//8)
                if len(mask)!=(size+7)//8:raise ValueError('truncated mask')
                for i in range(size):
                    if mask[i//8] & (1<<(i%8)):
                        if content[pos+i] or not value[off+i]:raise ValueError('invalid scene byte')
                        content[pos+i]=value[off+i];total+=1
                rows_total+=1
        write_splits(base,entries,content)
    if a.mode=='scrub':
        value=bytearray(plan.getvalue());value[40:72]=sha(base)
        with a.plan.open('xb') as f:f.write(value)
    elif plan.read(1) or sha(base).hex()!=original:raise ValueError('restored scene template differs')
    with a.output.open('xb') as f:f.write(base)
    print(a.mode,rows_total,'material objects,',total,'nonzero bytes; template',sha(base).hex())


if __name__=='__main__':main()
