#!/usr/bin/env python3
"""Create pinned entry-hash inventories of local research OBBs.

Inventories still need rights review; they are not a release artifact.
"""
import hashlib, json, os, shutil
from pathlib import Path
import tempfile, zipfile

ROOT=Path(__file__).resolve().parents[2]
OBB='main.1.com.megabonk.nativeport.splittest.obb'
SOURCES={
 'old':('no-rewired-split-repaired-v2','ca12a7ec37c7e647d487a310d4924a5ba1f4e4d2cb3d30c8da2a7091d552d961'),
 'current':('no-rewired-current-split-repaired','d794c498d2631f32307c1b0c97b9af77906991f42e07fb33ec3a0baf3ca9a5f5'),
 'transaction':('transaction-split-repaired','3a6ce0ac78310852c1a2252fe65c267a9812b9b900e94c3e9062472a4ada2d58')}
out=ROOT/'build/android/local-split-obb-inventories'
if out.exists() or out.is_symlink():raise FileExistsError(out)
results={}
for label,(folder,digest) in SOURCES.items():
 path=ROOT/'build/android'/folder/OBB
 with path.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest()!=digest:raise ValueError('unexpected OBB: '+label)
 with zipfile.ZipFile(path) as z:
  names=z.namelist()
  if len(names)!=len(set(names)):raise ValueError('duplicate OBB entry')
  if names.count('unity_obb_guid')!=1:raise ValueError('missing GUID')
  guid=z.read('unity_obb_guid').decode('ascii')
  entries={}
  for name in sorted(names):
   info=z.getinfo(name)
   with z.open(info) as f:h=hashlib.file_digest(f,'sha256').hexdigest()
   entries[name]={'size':info.file_size,'sha256':h}
 with path.open('rb') as f:
  if hashlib.file_digest(f,'sha256').hexdigest()!=digest:raise ValueError('OBB changed while reading: '+label)
 results[label]={'version':1,'source_obb_sha256':digest,'unity_obb_guid':guid,'entries':entries}
with tempfile.TemporaryDirectory(prefix='.obb-inventory-',dir=out.parent) as t:
 stage=Path(t)/'inventories';stage.mkdir()
 for label,doc in results.items():
  path=stage/(label+'.json');path.write_text(json.dumps(doc,sort_keys=True,separators=(',',':'))+'\n')
  with path.open('rb') as f:h=hashlib.file_digest(f,'sha256').hexdigest()
  print(label,len(doc['entries']),h)
 out.mkdir(mode=0o700)
 try:
  for path in stage.iterdir():os.link(path,out/path.name)
 except BaseException:
  shutil.rmtree(out);raise
