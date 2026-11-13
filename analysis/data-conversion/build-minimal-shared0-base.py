#!/usr/bin/env python3
"""Extract the shared0 template only; private research output, not distributable."""
import hashlib,zipfile
from pathlib import Path
B=Path(__file__).resolve().parents[2]/'build/android';SOURCE=B/'localization-shared-scrubbed-base/base.apk';OUTPUT=B/'shared0-no-sky-font-base/base.zip';N=126
START=98689760; SIZE=524280
FONT_START=132021416; FONT_SIZE=7480
ORIGINAL='eb74c12a63e36273f0f071d54486db6edbecab6e41bb187058bfc515a476d355'
CUBEMAP='b7607c1b9a4cd75ca0c688665e68429c846abada785dc63c9d367f3759efefdf'
FONT='822b721abc587cd2b463138007ea4f0ba162d92dc64c44ad52c8f83c242bb3c8'

def main():
 if OUTPUT.exists():raise ValueError('output exists')
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 original=hashlib.sha256();cubemap=hashlib.sha256();font=hashlib.sha256();scrubbed=hashlib.sha256()
 with zipfile.ZipFile(SOURCE) as source:
  names=source.namelist()
  if len(names)!=len(set(names)):raise ValueError('duplicate source APK entry')
  with zipfile.ZipFile(OUTPUT,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=9) as out:
   for i in range(N):
    name='assets/bin/Data/sharedassets0.assets.split'+str(i);data=source.read(name);expected=min(1048576,132028896-i*1048576)
    if len(data)!=expected:raise ValueError('split size differs')
    original.update(data);start=i*1048576;data=bytearray(data)
    for begin,size,digest in ((START,SIZE,cubemap),(FONT_START,FONT_SIZE,font)):
     lo=max(begin,start);hi=min(begin+size,start+len(data))
     if lo<hi:
      digest.update(data[lo-start:hi-start]);data[lo-start:hi-start]=bytes(hi-lo)
    scrubbed.update(data)
    info=zipfile.ZipInfo(name,(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;out.writestr(info,data)
 if original.hexdigest()!=ORIGINAL or cubemap.hexdigest()!=CUBEMAP or font.hexdigest()!=FONT:
  OUTPUT.unlink();raise ValueError('source base or cubemap differs')
 with OUTPUT.open('rb') as stream:h=hashlib.file_digest(stream,'sha256').hexdigest()
 print('PASS:',N,'template splits;',OUTPUT.stat().st_size,'bytes; ZIP sha256',h,'; scrubbed asset sha256',scrubbed.hexdigest(),'; private research only')
if __name__=='__main__':main()
