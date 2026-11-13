#!/usr/bin/env python3
"""Create a private Android base with selected Steam-reconstructible text blanked."""
import argparse,copy,hashlib,importlib.util,json,shutil,struct,tempfile,zipfile,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
repair=load('repair',ROOT/'analysis/port-inventory/repair-split-guid-probe.py')
locale=load('locale',ROOT/'analysis/data-conversion/localization-text-template.py')
APK='66f75a49dbaf514fa254f2aacb88d1e5117abb1360789b4ecf32b577d493cf8d'
OBB='ca0fc86fa3f1344191b04e4be6f0fb05a97fc4bbdb6e0f4dac105b94b3aa7f21'
def sha(b):return hashlib.sha256(b).hexdigest()
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('template',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for v in (a.base,a.template):
  v=v.resolve()
  if out.resolve().is_relative_to(v) or v.is_relative_to(out.resolve()):p.error('output must be separate')
 for f,h in ((a.base/'base.apk',APK),(a.base/'base.obb',OBB),(a.template/'manifest.json',locale.MANIFEST_SHA)):
  if not f.is_file() or repair.sha(f)!=h:raise ValueError('incompatible input: '+str(f))
 doc=json.loads((a.template/'manifest.json').read_text());replacements={};total=0
 with tempfile.TemporaryDirectory(prefix='.openbonk-locale-scrub-',dir=out.parent) as tmp:
  tmp=Path(tmp);apk=tmp/'base.apk';obb=tmp/'base.obb'
  with zipfile.ZipFile(a.base/'base.apk') as az,zipfile.ZipFile(a.base/'base.obb') as oz:
   for z in (az,oz):repair.unique(z)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('GUID mismatch')
   dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,OBB.encode())
   for row in doc['locales']:
    name=row['name'];original=oz.read(locale.ANDROID+name);header,raw=locale.envelope(original);h=(a.template/(name+'.header')).read_bytes();packed=(a.template/(name+'.zlib')).read_bytes();blank=zlib.decompress(packed)
    if sha(original)!=row['bundle_sha256'] or header!=h or sha(h)!=row['header_sha256'] or sha(packed)!=row['packed_sha256'] or sha(blank)!=row['blank_sha256'] or len(blank)!=len(raw):raise ValueError('locale template differs: '+name)
    cursor=0
    for r in sorted(row['ranges'],key=lambda r:r['offset']):
     s=r['offset'];e=s+r['length']
     if s<cursor or e>len(raw) or raw[cursor:s]!=blank[cursor:s] or any(blank[s:e]) or sha(raw[s:e])!=r['sha256']:raise ValueError('locale range differs: '+name)
     total+=e-s;cursor=e
    if raw[cursor:]!=blank[cursor:]:raise ValueError('locale data outside ranges differs')
    replacements[locale.ANDROID+name]=struct.pack('>I',len(h))+h+packed
   if len(replacements)!=17:raise ValueError('unexpected number of locales')
   with zipfile.ZipFile(obb,'x',allowZip64=True) as dest:
    for info in oz.infolist():dest.writestr(copy.copy(info),replacements.get(info.filename,oz.read(info)))
    dest.comment=b'00000000'
   digest=hashlib.sha256()
   with obb.open('rb') as f:
    remaining=obb.stat().st_size-8
    while remaining:
     b=f.read(min(1024*1024,remaining));
     if not b:raise ValueError('short OBB')
     digest.update(b);remaining-=len(b)
   for nonce in range(1_000_000):
    comment=f'{nonce:08x}'.encode();h=digest.copy();h.update(comment);new=h.hexdigest().encode()
    if previous<new<following and new!=OBB.encode():break
   else:raise ValueError('DEX ordering impossible')
   with obb.open('r+b') as f:f.seek(-8,2);f.write(comment)
   if repair.sha(obb).encode()!=new:raise ValueError('OBB mismatch')
   patched=bytearray(dex.replace(OBB.encode(),new));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
   with zipfile.ZipFile(apk,'x',allowZip64=True) as dest:
    for info in az.infolist():
     if not repair.jar_signature(info.filename):dest.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
  with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
   repair.unique(az);repair.unique(oz)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid') or az.read('classes.dex').count(new)!=1:raise ValueError('output mismatch')
   if any(oz.read(n)!=v for n,v in replacements.items()):raise ValueError('locale placeholder mismatch')
  report={'kind':'local research only, not for distribution','apk_sha256':repair.sha(apk),'obb_sha256':repair.sha(obb),'locales':len(replacements),'removed_text_bytes':total,'limitations':['Unmatched text, metadata, other game and third-party content remain.','Not a distributable package.']}
  out.mkdir(mode=0o700)
  try:
   for f in a.base.iterdir():
    if f.is_file() and f.name not in ('base.apk','base.obb','report.json'):shutil.copyfile(f,out/f.name)
   for i in (0,2,4):
    d=out/f'material-extra-{i}';d.mkdir();shutil.copyfile(a.base/f'material-extra-{i}'/'manifest.json',d/'manifest.json')
   shutil.copyfile(apk,out/'base.apk');shutil.copyfile(obb,out/'base.obb');shutil.copyfile(a.template/'manifest.json',out/'localization-text-manifest.json');(out/'report.json').write_text(json.dumps(report,indent=2)+'\n')
  except BaseException:shutil.rmtree(out);raise
 print(json.dumps(report,indent=2))
if __name__=='__main__':main()
