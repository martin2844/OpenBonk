#!/usr/bin/env python3
"""Verify final diagnostic entries against private Android reference and previous stage."""
import hashlib,importlib.util,json,struct,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];B=ROOT/'build/android'
s=importlib.util.spec_from_file_location('locale_verify',ROOT/'analysis/data-conversion/verify-localization-diagnostic-obb.py');v=importlib.util.module_from_spec(s);s.loader.exec_module(v)
def digest(data):return hashlib.sha256(data).digest()
def main():
 out=B/'preload-diagnostic-obb/linux.obb';previous=B/'builtin-shader-diagnostic-obb/result.obb';ref=B/'launcher-steam-picker-integrated/main.1.com.megabonk.nativeport.splittest.obb';manifest=json.loads((B/'localization-shared-scrubbed-base/preload-particle-manifest.json').read_text());names={'assets/bin/Data/'+n for n in manifest['files']}
 with zipfile.ZipFile(out) as a,zipfile.ZipFile(previous) as b,zipfile.ZipFile(ref) as c,out.open('rb') as f:
  if len(names)!=68 or set(a.namelist())!=set(b.namelist()) or set(a.namelist())!=set(c.namelist()):raise ValueError('inventory differs')
  exact=locales=stored=updated=0
  for n in a.namelist():
   x=a.read(n);y=c.read(n);info=a.getinfo(n)
   if x==y:exact+=1
   elif n.startswith('assets/aa/Android/localization-string-tables-') or n.endswith('/localization-assets-shared_assets_all.bundle'):
    _,raw=v.m.envelope(y)
    if v.raw_uncompressed(x)!=raw or info.compress_type!=zipfile.ZIP_STORED:raise ValueError('locale differs '+n)
    locales+=1
   else:raise ValueError('non-locale entry differs from reference '+n)
   if n in names:
    if x==b.read(n):raise ValueError('preload entry unchanged '+n)
    updated+=1
   elif digest(x)!=digest(b.read(n)):raise ValueError('other entry changed '+n)
   if info.compress_type==zipfile.ZIP_STORED:
    stored+=1;f.seek(info.header_offset);hdr=f.read(30);nam,extra=struct.unpack_from('<HH',hdr,26)
    if (info.header_offset+30+nam+extra)%4:raise ValueError('unaligned '+n)
  if (exact,locales,stored,updated)!=(1063,18,21,68) or a.comment!=b'OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE':raise ValueError('unexpected totals')
  print('PASS:',exact,'entries byte-identical;',locales,'locale payloads equal with different compression;',updated,'preload entries updated;',stored,'stored entries aligned')
if __name__=='__main__':main()
