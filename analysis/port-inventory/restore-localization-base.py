#!/usr/bin/env python3
"""Restore pinned Android locale bundles from the user's Steam copy."""
import argparse,copy,hashlib,importlib.util,json,shutil,struct,subprocess,sys,tempfile,zipfile,zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def load(n,p):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
repair=load('repair',ROOT/'analysis/port-inventory/repair-split-guid-probe.py')
locale=load('locale',ROOT/'analysis/data-conversion/localization-text-template.py')
NEW_APK='9d52de9b0f85a6bd4e3bbed76ee93485023fc1996e30dfce8295f54ee74a99e7';NEW_OBB='ca78de997deb2ba43e2ea6c25ce44339934e355726b6c71aa71e96f343d9544c'
OLD_APK='66f75a49dbaf514fa254f2aacb88d1e5117abb1360789b4ecf32b577d493cf8d';OLD_OBB='ca0fc86fa3f1344191b04e4be6f0fb05a97fc4bbdb6e0f4dac105b94b3aa7f21';OLD_COMMENT=b'000001fb'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('base',type=Path);p.add_argument('steam',type=Path);p.add_argument('output',type=Path);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for v in (a.base,a.steam):
  v=v.resolve()
  if out.resolve().is_relative_to(v) or v.is_relative_to(out.resolve()):p.error('output must be separate')
 for f,h in ((a.base/'base.apk',NEW_APK),(a.base/'base.obb',NEW_OBB),(a.base/'localization-text-manifest.json',locale.MANIFEST_SHA)):
  if not f.is_file() or repair.sha(f)!=h:raise ValueError('incompatible input: '+str(f))
 doc=json.loads((a.base/'localization-text-manifest.json').read_text())
 with tempfile.TemporaryDirectory(prefix='.openbonk-locale-restore-',dir=out.parent) as tmp:
  tmp=Path(tmp);template=tmp/'template';template.mkdir();shutil.copyfile(a.base/'localization-text-manifest.json',template/'manifest.json');restored=tmp/'restored';apk=tmp/'base.apk';obb=tmp/'base.obb'
  with zipfile.ZipFile(a.base/'base.apk') as az,zipfile.ZipFile(a.base/'base.obb') as oz:
   for z in (az,oz):repair.unique(z)
   if az.read('assets/unity_obb_guid')!=oz.read('unity_obb_guid'):raise ValueError('GUID mismatch')
   for row in doc['locales']:
    name=row['name'];placeholder=oz.read(locale.ANDROID+name)
    if len(placeholder)<4:raise ValueError('short placeholder')
    size=struct.unpack_from('>I',placeholder)[0]
    if not 0<size<=1024*1024 or size+4>=len(placeholder):raise ValueError('invalid placeholder')
    header=placeholder[4:4+size];packed=placeholder[4+size:]
    if hashlib.sha256(header).hexdigest()!=row['header_sha256'] or hashlib.sha256(packed).hexdigest()!=row['packed_sha256']:raise ValueError('invalid locale placeholder')
    (template/(name+'.header')).write_bytes(header);(template/(name+'.zlib')).write_bytes(packed)
   subprocess.run([sys.executable,str(ROOT/'analysis/data-conversion/localization-text-template.py'),'restore',str(a.steam),str(template),str(restored)],check=True)
   replacements={locale.ANDROID+row['name']:(restored/row['name']).read_bytes() for row in doc['locales']}
   with zipfile.ZipFile(obb,'x',allowZip64=True) as dst:
    for info in oz.infolist():dst.writestr(copy.copy(info),replacements.get(info.filename,oz.read(info)))
    dst.comment=OLD_COMMENT
   if repair.sha(obb)!=OLD_OBB:raise ValueError('restored OBB differs')
   dex=az.read('classes.dex');previous,following=repair.dex_bounds(dex,NEW_OBB.encode())
   if not previous<OLD_OBB.encode()<following:raise ValueError('DEX string order differs')
   patched=bytearray(dex.replace(NEW_OBB.encode(),OLD_OBB.encode()));patched[12:32]=hashlib.sha1(patched[32:]).digest();patched[8:12]=(zlib.adler32(patched[12:])&0xffffffff).to_bytes(4,'little')
   with zipfile.ZipFile(apk,'x',allowZip64=True) as dst:
    for info in az.infolist():
     if not repair.jar_signature(info.filename):dst.writestr(copy.copy(info),bytes(patched) if info.filename=='classes.dex' else az.read(info))
  if repair.sha(apk)!=OLD_APK:raise ValueError('restored APK differs')
  out.mkdir(mode=0o700)
  try:
   for f in a.base.iterdir():
    if f.is_file() and f.name not in ('base.apk','base.obb','report.json'):shutil.copyfile(f,out/f.name)
   for i in (0,2,4):
    d=out/f'material-extra-{i}';d.mkdir();shutil.copyfile(a.base/f'material-extra-{i}'/'manifest.json',d/'manifest.json')
   shutil.copyfile(apk,out/'base.apk');shutil.copyfile(obb,out/'base.obb');(out/'report.json').write_text('{"kind":"local research only, not for distribution"}\n')
  except BaseException:shutil.rmtree(out);raise
 print('Restored original Android locale base exactly; local research only.')
if __name__=='__main__':main()
