#!/usr/bin/env python3
"""Check private animation reconstruction and unchanged diagnostic OBB entries."""
import hashlib,struct,zipfile
from pathlib import Path
B=Path(__file__).resolve().parents[2]/'build/android'
def sha(data):return hashlib.sha256(data).digest()
def main():
 previous=B/'localization-diagnostic-obb/linux-v2.obb';output=B/'animation-diagnostic-obb/linux.obb';reference=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';templates=B/'animation-android-copy-plan/android-private-output'
 anim={'assets/bin/Data/'+f.name for f in templates.iterdir() if f.is_file()}
 if len(anim)!=108:raise ValueError('animation inventory differs')
 with zipfile.ZipFile(previous) as a,zipfile.ZipFile(output) as b,zipfile.ZipFile(reference) as c,output.open('rb') as f:
  if set(a.namelist())!=set(b.namelist()) or set(b.namelist())!=set(c.namelist()):raise ValueError('archive inventory differs')
  changed=stored=0
  for n in b.namelist():
   cur=b.read(n);before=a.read(n);info=b.getinfo(n)
   if n in anim:
    if cur!=c.read(n):raise ValueError('animation differs '+n)
    if sha(cur)!=sha(before):changed+=1
   elif sha(cur)!=sha(before):raise ValueError('other data differs '+n)
   if info.compress_type==zipfile.ZIP_STORED:
    stored+=1;f.seek(info.header_offset);header=f.read(30);nam,extra=struct.unpack_from('<HH',header,26)
    if (info.header_offset+30+nam+extra)%4:raise ValueError('unaligned entry '+n)
  if changed!=83 or stored!=21 or b.comment!=b'OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE':raise ValueError('unexpected archive content')
  print('PASS: 108 animations match reference;',changed,'updated;',len(b.namelist())-len(anim),'other entries preserved;',stored,'stored entries aligned')
if __name__=='__main__':main()
