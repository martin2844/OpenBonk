#!/usr/bin/env python3
"""Apply a pinned position-only material plan to the private launcher base."""
import hashlib
import io
from pathlib import Path
import struct
import sys
import zlib

ORIGINAL='d1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431'
RESULT='902dfed8c1dff91fe38b7a75acf72bb94b20fac36b121d145e7bdcce09f58e22'
PLAN='59a579b040fd13880a1ae1dff053eb06291df5f52b1b074aadeeab5e46a72593'
PARTS=(((1598882,79343,1048576),(1678334,6938,1048576)),
       ((2268886,66504,1048576),))
FIRST=(0,0)
def sha(x):return hashlib.sha256(x).hexdigest()
if len(sys.argv)!=4:raise ValueError('usage: SCRIPT BASE PLAN OUTPUT')
src,plan_path,dest=map(Path,sys.argv[1:])
if dest.exists() or dest.is_symlink() or not dest.parent.is_dir():raise ValueError('output must be new')
base=bytearray(src.read_bytes());plan_data=plan_path.read_bytes()
if sha(base)!=ORIGINAL or sha(plan_data)!=PLAN:raise ValueError('input differs')
plan=io.BytesIO(plan_data)
if plan.read(8)!=b'OBMAT001' or plan.read(32).hex()!=ORIGINAL or plan.read(32).hex()!=RESULT:raise ValueError('invalid plan header')
rows=changed=0
for scene_index,parts in enumerate(PARTS):
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
        pos,_,_,size=struct.unpack('>IIII',record);pos-=FIRST[scene_index]
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
if plan.read(1) or rows!=487 or changed!=176927 or sha(base)!=RESULT:raise ValueError('scene output differs')
with dest.open('xb') as f:f.write(base)
print('Applied material masks;',rows,'objects; template SHA256',RESULT)
