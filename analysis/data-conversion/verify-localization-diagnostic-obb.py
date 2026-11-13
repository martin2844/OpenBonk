#!/usr/bin/env python3
"""Verify a private locale diagnostic OBB against its input and Android reference."""
import hashlib,importlib.util,struct,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
s=importlib.util.spec_from_file_location('locale',ROOT/'analysis/data-conversion/localization-text-template.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
def raw_uncompressed(d):
 pos=d.index(b'\0')+5
 for _ in range(2):pos=d.index(b'\0',pos)+1
 total,ci,ui,flags=struct.unpack_from('>QIII',d,pos)
 if total!=len(d) or ci!=ui or flags!=0x240:raise ValueError('unexpected UnityFS layout')
 info=(pos+35)&~15;block=(info+ci+15)&~15
 unc,comp,kind=struct.unpack_from('>IIH',d,info+20)
 if struct.unpack_from('>I',d,info+16)[0]!=1 or unc!=comp or comp!=len(d)-block or kind:raise ValueError('unexpected UnityFS data block')
 return d[block:]
def main():
 out=B/'localization-diagnostic-obb/linux-v2.obb';base=B/'shared1-diagnostic-obb/linux.obb';ref=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
 with zipfile.ZipFile(out) as a,zipfile.ZipFile(base) as b,zipfile.ZipFile(ref) as c,out.open('rb') as f:
  if set(a.namelist())!=set(b.namelist()) or set(a.namelist())!=set(c.namelist()):raise ValueError('entry inventory differs')
  changed=stored=0
  for n in a.namelist():
   x=a.getinfo(n);content=a.read(n);name=n.rsplit('/',1)[-1]
   if n.startswith('assets/aa/Android/localization-string-tables-') or name=='localization-assets-shared_assets_all.bundle':
    _,raw=m.envelope(c.read(n))
    if raw_uncompressed(content)!=raw or x.compress_type!=zipfile.ZIP_STORED or b.getinfo(n).compress_type!=x.compress_type:raise ValueError('locale differs '+n)
    changed+=1
   elif hashlib.sha256(content).digest()!=hashlib.sha256(b.read(n)).digest():raise ValueError('other entry differs '+n)
   if x.compress_type==zipfile.ZIP_STORED:
    stored+=1;f.seek(x.header_offset);h=f.read(30);nam,extra=struct.unpack_from('<HH',h,26)
    if (x.header_offset+30+nam+extra)%4:raise ValueError('unaligned '+n)
  if changed!=18 or stored!=21 or a.comment!=b'OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE':raise ValueError('unexpected inventory')
  print('PASS:',changed,'locale payloads equal reference;',len(a.namelist())-changed,'other entries preserved;',stored,'stored entries aligned')
if __name__=='__main__':main()
