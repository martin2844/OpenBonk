#!/usr/bin/env python3
"""Build a diagnostic texture plan from the verified extra-texture study."""
import argparse
import hashlib
import importlib.util
import json
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('layouts',Path(__file__).with_name('study-extra-texture-layouts.py'))
study=importlib.util.module_from_spec(spec);spec.loader.exec_module(study)
INPUT=ROOT/'build/android/extra-texture-layout-study/layouts.json'
INPUT_SHA='579e78cb5536fd89e8039a8ce949d76e5dcc1d9ddb90420e4903306f5a53b4fd'
DEFAULT=ROOT/'build/android/extra-texture-android-plan/private-plan.bin'
def sha(b):return hashlib.sha256(b).hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--output',type=Path,default=DEFAULT);p.add_argument('--derived',action='store_true',help='omit header bytes; derive serialized fields at runtime');a=p.parse_args()
 if a.output.exists() or a.output.is_symlink():raise ValueError('output exists')
 data=INPUT.read_bytes()
 if sha(data)!=INPUT_SHA:raise ValueError('layout inventory differs')
 doc=json.loads(data);records=doc['textures']
 if doc['version']!=1 or doc['private'] is not True or len(records)!=25:raise ValueError('bad inventory')
 out=bytearray((b'OBSEX002' if a.derived else b'OBSEX001')+struct.pack('>I',len(records)))
 for pins in study.PINS:
  for h in pins:out.extend(bytes.fromhex(h))
 for r in records:
  name=r['file']
  if len(name)!=32 or any(c not in '0123456789abcdef' for c in name):raise ValueError('bad name')
  l,w=r['linux'],r['windows'];patch=[] if a.derived else r['header_patch']
  if l['sha256']!=w['sha256'] or l['size']!=w['size'] or len(patch)>256:raise ValueError('bad source or patch')
  out.extend(name.encode('ascii'))
  for h in (r['base_sha256'],r['target_sha256'],l['sha256']):out.extend(bytes.fromhex(h))
  out.extend(struct.pack('>IIIIIIIIIIQIQI',r['target_pixel_offset'],r['target_pixel_size'],l['size'],r['width'],r['height'],r['mips'],r['source_format'],r['target_format'],r['suffix_bytes'],study.SOURCE.index(l['file']),l['offset'],study.SOURCE.index(w['file']),w['offset'],len(patch)))
  for offset,value in patch:out.extend(struct.pack('>IB',offset,value))
 a.output.parent.mkdir(parents=True,exist_ok=True)
 with a.output.open('xb') as f:f.write(out)
 print('derived plan:' if a.derived else 'private plan:',len(records),'textures,',0 if a.derived else sum(len(r['header_patch']) for r in records),'header bytes,',len(out),'bytes, sha256',sha(out))
if __name__=='__main__':main()
