#!/usr/bin/env python3
"""Build an offset/hash plan for an explicitly incomplete research OBB."""
import hashlib
import struct
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
B=ROOT/'build/android'
BASE=B/'localization-shared-scrubbed-base/base.obb'
TARGET=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb'
PREPARED=B/'derived-preparation-check/linux-output'
OUTPUT=B/'partial-obb-android-plan/plan.bin'
BASE_SHA='ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d'
TARGET_SHA='e933769aad4ffe3ebdc609020ede887c1ef4cada8044a881fc0a4d2dab779691'
FOLDERS=('scenes','audio','meshes','textures','animations','converted-textures','derived-extra-textures')
def digest(b):return hashlib.sha256(b).digest()
def sha_file(p):
 with p.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def main():
 if OUTPUT.exists() or OUTPUT.is_symlink():raise ValueError('output exists')
 if sha_file(BASE)!=BASE_SHA or sha_file(TARGET)!=TARGET_SHA:raise ValueError('input OBB mismatch')
 records=[];seen=set()
 with zipfile.ZipFile(BASE) as base,zipfile.ZipFile(TARGET) as target:
  for folder in FOLDERS:
   for f in sorted((PREPARED/folder).iterdir()):
    if not f.is_file() or f.is_symlink():raise ValueError('invalid prepared entry')
    data=f.read_bytes();parts=(len(data)+1048575)//1048576 if folder=='scenes' else 1
    for i in range(parts):
     name=f.name+('.split'+str(i) if folder=='scenes' else '');entry='assets/bin/Data/'+name;offset=i*1048576 if folder=='scenes' else 0;payload=data[offset:offset+1048576] if folder=='scenes' else data
     if entry in seen or entry not in base.namelist() or entry not in target.namelist() or digest(payload)!=digest(target.read(entry)):raise ValueError('invalid reconstructed OBB entry '+entry)
     seen.add(entry);records.append((entry,folder,f.name,offset,len(payload),digest(payload)))
  if len(records)!=344:raise ValueError('incomplete plan inventory')
  unchanged=sum(digest(base.read(n))==digest(target.read(n)) for n in base.namelist() if n not in seen and n in target.namelist())
  print('verified',len(records),'reconstructed entries;',unchanged,'unchanged;',len(target.namelist())-len(records)-unchanged,'target entries still missing or different')
 plan=bytearray(b'OBSPB001'+struct.pack('>I',len(records)))
 for entry,folder,name,offset,size,h in records:
  for value in (entry,folder,name):
   raw=value.encode('ascii');plan.extend(struct.pack('>H',len(raw)));plan.extend(raw)
  plan.extend(struct.pack('>QI',offset,size));plan.extend(h)
 OUTPUT.parent.mkdir(parents=True,exist_ok=True)
 with OUTPUT.open('xb') as f:f.write(plan)
 print('partial plan',len(plan),'bytes sha256',digest(plan).hex(),'; hashes and offsets only; not a release OBB')
if __name__=='__main__':main()
