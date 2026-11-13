#!/usr/bin/env python3
"""Private diagnostic: store Unity's video-containing resource uncompressed."""
import copy
import hashlib
from pathlib import Path
import sys
import zipfile

EXPECTED='66ae9c8f5cec738cca1b613f014121de9e93e4ea0c81c698c27ba953fe06aa42'
NAME='assets/bin/Data/sharedassets1.resource'

def main():
 if len(sys.argv)!=3: raise ValueError('usage: SCRIPT INPUT_OBB NEW_OUTPUT_OBB')
 source,target=map(Path,sys.argv[1:])
 with source.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest() not in {EXPECTED, '632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29'}: raise ValueError('unexpected input')
 if target.exists() or target.is_symlink() or not target.parent.is_dir(): raise ValueError('invalid output')
 try:
  with zipfile.ZipFile(source) as inp, zipfile.ZipFile(target,'x',allowZip64=True) as out:
   infos=inp.infolist()
   if len(infos)!=1081 or len(set(e.filename for e in infos))!=1081 or NAME not in inp.namelist(): raise ValueError('unexpected inventory')
   out.comment=inp.comment
   for info in infos:
    if info.is_dir() or info.file_size>64*1024*1024: raise ValueError('invalid input entry')
    data=inp.read(info)
    new=copy.copy(info)
    if info.filename==NAME:
     if info.compress_type!=zipfile.ZIP_DEFLATED: raise ValueError('video resource already not deflated')
     new.compress_type=zipfile.ZIP_STORED
    out.writestr(new,data)
  with zipfile.ZipFile(source) as a,zipfile.ZipFile(target) as b:
   if b.getinfo(NAME).compress_type!=zipfile.ZIP_STORED: raise ValueError('resource not stored')
   for info in a.infolist():
    other=b.getinfo(info.filename)
    if (other.file_size,other.CRC)!=(info.file_size,info.CRC): raise ValueError('content differs: '+info.filename)
 except BaseException:
  target.unlink(missing_ok=True);raise
 print('Staged private OBB diagnostic; not distributable.')
if __name__=='__main__':main()
