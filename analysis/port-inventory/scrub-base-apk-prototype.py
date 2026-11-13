#!/usr/bin/env python3
"""Zero sharedassets0 fragments in a pinned local Android build template.

Other Unity assets, reconstructed and third-party code remain. This unsigned
intermediate is a research artifact, not an installable or redistributable APK.
"""
import argparse,copy,hashlib,os,re,tempfile,zipfile
from pathlib import Path
EXPECTED='850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1048576),b''):h.update(b)
 return h.hexdigest()
p=argparse.ArgumentParser(description=__doc__);p.add_argument('input_apk',type=Path);p.add_argument('output_apk',type=Path);a=p.parse_args()
if a.output_apk.exists() or not a.output_apk.parent.is_dir():p.error('output must not exist and parent must exist')
if not a.input_apk.is_file() or sha(a.input_apk)!=EXPECTED:raise ValueError('unsupported base APK')
prefix='assets/bin/Data/sharedassets0.assets.split'
tmp=None
try:
 with zipfile.ZipFile(a.input_apk) as src:
  names=src.namelist()
  if len(names)!=len(set(names)):raise ValueError('duplicate APK members')
  pieces=[x for x in names if x.startswith(prefix)]
  if len(pieces)!=126 or any(re.fullmatch(re.escape(prefix)+r'\d+',x) is None for x in pieces):raise ValueError('unexpected sharedassets0 fragments')
  pieces.sort(key=lambda x:int(x[len(prefix):]))
  if [int(x[len(prefix):]) for x in pieces]!=list(range(126)):raise ValueError('missing fragments')
  count=sum(src.getinfo(x).file_size for x in pieces)
  if any(src.getinfo(x).file_size!=1048576 for x in pieces[:-1]) or not 0<src.getinfo(pieces[-1]).file_size<=1048576:raise ValueError('fragment sizes mismatch')
  with tempfile.NamedTemporaryFile(prefix='.openbonk-scrub-apk-',dir=a.output_apk.parent,delete=False) as f:tmp=Path(f.name)
  with zipfile.ZipFile(tmp,'w',allowZip64=True) as dest:
   for info in src.infolist():
    if info.filename.upper().startswith('META-INF/'):continue
    data=bytes(info.file_size) if info.filename in pieces else src.read(info)
    dest.writestr(copy.copy(info),data)
 with zipfile.ZipFile(tmp) as check:
  if any(any(check.read(n)) for n in pieces):raise ValueError('fragment not zero')
  if check.testzip() is not None:raise ValueError('invalid ZIP')
 os.link(tmp,a.output_apk)
 print('Zeroed',len(pieces),'sharedassets0 fragments,',count,'bytes; SHA256',sha(a.output_apk))
 print('Unsigned local research template: other data and code remain; not redistributable.')
finally:
 if tmp is not None:tmp.unlink(missing_ok=True)
