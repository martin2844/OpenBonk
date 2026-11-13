#!/usr/bin/env python3
"""Apply a pinned position-only scene plan to the private launcher base."""
import hashlib
import io
from pathlib import Path
import struct
import sys
import zlib

ORIGINAL='9060bf045d4da71d6a1663459361dc38c811a186683e3c92a482200163e41d79'
RESULT='6212c14a17747e1abd5fb34cbf6ea2ee13c1c766f4ece5f74d1657d1be5b260d'
PLAN='a35bf01205a78f486bb3f062e83f8649ca975aa0ef43acceda1047554b8d7317'
PARTS=(((1039009,111229,1048576),(1150333,50918,660176)),
       ((1201346,158494,1048576),(1359935,12733,1048576),(1372763,50172,1048576),(1423030,20428,191088)),
       ((1445073,91017,1048576),(1536185,17413,1048576),(1553693,45080,517688)))
def sha(x):return hashlib.sha256(x).hexdigest()
if len(sys.argv)!=4:raise ValueError('usage: SCRIPT BASE PLAN OUTPUT')
src,plan_path,dest=map(Path,sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir():raise ValueError('output must be new')
base=bytearray(src.read_bytes());plan_data=plan_path.read_bytes()
if sha(base)!=ORIGINAL or sha(plan_data)!=PLAN:raise ValueError('input differs')
plan=io.BytesIO(plan_data)
if plan.read(8)!=b'OBSCN001' or plan.read(32).hex()!=ORIGINAL or plan.read(32).hex()!=RESULT:raise ValueError('invalid plan header')
rows=changed=0
for parts in PARTS:
    content=bytearray()
    for off,packed,size in parts:
        value=zlib.decompress(base[off:off+packed],-15)
        if len(value)!=size:raise ValueError('invalid split')
        content.extend(value)
    header=plan.read(68)
    if len(header)!=68:raise ValueError('truncated plan')
    count=struct.unpack_from('>I',header)[0]
    if count>20000:raise ValueError('invalid record count')
    ranges=[]
    for _ in range(count):
        record=plan.read(16)
        if len(record)!=16:raise ValueError('truncated record')
        pos,_,_,size=struct.unpack('>IIII',record)
        if not size or size>1048576 or pos+size>len(content):raise ValueError('invalid scene range')
        bits=plan.read((size+7)//8)
        if len(bits)!=(size+7)//8:raise ValueError('truncated mask')
        ranges.append((pos,pos+size))
        for i in range(size):
            if bits[i//8] & (1<<(i%8)):
                if not content[pos+i]:raise ValueError('scene byte unexpectedly zero')
                content[pos+i]=0;changed+=1
        rows+=1
    ranges.sort()
    if any(x[1]>y[0] for x,y in zip(ranges,ranges[1:])):raise ValueError('overlapping scene objects')
    cursor=0
    for off,reserved,size in parts:
        value=content[cursor:cursor+size];cursor+=size
        z=zlib.compressobj(6,zlib.DEFLATED,-15);encoded=z.compress(value)+z.flush()
        if len(encoded)>reserved:raise ValueError('compressed split too large')
        base[off:off+reserved]=encoded+bytes(reserved-len(encoded))
if plan.read(1) or rows!=33079 or changed!=447291 or sha(base)!=RESULT:raise ValueError('scene output differs')
with dest.open('xb') as f:f.write(base)
print('Applied scene masks;',rows,'objects; template SHA256',RESULT)
