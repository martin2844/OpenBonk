#!/usr/bin/env python3
"""Independently verify the private built-in shader diagnostic archive."""
import hashlib,struct,zipfile
from pathlib import Path
B=Path(__file__).resolve().parents[2]/'build/android';N='assets/bin/Data/0000000000000000f000000000000000'
def main():
 out=B/'builtin-shader-diagnostic-obb/result.obb';before=B/'animation-diagnostic-obb/linux.obb';ref=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
 with zipfile.ZipFile(out) as a,zipfile.ZipFile(before) as b,zipfile.ZipFile(ref) as c,out.open('rb') as f:
  if set(a.namelist())!=set(b.namelist()) or set(a.namelist())!=set(c.namelist()):raise ValueError('entry inventory differs')
  stored=changed=0
  for n in a.namelist():
   x=a.read(n);y=b.read(n);info=a.getinfo(n)
   if n==N:
    if x!=c.read(n) or x==y:raise ValueError('shader mismatch')
    changed+=1
   elif hashlib.sha256(x).digest()!=hashlib.sha256(y).digest():raise ValueError('unexpected change '+n)
   if info.compress_type==zipfile.ZIP_STORED:
    stored+=1;f.seek(info.header_offset);header=f.read(30);nam,extra=struct.unpack_from('<HH',header,26)
    if (info.header_offset+30+nam+extra)%4:raise ValueError('unaligned '+n)
  if changed!=1 or stored!=21 or a.comment!=b'OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE':raise ValueError('unexpected archive')
  print('PASS: built-in shaders match reference; 1080 other entries preserved; 21 stored entries aligned')
if __name__=='__main__':main()
